using TapCom.Presentation;
using UnityEngine;

namespace TapCom.Gameplay
{
    /// <summary>
    /// LevelData → WorldGrid + cellActor 实例化（tech-spec §2 加载、§7 转换链的运行时端）。
    /// 本类只管装载；装载后的环境修改表现由 Presentation 订阅事件负责（Magic Circuit 任务接入）。
    /// </summary>
    public static class LevelLoader
    {
        /// <summary>按关卡数据建世界并铺占位表现；actorPrefab 为 null 时只建数据（纯模拟测试用）。</summary>
        public static WorldGrid Load(WorldManager manager, string worldId, LevelData data, CellActor actorPrefab, Transform actorParent = null)
        {
            var world = manager.Create(worldId);
            foreach (var def in data.Cells)
            {
                var cell = world.AddCell(def.Pos, def.Type, def.Gravity, def.Magic, def.Locked);
                if (actorPrefab != null)
                {
                    var actor = Object.Instantiate(actorPrefab, actorParent);
                    actor.Bind(cell);
                }
            }
            Debug.Log($"[LevelLoader] '{worldId}' loaded: {data.Cells.Count} cells");
            return world;
        }
    }
}
