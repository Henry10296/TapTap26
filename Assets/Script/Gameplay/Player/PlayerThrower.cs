using TapCom.Core;
using TapCom.MagicCircuit;
using UnityEngine;

namespace TapCom.Gameplay
{
    /// <summary>投掷魔力种子：瞄准方向 → 扣库存 → 发布 SeedThrownEvent。
    /// 铁律 #3/#4：只提交请求；落点、贴合、配方、效果全部由 Magic Circuit 系统负责。
    /// 出点 = ThrowPoint 头顶子物体（TASK-007 抛点契约：与 TrajectoryPreview 出点同源，预览=真实的前提）。
    /// 2026-10-13：出手口预检前置（拒投不白扣种子）+ 弹道目标点随事件下发。
    /// TASK-016 切色：1/2/3 直接选蓝/红/黄、滚轮 ±1 循环（顺序 Blue→Red→Yellow）；
    /// 所选颜色余 0 → 拒绝投掷且不消耗（UI 提示挂账）。随机三色降级为调试开关（默认关）。</summary>
    public class PlayerThrower : MonoBehaviour
    {
        /// <summary>当前投掷的种子属性；1/2/3 与滚轮改写，SeedIndicator 轮询显示同一值。</summary>
        [SerializeField] private SeedMagicType _currentSeedType = SeedMagicType.Blue;

        /// <summary>随机三色（调试用，TASK-016 默认关）；开启时瞄准开始/每次投出后重掷，手动切色不生效。</summary>
        [SerializeField] private bool _randomSeedType = false;

        /// <summary>投掷初速（TASK-016 Inspector 化）：默认 = MagicParams.ThrowSpeed（420 px/s ÷32）。
        /// 仅经 TrajectoryPreview → Ballistics.SolveThrowVelocity(speed) 接线；模拟器侧接线归 D 线（改此值须同步）。</summary>
        [SerializeField] private float _throwSpeed = MagicParams.ThrowSpeed;

        private static readonly SeedMagicType[] SeedTypes =
        {
            SeedMagicType.Blue,
            SeedMagicType.Red,
            SeedMagicType.Yellow,
        };

        /// <summary>当前待投种子属性：SeedIndicator 显示的就是它，投出的就是显示那颗（拍板「瞄准即定色」）。</summary>
        public SeedMagicType CurrentSeedType { get; private set; }

        /// <summary>投掷初速序列化值（TASK-016；TrajectoryPreview 接线读它，保证预览与配置同源）。</summary>
        public float ThrowSpeed => _throwSpeed;

        /// <summary>头顶抛点；留空则 Awake 找名为 ThrowPoint 的子物体，再找不到回退 pivot（一次性告警）。</summary>
        [SerializeField] private Transform _throwPoint;

        private PlayerActor _actor;
        private PlayerMotor _motor;
        private Camera _mainCamera;
        private LevelManager _levels;
        private WorldManager _worlds;
        private bool _wasAiming;
        private bool _throwPointWarned;
        private GameManager _game;
        private int _lastNonPlayingFrame = int.MinValue; // 最近一次非 Playing 帧号：切回 Playing 首帧吞点击用（2026-10-17）

        private void Awake()
        {
            _actor = GetComponent<PlayerActor>();
            _motor = GetComponent<PlayerMotor>();
            if (_throwPoint == null) _throwPoint = transform.Find("ThrowPoint");
            RollSeedType();
        }

        private void Update()
        {
            var reader = _actor?.InputReader;
            if (reader == null) return;

            // 2026-10-17 修 bug（CG 点击穿透）：非 Playing 态不处理任何投掷输入。
            // 跳过 CG / 暂停恢复的那一下点击发生在状态切换帧内，CgController 与本类 Update 顺序不定，
            // 故切换后首帧再吞一次点击——保证「跳过 CG 的那一下」无论组件先后都带不出种子。
            if (_game == null) _game = Services.Container.Get<GameManager>();
            if (_game.Current != GameState.Playing)
            {
                _lastNonPlayingFrame = Time.frameCount;
                return;
            }
            if (Time.frameCount == _lastNonPlayingFrame + 1) return;

            if (_randomSeedType)
            {
                // 调试旁路：进入瞄准（右键按下沿）掷定本次种子；显示与投出同源
                var aiming = reader.AimHeld;
                if (aiming && !_wasAiming) RollSeedType();
                _wasAiming = aiming;
            }
            else
            {
                // TASK-016 手动切色：1/2/3 直接选，滚轮 ±1 循环（顺序 Blue→Red→Yellow，SeedTypes 数组序）
                if (reader.SelectSeed1Pressed) SetSeedType(SeedMagicType.Blue);
                else if (reader.SelectSeed2Pressed) SetSeedType(SeedMagicType.Red);
                else if (reader.SelectSeed3Pressed) SetSeedType(SeedMagicType.Yellow);
                else if (Mathf.Abs(reader.SeedScrollDelta) > 1e-3f)
                    CycleSeedType(reader.SeedScrollDelta > 0f ? 1 : -1);
            }

            if (!reader.ThrowPressed) return;

            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                Debug.LogError("[PlayerThrower] 找不到主相机（MainCamera tag）", this);
                return;
            }

            TryThrow();
        }

        private void TryThrow()
        {
            // TASK-016：所选颜色余 0 → 拒绝投掷且不消耗（不做耗尽判定，UI 提示挂账）
            if (_actor.Inventory.GetCount(CurrentSeedType) <= 0)
            {
                Debug.Log($"[PlayerThrower] 拒投：{CurrentSeedType} 种子余 0（不消耗，UI 提示挂账）", this);
                return;
            }

            var screen = _actor.InputReader.AimScreenPosition;
            // 正交 2D：把屏幕坐标投回 z=0 世界平面（铁律 #11）
            var planeDistance = -_mainCamera.transform.position.z;
            var aimWorld = _mainCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, planeDistance));

            var origin = ResolveThrowPoint();
            var dir = (Vector2)aimWorld - origin;
            dir = dir.sqrMagnitude < 1e-6f ? Vector2.right : dir.normalized;

            // 出手口预检（与模拟器同一判定 Ballistics.TryScanMuzzle）：被挡直接拒投，不扣种子不发事件
            if (TryGetWorld(out var world) && !Ballistics.TryScanMuzzle(world, origin, dir, out _))
            {
                Debug.Log("[PlayerThrower] throw blocked: muzzle occluded（出手位置被挡，拒投不扣种子）", this);
                return;
            }

            if (!_actor.Inventory.TryConsume(CurrentSeedType)) return;
            Services.EventBus.Publish(new SeedThrownEvent
            {
                Type = CurrentSeedType,
                Origin = origin,
                Direction = dir,
                Target = aimWorld,
                PlayerVelocity = _motor != null ? _motor.Velocity : Vector2.zero,
                Speed = _throwSpeed, // TASK-016：模拟器解算同源，保证预览=真实（数值可调）
            });
            RollSeedType(); // 投出即重掷：继续瞄准时显示的是下一颗
        }

        private void RollSeedType()
        {
            CurrentSeedType = _randomSeedType
                ? SeedTypes[Random.Range(0, SeedTypes.Length)]
                : _currentSeedType;
            // TASK-018：HUD 指针同步（Awake 初始 / 随机瞄准重掷 / 投出后共用此口）
            Services.EventBus.Publish(new SeedTypeChangedEvent { Type = CurrentSeedType });
        }

        /// <summary>TASK-016 手动切色：序列化字段与当前待投值同写（RollSeedType 非随机分支回读 _currentSeedType 不回退）。</summary>
        private void SetSeedType(SeedMagicType type)
        {
            _currentSeedType = type;
            CurrentSeedType = type;
            Services.EventBus.Publish(new SeedTypeChangedEvent { Type = type }); // TASK-018：HUD 指针跟随 1/2/3 键与滚轮
        }

        /// <summary>TASK-016 滚轮循环：delta ±1，超出表尾绕回（Blue→Red→Yellow→Blue）。</summary>
        private void CycleSeedType(int delta)
        {
            var index = System.Array.IndexOf(SeedTypes, CurrentSeedType);
            if (index < 0) index = 0;
            index = (index + delta % SeedTypes.Length + SeedTypes.Length) % SeedTypes.Length;
            SetSeedType(SeedTypes[index]);
        }

        private bool TryGetWorld(out WorldGrid world)
        {
            world = null;
            if (_levels == null) _levels = Services.Container.Get<LevelManager>();
            if (_worlds == null) _worlds = Services.Container.Get<WorldManager>();
            var id = _levels.CurrentWorldId;
            return id != null && _worlds.TryGet(id, out world);
        }

        private Vector2 ResolveThrowPoint()
        {
            if (_throwPoint != null) return _throwPoint.position;
            if (!_throwPointWarned)
            {
                Debug.LogWarning("[PlayerThrower] 未找到 ThrowPoint 子物体，出点回退为角色 pivot（预览与弹道会有偏移）", this);
                _throwPointWarned = true;
            }
            return transform.position;
        }
    }
}
