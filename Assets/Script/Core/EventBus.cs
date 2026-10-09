using System;
using System.Collections.Generic;
using UnityEngine;

namespace TapCom.Core
{
    /// <summary>
    /// 类型化事件总线（tech-spec §1 Event）：系统间低耦合通信。
    /// 形态裁决（2026-10-08 用户拍板）：实例 class，注册进 ServiceContainer，经 <see cref="Services"/> 门面获取；
    /// 总线随容器在 Play 轮次重建，订阅不跨轮回残留。
    /// Publish 快照遍历：handler 内订阅/退订安全；单 handler 异常只 LogException 不中断其余。
    /// </summary>
    public class EventBus
    {
        private readonly Dictionary<Type, Delegate> _handlers = new Dictionary<Type, Delegate>();

        public void Subscribe<T>(Action<T> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (_handlers.TryGetValue(typeof(T), out var existing))
                _handlers[typeof(T)] = Delegate.Combine(existing, handler);
            else
                _handlers[typeof(T)] = handler;
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (!_handlers.TryGetValue(typeof(T), out var existing)) return;
            var remaining = Delegate.Remove(existing, handler);
            if (remaining == null) _handlers.Remove(typeof(T));
            else _handlers[typeof(T)] = remaining;
        }

        public void Publish<T>(T evt)
        {
            if (!_handlers.TryGetValue(typeof(T), out var existing)) return;
            foreach (var d in existing.GetInvocationList())
            {
                try
                {
                    ((Action<T>)d)(evt);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
