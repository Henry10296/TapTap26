using TapCom.Core;
using UnityEngine;

namespace TapCom.Gameplay
{
    /// <summary>投掷魔力种子：瞄准方向 → 扣库存 → 发布 SeedThrownEvent。
    /// 铁律 #3/#4：只提交请求；落点、贴合、配方、效果全部由 Magic Circuit 系统负责。
    /// 出点 = ThrowPoint 头顶子物体（TASK-007 抛点契约：与 TrajectoryPreview 出点同源，预览=真实的前提）。</summary>
    public class PlayerThrower : MonoBehaviour
    {
        /// <summary>当前投掷的种子属性；M1 单类型默认蓝，多类型切换非本轮目标（design OQ#6）。</summary>
        [SerializeField] private SeedMagicType _currentSeedType = SeedMagicType.Blue;

        /// <summary>每次投掷随机三色（配方测试用）；关掉则固定用 _currentSeedType。</summary>
        [SerializeField] private bool _randomSeedType = true;

        private static readonly SeedMagicType[] SeedTypes =
        {
            SeedMagicType.Blue,
            SeedMagicType.Red,
            SeedMagicType.Yellow,
        };

        /// <summary>头顶抛点；留空则 Awake 找名为 ThrowPoint 的子物体，再找不到回退 pivot（一次性告警）。</summary>
        [SerializeField] private Transform _throwPoint;

        private PlayerActor _actor;
        private PlayerMotor _motor;
        private Camera _mainCamera;
        private bool _throwPointWarned;

        private void Awake()
        {
            _actor = GetComponent<PlayerActor>();
            _motor = GetComponent<PlayerMotor>();
            if (_throwPoint == null) _throwPoint = transform.Find("ThrowPoint");
        }

        private void Update()
        {
            if (_actor?.InputReader == null || !_actor.InputReader.ThrowPressed) return;

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
            var screen = _actor.InputReader.AimScreenPosition;
            // 正交 2D：把屏幕坐标投回 z=0 世界平面（铁律 #11）
            var planeDistance = -_mainCamera.transform.position.z;
            var aimWorld = _mainCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, planeDistance));

            var origin = ResolveThrowPoint();
            var dir = (Vector2)aimWorld - origin;
            dir = dir.sqrMagnitude < 1e-6f ? Vector2.right : dir.normalized;

            var seedType = _randomSeedType
                ? SeedTypes[Random.Range(0, SeedTypes.Length)]
                : _currentSeedType;

            if (!_actor.Inventory.TryConsume(seedType)) return;
            Services.EventBus.Publish(new SeedThrownEvent
            {
                Type = seedType,
                Origin = origin,
                Direction = dir,
                PlayerVelocity = _motor != null ? _motor.Velocity : Vector2.zero,
            });
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
