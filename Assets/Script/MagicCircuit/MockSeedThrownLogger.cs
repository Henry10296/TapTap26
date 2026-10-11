using TapCom.Core;
using UnityEngine;

namespace TapCom.MagicCircuit
{
    /// <summary>M1 handoff 对接点（mock）：订阅 SeedThrownEvent 打日志自证「请求已送达」。
    /// M1 的 Magic Circuit（落点/连接/成环/效果）实现后整体替换本组件，不复用。</summary>
    public class MockSeedThrownLogger : MonoBehaviour
    {
        private EventBus _bus;

        private void OnEnable()
        {
            // 捕获引用再订阅：teardown 期（容器已 Shutdown）退订不打到 Services 上
            _bus = Services.EventBus;
            _bus.Subscribe<SeedThrownEvent>(OnSeedThrown);
        }

        private void OnDisable() => _bus?.Unsubscribe<SeedThrownEvent>(OnSeedThrown);

        private void OnSeedThrown(SeedThrownEvent e)
        {
            Debug.Log($"[MockSeedThrown] type={e.Type} origin={e.Origin} dir={e.Direction} pv={e.PlayerVelocity}");
        }
    }
}
