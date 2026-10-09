namespace TapCom.Core
{
    /// <summary>
    /// 需要每帧持续更新的系统实现此接口并注册到 TickManager（tech-spec §1 Tick）。
    /// 事件驱动的系统（如环境模拟）不注册本接口。
    /// </summary>
    public interface ITickable
    {
        /// <summary>越小越先 tick。</summary>
        int Priority { get; }

        void Tick(float deltaTime);
    }
}
