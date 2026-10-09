using System;
using System.Collections.Generic;

namespace TapCom.Core
{
    /// <summary>
    /// 类型键单例服务容器（tech-spec §1：Service 容器注册所有单例管理器）。
    /// 缺失解析与重复注册都 fail fast（Invariant 1：哑配置炸在启动时）。
    /// </summary>
    public class ServiceContainer
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public void Register<T>(T instance) where T : class
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (_services.ContainsKey(typeof(T)))
                throw new InvalidOperationException($"[Core] service already registered: {typeof(T).Name}");
            _services[typeof(T)] = instance;
        }

        public T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
                return (T)service;
            throw new InvalidOperationException($"[Core] service not registered: {typeof(T).Name}");
        }

        public bool TryGet<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var found))
            {
                service = (T)found;
                return true;
            }
            service = null;
            return false;
        }
    }
}
