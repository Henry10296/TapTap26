using System;
using System.Collections.Generic;

namespace TapCom.Gameplay
{
    /// <summary>
    /// 大地图容器：装每关一个 WorldGrid 实例，负责创建 / 获取 / 清理（tech-spec §2 WorldManager）。
    /// 实例互不共享 Cell 对象（Invariant 5）。
    /// </summary>
    public class WorldManager
    {
        private readonly Dictionary<string, WorldGrid> _worlds = new Dictionary<string, WorldGrid>();

        public int Count => _worlds.Count;

        /// <summary>创建关卡世界；重名抛异常（沿 Core Invariant 1 风格 fail fast）。</summary>
        public WorldGrid Create(string worldId)
        {
            if (string.IsNullOrEmpty(worldId))
                throw new ArgumentException("[WorldManager] worldId required", nameof(worldId));
            if (_worlds.ContainsKey(worldId))
                throw new InvalidOperationException($"[WorldManager] world already exists: {worldId}");
            var world = new WorldGrid();
            _worlds[worldId] = world;
            return world;
        }

        /// <summary>直取；不存在抛异常。</summary>
        public WorldGrid Get(string worldId)
        {
            if (_worlds.TryGetValue(worldId, out var world)) return world;
            throw new InvalidOperationException($"[WorldManager] world not found: {worldId}");
        }

        public bool TryGet(string worldId, out WorldGrid world)
        {
            return _worlds.TryGetValue(worldId, out world);
        }

        public bool Destroy(string worldId)
        {
            return _worlds.Remove(worldId);
        }

        public void Clear()
        {
            _worlds.Clear();
        }
    }
}
