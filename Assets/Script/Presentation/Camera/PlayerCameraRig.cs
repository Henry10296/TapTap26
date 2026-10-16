using System.Collections;
using TapCom.Core;
using TapCom.Gameplay;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TapCom.Presentation
{
    /// <summary>玩家相机桥（TASK-016 C 线）：Cinemachine 3.x 桥接层，挂 Main Camera（与 CinemachineBrain 同物体）。
    /// 持有两台 CinemachineCamera 引用（场景人工搭建）：
    /// 跟随机位 Follow 指向本类运行时创建的代理目标 = 玩家位置 + 鼠标视口偏移（钳幅内平滑），
    /// 代理坐标每帧在网格边缘钳制后写入（GridBounds 零尺寸则不钳）；
    /// 终点机位平时压优先级，开场运镜时抬到终点格心停留再切回。
    /// 职责：①跟随 + 鼠标偏移 + 边缘钳制 + 动态缩放（Lens.OrthographicSize，移动拉近/静止拉回）
    /// ②开场运镜（收 IntroStartedEvent → 终点停留 _goalDwellTime → 回玩家 → Publish(IntroFinishedEvent)；
    /// GoalPoint=(0,0,0) 无终点则直接回执略过运镜）③双档震动（FailTriggeredEvent → 大震、
    /// SpellCastEvent{Blast} → 小震，组件内默认订阅；TriggerSmall/TriggerLarge 供外部调用）。
    /// 约束：相机 z 恒 _cameraZ（不破坏铁律 #11 投掷的 z=0 平面换算；震动速度 z=0 同理）；
    /// 引用缺失时运镜 fail-open 直接回执 IntroFinishedEvent，不让流程卡死在 Cg 态。</summary>
    public class PlayerCameraRig : MonoBehaviour
    {
        [Header("Cinemachine 引用（人工拖）")]
        [SerializeField] private CinemachineCamera _followCamera;

        /// <summary>终点机位：开场运镜用，无任何过程组件，位置由代码写入。</summary>
        [SerializeField] private CinemachineCamera _goalCamera;

        [SerializeField] private CinemachineImpulseSource _smallShake;

        [SerializeField] private CinemachineImpulseSource _largeShake;

        [Header("跟随与鼠标偏移")]
        [SerializeField] private float _positionSmoothing = 5f;

        /// <summary>鼠标视口偏移系数：偏移 = 视口居中坐标(-0.5~0.5) × 2 × 视野半宽高 × 本系数。</summary>
        [SerializeField] private float _mouseOffsetFactor = 0.35f;

        /// <summary>鼠标偏移最大世界距离（格）；契约默认 ~2.5 格。</summary>
        [SerializeField] private float _maxMouseOffset = 2.5f;

        /// <summary>相机恒定高度（场景 Main Camera 的 z；投掷瞄准按 -z 投回 z=0 平面）。</summary>
        [SerializeField] private float _cameraZ = -10f;

        [Header("动态缩放：移动拉近 / 静止拉回")]
        [SerializeField] private float _idleOrthoSize = 8f;

        [SerializeField] private float _moveOrthoSize = 6.5f;

        [SerializeField] private float _zoomSmoothing = 3f;

        [SerializeField] private float _moveSpeedThreshold = 0.1f;

        [Header("开场运镜")]
        /// <summary>切换机位后的等待时长，须 ≥ CinemachineBrain 默认 Blend 时长（默认 2s）。</summary>
        [SerializeField] private float _introBlendWait = 2f;

        /// <summary>到达终点后的停留时长（契约默认 1s）。</summary>
        [SerializeField] private float _goalDwellTime = 1f;

        /// <summary>运镜切换时机位优先级抬升量（终点 = 跟随 + 本值）。</summary>
        [SerializeField] private int _goalPriorityBoost = 10;

        [Header("震动强度（velocity 幅值；频率/波形在 ImpulseSource Inspector 调）")]
        [SerializeField] private float _smallShakeStrength = 0.8f;

        [SerializeField] private float _largeShakeStrength = 2f;

        private readonly Vector3Int NoGoalPoint = Vector3Int.zero;

        private Transform _target;
        private PlayerActor _targetActor;
        private Camera _camera;
        private Transform _followProxy;
        private Vector2 _smoothedOffset;
        private bool _introRunning;

        private bool _clampEnabled;
        private Vector2 _gridMin;
        private Vector2 _gridMax;
        private Vector3Int _goalPoint;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera != null && !_camera.orthographic)
                Debug.LogWarning("[PlayerCameraRig] 相机不是正交模式，视野/钳制按正交算会失真", this);

            if (_followCamera == null || _goalCamera == null)
                Debug.LogError("[PlayerCameraRig] 未拖 CinemachineCamera 引用：跟随/运镜不可用", this);

            // 运行时代理目标：CinemachineCamera.Follow 指向它，本类只写这一个 Transform
            var proxyGo = new GameObject("CameraFollowProxy");
            _followProxy = proxyGo.transform;
            _followProxy.position = new Vector3(0f, 0f, _cameraZ);
        }

        private void OnDestroy()
        {
            if (_followProxy != null) Destroy(_followProxy.gameObject);
        }

        private void Start()
        {
            if (_followCamera != null && _followProxy != null)
                _followCamera.Follow = _followProxy;

            if (_target == null)
                Debug.LogWarning("[PlayerCameraRig] 暂无跟随目标，等待 PlayerSpawner 生成玩家", this);
        }

        private void OnEnable()
        {
            Services.EventBus.Subscribe<LevelLoadedEvent>(OnLevelLoaded);
            Services.EventBus.Subscribe<IntroStartedEvent>(OnIntroStarted);
            Services.EventBus.Subscribe<FailTriggeredEvent>(OnFailTriggered);
            Services.EventBus.Subscribe<SpellCastEvent>(OnSpellCast);
        }

        private void OnDisable()
        {
            Services.EventBus.Unsubscribe<LevelLoadedEvent>(OnLevelLoaded);
            Services.EventBus.Unsubscribe<IntroStartedEvent>(OnIntroStarted);
            Services.EventBus.Unsubscribe<FailTriggeredEvent>(OnFailTriggered);
            Services.EventBus.Unsubscribe<SpellCastEvent>(OnSpellCast);
        }

        private void LateUpdate()
        {
            if (_followProxy == null) return;

            if (!_introRunning)
            {
                // 跟随目标 = 玩家 + 鼠标偏移（平滑），再按网格钳制；运镜期间冻结回玩家中心
                var playerPos = _target != null ? (Vector2)_target.position : (Vector2)_followProxy.position;
                var desiredOffset = ComputeMouseOffset();
                // unscaled：Cg/Paused 等 timeScale=0 态相机仍要动（GameManager.cs:85 冻结契约）
                _smoothedOffset = Vector2.Lerp(
                    _smoothedOffset, desiredOffset,
                    1f - Mathf.Exp(-_positionSmoothing * Time.unscaledDeltaTime));
                var desired = ClampToGrid(playerPos + _smoothedOffset);
                _followProxy.position = new Vector3(desired.x, desired.y, _cameraZ);
            }

            AnimateZoom();
        }

        /// <summary>运行时绑定跟随目标（PlayerSpawner 生成玩家后调用）；立即贴合并同步 CM 状态，避免换关时从旧机位飘过去。</summary>
        public void SetTarget(Transform target)
        {
            _target = target;
            _targetActor = target != null ? target.GetComponent<PlayerActor>() : null;
            if (target != null) SnapToTarget();
        }

        /// <summary>小震（爆裂配方默认接线）。</summary>
        public void TriggerSmall()
        {
            if (_smallShake == null) return;
            _smallShake.GenerateImpulseWithVelocity(RandomShakeVelocity(_smallShakeStrength));
        }

        /// <summary>大震（失败默认接线）。</summary>
        public void TriggerLarge()
        {
            if (_largeShake == null) return;
            _largeShake.GenerateImpulseWithVelocity(RandomShakeVelocity(_largeShakeStrength));
        }

        private void SnapToTarget()
        {
            if (_followProxy == null || _target == null) return;
            var old = _followProxy.position;
            _smoothedOffset = Vector2.zero;
            _followProxy.position = new Vector3(_target.position.x, _target.position.y, _cameraZ);
            // 通知 CM 目标瞬移：内部分析状态同步平移，镜头无动画直切（换关/重生场景）
            if (_followCamera != null)
                _followCamera.OnTargetObjectWarped(_followProxy, _followProxy.position - old);
        }

        /// <summary>鼠标偏移：屏幕坐标 → 视口居中坐标(-0.5~0.5) → 世界半宽高比例，与相机位置无关（无反馈回路）。</summary>
        private Vector2 ComputeMouseOffset()
        {
            var mouse = Mouse.current;
            if (mouse == null || _camera == null) return Vector2.zero;

            var viewport = _camera.ScreenToViewportPoint(mouse.position.ReadValue());
            var centered = new Vector2(viewport.x - 0.5f, viewport.y - 0.5f);
            var halfWorld = new Vector2(_camera.orthographicSize * _camera.aspect, _camera.orthographicSize);
            var offset = Vector2.Scale(centered * 2f, halfWorld) * _mouseOffsetFactor;
            return Vector2.ClampMagnitude(offset, _maxMouseOffset);
        }

        /// <summary>网格边缘钳制：视野半宽高超出关卡范围时该轴居中，否则钳在 [min+half, max-half]。</summary>
        private Vector2 ClampToGrid(Vector2 pos)
        {
            if (!_clampEnabled || _camera == null) return pos;

            var halfH = _followCamera != null
                ? _followCamera.Lens.OrthographicSize
                : _camera.orthographicSize;
            var halfW = halfH * _camera.aspect;
            return new Vector2(
                ClampAxis(pos.x, _gridMin.x, _gridMax.x, halfW),
                ClampAxis(pos.y, _gridMin.y, _gridMax.y, halfH));
        }

        private static float ClampAxis(float v, float min, float max, float half)
        {
            if (max - min < half * 2f) return (min + max) * 0.5f; // 关卡比视野窄：居中
            return Mathf.Clamp(v, min + half, max - half);
        }

        /// <summary>动态缩放：移动 → moveSize（拉近），静止 → idleSize（拉回）；写 Lens.OrthographicSize。</summary>
        private void AnimateZoom()
        {
            if (_followCamera == null) return;
            var moving = _targetActor != null && _targetActor.Motor != null &&
                         _targetActor.Motor.Velocity.magnitude > _moveSpeedThreshold;
            var targetSize = moving ? _moveOrthoSize : _idleOrthoSize;
            // unscaled：同上，冻结态缩放动画照常收敛（Playing 态 unscaled==scaled，零手感影响）
            _followCamera.Lens.OrthographicSize = Mathf.Lerp(
                _followCamera.Lens.OrthographicSize,
                targetSize,
                1f - Mathf.Exp(-_zoomSmoothing * Time.unscaledDeltaTime));
        }

        // ---- 事件 ----

        private void OnLevelLoaded(LevelLoadedEvent e)
        {
            if (e.Data == null)
            {
                _clampEnabled = false;
                _goalPoint = NoGoalPoint;
                return;
            }

            _goalPoint = e.Data.GoalPoint;
            var b = e.Data.GridBounds;
            if (b.size == Vector3Int.zero)
            {
                _clampEnabled = false; // 零尺寸 = 不钳制（C0 契约）
                return;
            }
            _gridMin = new Vector2(b.xMin, b.yMin);
            _gridMax = new Vector2(b.xMax, b.yMax);
            _clampEnabled = true;
        }

        private void OnIntroStarted(IntroStartedEvent _)
        {
            if (_introRunning) return; // CG 连点防重入
            StartCoroutine(IntroSequence());
        }

        /// <summary>开场运镜：抬终点机位 → 等 Blend → 停留 → 切回 → 等 Blend → 回执。
        /// 无终点（GoalPoint=(0,0,0)）或引用缺失：直接回执略过（fail-open，A 线凭 IntroFinishedEvent 推进 Cg→Playing）。</summary>
        private IEnumerator IntroSequence()
        {
            _introRunning = true;
            try
            {
                var hasGoal = _goalPoint != NoGoalPoint;
                if (!hasGoal || _goalCamera == null || _followCamera == null || _followProxy == null)
                {
                    if (hasGoal)
                        Debug.LogError("[PlayerCameraRig] 有终点但机位引用缺失，跳过运镜直接回执", this);
                    Services.EventBus.Publish(new IntroFinishedEvent());
                    yield break;
                }

                SnapToTarget(); // 运镜期间冻结鼠标偏移，跟随目标回到玩家中心

                _goalCamera.transform.position = new Vector3(
                    _goalPoint.x + 0.5f, _goalPoint.y + 0.5f, _cameraZ);
                var followPriority = (int)_followCamera.Priority;
                // TASK-016 修复：回程还原终点机位「自身」的静态优先级——原实现写 followPriority，
                // 令 Goal=Follow 平级，Brain 平级不切回 → 镜头永远钉在终点不回玩家
                var goalRestPriority = (int)_goalCamera.Priority;
                _goalCamera.Priority = followPriority + _goalPriorityBoost; // Brain 按 Blend 平滑过渡
                // 协程等待一律 realtime（unscaled）：Cg 态 timeScale=0，WaitForSeconds 会永不到期 → 软锁
                yield return new WaitForSecondsRealtime(_introBlendWait);

                yield return new WaitForSecondsRealtime(_goalDwellTime);

                _goalCamera.Priority = goalRestPriority; // 切回玩家
                yield return new WaitForSecondsRealtime(_introBlendWait);

                Services.EventBus.Publish(new IntroFinishedEvent());
            }
            finally
            {
                _introRunning = false;
            }
        }

        private void OnFailTriggered(FailTriggeredEvent _) => TriggerLarge();

        private void OnSpellCast(SpellCastEvent e)
        {
            if (e.Type == SpellType.Blast) TriggerSmall();
        }

        /// <summary>随机方向的 2D 震动速度（z=0：不破坏相机恒定高度与 z=0 平面换算）。</summary>
        private static Vector3 RandomShakeVelocity(float strength)
        {
            var dir = Random.insideUnitCircle;
            if (dir.sqrMagnitude < 1e-6f) dir = Vector2.right;
            return new Vector3(dir.x / dir.magnitude, dir.y / dir.magnitude, 0f) * strength;
        }
    }
}
