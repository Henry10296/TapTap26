using TapCom.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TapCom.EditorTools
{
    /// <summary>
    /// 自带格子定义的笔刷 Tile（tech-spec §7 Level Editor）：
    /// 画到 Tilemap 上即代表一个 Cell；导出时直接读本资产的字段，「笔刷的格子读取对应的格子」。
    /// Sprite / Color 在 Inspector 自由配置（美术自己定）；运行时零引用（铁律 #7）。
    /// TASK-016：cellType 选 Bedrock 时 Magic 强制 None（基岩无色，见下方 CellTileEditor）；
    /// 导出链（TilemapLevelWindow 直读本字段）自动透传，Type=Bedrock 经 LevelLoader 装载即 Frozen（Cell 构造收口）。
    /// </summary>
    [CreateAssetMenu(fileName = "CellTile", menuName = "魔法回路/CellTile")]
    public class CellTile : Tile
    {
        [Header("对应的 Cell 定义")]
        public CellType cellType = CellType.Rock;

        public CellGravityAttr gravity = CellGravityAttr.Dynamic;

        public CellMagicAttr magic = CellMagicAttr.None;
    }

    /// <summary>
    /// CellTile Inspector 守卫（TASK-016）：cellType = Bedrock 时强制 magic = None，
    /// 防误配出「彩色基岩」。运行时另有兜底（EvaluateRecipes 按 Type 排除基岩），这里是画关卡时的源头收口。
    /// </summary>
    [CustomEditor(typeof(CellTile))]
    public class CellTileEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // 先纠正再绘制：DrawDefaultInspector 内部走 serializedObject.Update()，本帧即显示纠正后的值
            var tile = (CellTile)target;
            if (tile.cellType == CellType.Bedrock && tile.magic != CellMagicAttr.None)
            {
                Undo.RecordObject(tile, "Bedrock Force Magic None");
                tile.magic = CellMagicAttr.None;
                EditorUtility.SetDirty(tile);
            }

            DrawDefaultInspector();

            if (tile.cellType == CellType.Bedrock)
                EditorGUILayout.HelpBox("基岩：全魔法免疫地形格，无颜色不触发配方；Magic 已强制 None（TASK-016）", MessageType.Info);
        }
    }
}
