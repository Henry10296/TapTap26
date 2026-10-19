using UnityEngine;
using UnityEngine.Tilemaps;

namespace TapCom.EditorTools
{
    /// <summary>
    /// 终点笔刷 Tile（TASK-016，关卡编辑器用，与 SpawnTile 同用法）：
    /// 画到 Tilemap 上标记终点位置；导出时读坐标写入 LevelData.GoalPoint（多格画了取最后一个），
    /// 本身不是格子（不进 Cells、无碰撞）。Sprite 在 Inspector 自由配置。
    /// </summary>
    [CreateAssetMenu(fileName = "EndTile", menuName = "魔法回路/EndTile")]
    public class EndTile : Tile
    {
    }
}
