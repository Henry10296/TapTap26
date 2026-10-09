using System;
using System.Collections.Generic;

namespace TapCom.Core
{
    /// <summary>
    /// 统一驱动所有注册的 ITickable（tech-spec §1 Tick），按 Priority 稳定升序。
    /// Tick 期间注销安全：本帧置空跳过，集合变动下帧重排（Invariant 3）。
    /// </summary>
    public class TickManager
    {
        private readonly List<ITickable> _tickables = new List<ITickable>();
        private ITickable[] _snapshot = new ITickable[0];
        private bool _dirty;

        public void Register(ITickable tickable)
        {
            if (tickable == null) throw new ArgumentNullException(nameof(tickable));
            if (_tickables.Contains(tickable)) return;
            _tickables.Add(tickable);
            _dirty = true;
        }

        public void Unregister(ITickable tickable)
        {
            if (tickable == null) return;
            if (!_tickables.Remove(tickable)) return;
            _dirty = true;
            // 本帧快照内置空：正在 tick 的这帧跳过它，不中断遍历。
            for (var i = 0; i < _snapshot.Length; i++)
            {
                if (ReferenceEquals(_snapshot[i], tickable))
                    _snapshot[i] = null;
            }
        }

        public void Tick(float deltaTime)
        {
            if (_dirty)
            {
                _tickables.Sort((a, b) => a.Priority.CompareTo(b.Priority));
                _snapshot = _tickables.ToArray();
                _dirty = false;
            }
            // Tick 内的 Register 下帧生效（本帧不重排，避免遍历中扩容语义混乱）。
            for (var i = 0; i < _snapshot.Length; i++)
            {
                _snapshot[i]?.Tick(deltaTime);
            }
        }
    }
}
