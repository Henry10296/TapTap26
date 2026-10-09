using TapCom.Gameplay;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TapCom.EditorTools
{
    /// <summary>
    /// 自带格子定义的笔刷 Tile（tech-spec §7 Level Editor）：
    /// 画到 Tilemap 上即代表一个 Cell；导出时直接读本资产的字段，「笔刷的格子读取对应的格子」。
    /// Sprite / Color 在 Inspector 自由配置（美术自己定）；运行时零引用（铁律 #7）。
    /// </summary>
    [CreateAssetMenu(fileName = "CellTile", menuName = "魔法回路/CellTile")]
    public class CellTile : Tile
    {
        [Header("对应的 Cell 定义")]
        public CellType cellType = CellType.Rock;

        public CellGravityAttr gravity = CellGravityAttr.Dynamic;

        public CellMagicAttr magic = CellMagicAttr.None;
    }
}
