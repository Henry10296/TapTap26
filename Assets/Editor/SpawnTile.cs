using UnityEngine;
using UnityEngine.Tilemaps;

namespace TapCom.EditorTools
{
    /// <summary>
    /// 出生点笔刷 Tile（关卡编辑器用，与 CellTile 同用法）：
    /// 画到 Tilemap 上标记玩家出生位置；导出时读坐标写入 LevelData.SpawnPoint，
    /// 本身不是格子（不进 Cells、无碰撞）。Sprite 在 Inspector 自由配置。
    /// </summary>
    [CreateAssetMenu(fileName = "SpawnTile", menuName = "魔法回路/SpawnTile")]
    public class SpawnTile : Tile
    {
    }
}
