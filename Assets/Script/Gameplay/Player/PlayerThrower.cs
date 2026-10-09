using TapCom.Core;
using UnityEngine;

namespace TapCom.Gameplay
{
    /// <summary>投掷魔力种子：瞄准方向 → 扣库存 → 发布 SeedThrownEvent。
    /// 铁律 #3/#4：只提交请求；落点、连接、魔法效果全部由 Magic Circuit 系统负责。</summary>
    public class PlayerThrower : MonoBehaviour
    {
        /// <summary>当前投掷的种子属性；M2 单类型默认蓝，多类型切换非本轮目标（design OQ#6）。</summary>
        [SerializeField] private SeedMagicType _currentSeedType = SeedMagicType.Blue;

        private PlayerActor _actor;
        private Camera _mainCamera;

        private void Awake()
        {
            _actor = GetComponent<PlayerActor>();
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

            var origin = (Vector2)transform.position;
            var dir = (Vector2)aimWorld - origin;
            dir = dir.sqrMagnitude < 1e-6f ? Vector2.right : dir.normalized;

            if (!_actor.Inventory.TryConsume(_currentSeedType)) return;
            Services.EventBus.Publish(new SeedThrownEvent { Type = _currentSeedType, Origin = origin, Direction = dir });
        }
    }
}
