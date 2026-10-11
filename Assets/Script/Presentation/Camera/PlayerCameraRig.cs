using TapCom.Gameplay;
using UnityEngine;

namespace TapCom.Presentation
{
    /// <summary>玩家相机（tech-spec §6 Camera）：跟随玩家 + 按运动状态动态缩放。
    /// 移动时放大（正交 size 变小、看得近），静止时拉回默认视野。
    /// 只读玩家状态做表现，不参与规则计算（铁律 #6）；z 锁定不动（不破坏铁律 #11 与投掷的 z=0 平面换算）。</summary>
    public class PlayerCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform _target;

        [SerializeField] private Vector2 _offset = new(0f, 1f);

        [SerializeField] private float _positionSmoothing = 5f;

        [Header("动态缩放：移动拉近 / 静止拉回")]
        [SerializeField] private float _idleOrthoSize = 8f;

        [SerializeField] private float _moveOrthoSize = 6.5f;

        [SerializeField] private float _zoomSmoothing = 3f;

        [SerializeField] private float _moveSpeedThreshold = 0.1f;

        private PlayerActor _targetActor;
        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera != null && !_camera.orthographic)
                Debug.LogWarning("[PlayerCameraRig] 相机不是正交模式，缩放不生效", this);
        }

        private void Start()
        {
            if (_target == null)
            {
                // 兜底：场景里按名字找（挂载时忘了拖也能工作）
                var player = GameObject.Find("Player");
                if (player != null) SetTarget(player.transform);
            }

            if (_target == null)
            {
                // 玩家由 PlayerSpawner 运行时生成（进主世界流程），等它生成后 SetTarget 喂入
                Debug.LogWarning("[PlayerCameraRig] 暂无跟随目标，等待 PlayerSpawner 生成玩家", this);
            }
        }

        /// <summary>运行时绑定跟随目标（PlayerSpawner 生成玩家后调用）；立即贴合，避免从原点飘过去。</summary>
        public void SetTarget(Transform target)
        {
            _target = target;
            _targetActor = target != null ? target.GetComponent<PlayerActor>() : null;
            if (target != null) transform.position = ComputeTargetPosition();
        }

        private void LateUpdate()
        {
            if (_target == null || _camera == null) return;

            // 跟随：帧率无关的指数平滑
            transform.position = Vector3.Lerp(
                transform.position,
                ComputeTargetPosition(),
                1f - Mathf.Exp(-_positionSmoothing * Time.deltaTime));

            // 动态缩放：移动 → moveSize（放大），静止 → idleSize（拉回）
            var moving = _targetActor != null && _targetActor.Motor != null &&
                         _targetActor.Motor.Velocity.magnitude > _moveSpeedThreshold;
            var targetSize = moving ? _moveOrthoSize : _idleOrthoSize;
            _camera.orthographicSize = Mathf.Lerp(
                _camera.orthographicSize,
                targetSize,
                1f - Mathf.Exp(-_zoomSmoothing * Time.deltaTime));
        }

        private Vector3 ComputeTargetPosition()
        {
            var p = _target.position + (Vector3)_offset;
            return new Vector3(p.x, p.y, transform.position.z);
        }
    }
}
