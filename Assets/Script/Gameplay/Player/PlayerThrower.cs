using TapCom.Core;
using TapCom.MagicCircuit;
using UnityEngine;

namespace TapCom.Gameplay
{
    /// <summary>投掷魔力种子：瞄准方向 → 扣库存 → 发布 SeedThrownEvent。
    /// 铁律 #3/#4：只提交请求；落点、贴合、配方、效果全部由 Magic Circuit 系统负责。
    /// 出点 = ThrowPoint 头顶子物体（TASK-007 抛点契约：与 TrajectoryPreview 出点同源，预览=真实的前提）。
    /// 2026-10-13 三改：①出手口预检前置到扣种子之前（贴墙/贴天花板拒投不再白扣种子；模拟器仍是权威，
    /// 同帧竞态拒绝时以模拟器日志为准）②弹道目标点随事件下发（SeedThrownEvent.Target，解算归模拟器）③
    /// 随机三色改「瞄准即定色」——瞄准开始/每次投出后掷定 CurrentSeedType（SeedIndicator 显示用），投出的即显示那颗。</summary>
    public class PlayerThrower : MonoBehaviour
    {
        /// <summary>当前投掷的种子属性；M1 单类型默认蓝，多类型切换非本轮目标（design OQ#6）。</summary>
        [SerializeField] private SeedMagicType _currentSeedType = SeedMagicType.Blue;

        /// <summary>随机三色（配方测试用）；开启时瞄准开始/每次投出后重掷 CurrentSeedType，关掉则固定用 _currentSeedType。</summary>
        [SerializeField] private bool _randomSeedType = true;

        private static readonly SeedMagicType[] SeedTypes =
        {
            SeedMagicType.Blue,
            SeedMagicType.Red,
            SeedMagicType.Yellow,
        };

        /// <summary>当前待投种子属性：SeedIndicator 显示的就是它，投出的就是显示那颗（拍板「瞄准即定色」）。</summary>
        public SeedMagicType CurrentSeedType { get; private set; }

        /// <summary>头顶抛点；留空则 Awake 找名为 ThrowPoint 的子物体，再找不到回退 pivot（一次性告警）。</summary>
        [SerializeField] private Transform _throwPoint;

        private PlayerActor _actor;
        private PlayerMotor _motor;
        private Camera _mainCamera;
        private LevelManager _levels;
        private WorldManager _worlds;
        private bool _wasAiming;
        private bool _throwPointWarned;

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

            // 进入瞄准（右键按下沿）掷定本次种子；显示与投出同源
            var aiming = reader.AimHeld;
            if (aiming && !_wasAiming) RollSeedType();
            _wasAiming = aiming;

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
            });
            RollSeedType(); // 投出即重掷：继续瞄准时显示的是下一颗
        }

        private void RollSeedType()
        {
            CurrentSeedType = _randomSeedType
                ? SeedTypes[Random.Range(0, SeedTypes.Length)]
                : _currentSeedType;
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
