using UnityEngine;

namespace TapCom.Gameplay
{
    /// <summary>
    /// LevelData → WorldGrid 纯数据装载（tech-spec §2 加载）。
    /// 表现物的生成/清理归 Presentation（LevelActorBinder 订阅 LevelLoadedEvent），本类零 GameObject 操作。
    /// </summary>
    public static class LevelLoader
    {
        public static WorldGrid Load(WorldManager manager, string worldId, LevelData data)
        {
            var world = manager.Create(worldId);
            foreach (var def in data.Cells)
            {
                world.AddCell(def.Pos, def.Type, def.Gravity, def.Magic, def.Locked);
            }
            Debug.Log($"[LevelLoader] '{worldId}' loaded: {data.Cells.Count} cells");
            return world;
        }
    }
}
