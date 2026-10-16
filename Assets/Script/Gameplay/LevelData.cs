using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TapCom.Gameplay
{
    /// <summary>关卡里一个格子的静态定义（tech-spec §7 Cell Definition）。</summary>
    [Serializable]
    public struct CellDef
    {
        public Vector3Int Pos;
        public CellType Type;
        public CellGravityAttr Gravity;
        public CellMagicAttr Magic;

        /// <summary>初始锁定态；世界默认冻结，恒 true（铁律 #9），字段保留给编辑器显式覆盖。</summary>
        public bool Locked;
    }

    /// <summary>背景层一格（TASK-015）：位置 + Tile 资产引用；运行时 SetTile 重建，保持 Tile 形态。</summary>
    [Serializable]
    public struct BackgroundTileDef
    {
        public Vector3Int Pos;
        public TileBase Tile;
    }

    /// <summary>背景装饰层（TASK-015）：分类分层随关卡走，纯视觉、不进 Cell/魔法反应体系（铁律 #1）。</summary>
    [Serializable]
    public class BackgroundLayerDef
    {
        /// <summary>层名（与编辑器 Background_* Tilemap 同名，便于对照）。</summary>
        public string Name = "Background";

        /// <summary>TilemapRenderer sortingOrder（越小越远；Cells 表现为 0，背景取负值）。</summary>
        public int SortingOrder;

        public List<BackgroundTileDef> Tiles = new List<BackgroundTileDef>();
    }

    /// <summary>
    /// 关卡静态数据资产（tech-spec §7 Level Data）：编辑器生成、运行时只读，与运行时状态严格分离。
    /// </summary>
    [CreateAssetMenu(fileName = "LevelData", menuName = "魔法回路/LevelData")]
    public class LevelData : ScriptableObject
    {
        public List<CellDef> Cells = new List<CellDef>();

        /// <summary>玩家出生点（格子坐标；关卡编辑器用 SpawnTile 笔刷画，导出时写入）。</summary>
        public Vector3Int SpawnPoint;

        /// <summary>终点格子坐标（TASK-015；EndTile 笔刷导出写入；(0,0,0) = 本关无终点）。</summary>
        public Vector3Int GoalPoint;

        /// <summary>开场 CG 图（TASK-015 配置项；null = 本关无 CG，直接运镜进 Playing）。</summary>
        public Sprite CgImage;

        /// <summary>本关限时（秒，TASK-017；0 = 不限时，计时器隐藏）。</summary>
        public float TimeLimit;

        /// <summary>本关初始种子数（TASK-019 每关自配；0 = 该色 0 颗，Inspector 手填）。</summary>
        public int BlueSeeds;
        public int RedSeeds;
        public int YellowSeeds;

        /// <summary>关卡网格范围（TASK-015；导出时按 Tilemap 已用格并集计算，玩家相机边缘钳制用）。</summary>
        public BoundsInt GridBounds;

        /// <summary>背景装饰层（TASK-015）：编辑器从 Background_* Tilemap 导出，运行时由 BackgroundLayerLoader 重建。</summary>
        public List<BackgroundLayerDef> BackgroundLayers = new List<BackgroundLayerDef>();
    }
}
