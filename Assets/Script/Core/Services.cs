using System;

namespace TapCom.Core
{
    /// <summary>
    /// <see cref="ServiceContainer"/> 的静态门面：单容器实例由 CoreDriver 装配（tech-spec §1）。
    /// 未初始化即取会抛出明确异常；CoreDriver.OnDestroy 调 <see cref="Shutdown"/>，防域重载关闭时残留。
    /// </summary>
    public static class Services
    {
        private static ServiceContainer _container;

        /// <summary>全局服务容器。</summary>
        public static ServiceContainer Container => _container ?? throw new InvalidOperationException("[Services] 容器未初始化（Core 未装配）");

        /// <summary>全局事件总线。</summary>
        public static EventBus EventBus => Container.Get<EventBus>();

        public static void Initialize()
        {
            _container ??= new ServiceContainer();
        }

        public static void Shutdown()
        {
            _container = null;
        }
    }
}
