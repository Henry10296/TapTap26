using System.Collections.Generic;
using UnityEngine;

namespace TapCom.Gameplay
{
    /// <summary>Cell 类型（M1 单类型；加类型 = 填枚举 + 数据行，tech-spec §10 #3）。
    /// 取值只增不改位（追加到尾部）：已序列化资产按数值索引映射，插入中间会错位。</summary>
    public enum CellType
    {
        Rock,

        /// <summary>基岩（TASK-016）：全魔法免疫的固定地形格——不参与反应、无颜色不触发配方；种子可贴其面落格（地形语义）。</summary>
        Bedrock,
    }

    /// <summary>重力属性（静态规则）：能否被重力效果解锁（TASK-002 Q5 拍板）。</summary>
    public enum CellGravityAttr
    {
        Static,
        Dynamic,
    }

    /// <summary>魔法属性（静态）：与哪种属性的种子反应（TASK-002 蓝红黄拍板）。</summary>
    public enum CellMagicAttr
    {
        None,
        Blue,
        Red,
        Yellow,
    }

    /// <summary>当前状态（运行时生命周期）：tech-spec §3 状态机原文。</summary>
    public enum CellState
    {
        Normal,
        MagicAffected,
        Unstable,
        Collapsed,
        Vanished,
    }

    /// <summary>魔法状态（运行时）：是否连入回路（最小集，Magic Circuit 任务扩展取值）。</summary>
    public enum CellMagicState
    {
        None, 
        Connected,
    }

    /// <summary>
    /// 纯数据单元（tech-spec §3）：不是 GameObject，无表现、无物理（铁律 #1/#2）。
    /// 「字典」落在 WorldGrid 的格点字典；本类用强类型字段（可编译期检查、可调试）。
    /// TASK-016：Bedrock 构造即 Frozen=true（基岩=纯地形），复用静止魔法的免疫闸，编辑器无法误配出可动基岩。
    /// </summary>
    public sealed class Cell
    {
        /// <summary>格点位置（z 保留，内容恒 z=0，铁律 #11）。</summary>
        public Vector3Int Position;

        public CellType Type;
        public CellState State;
        public CellMagicState MagicState;

        /// <summary>重力属性（静态规则）：能否被重力效果解锁。</summary>
        public CellGravityAttr GravityAttr;

        /// <summary>魔法属性（静态）：与哪种属性的种子反应。</summary>
        public CellMagicAttr MagicAttr;

        /// <summary>锁定状态（运行时）：true=冻结（世界默认，无重力无物理）；false=解锁，物理开启。</summary>
        public bool Locked;

        /// <summary>始终固定（GDD 静止魔法配方）：true=永久免疫解锁（爆炸/引力均不可动），世界默认 false。</summary>
        public bool Frozen;

        /// <summary>连接关系（运行时边集）：与本格相连的其他格点位置；只存已存在格点（Invariant 3）。</summary>
        public readonly List<Vector3Int> Connections = new List<Vector3Int>();

        public Cell(Vector3Int position, CellType type, CellGravityAttr gravityAttr, CellMagicAttr magicAttr, bool locked = true)
        {
            Position = position;
            Type = type;
            GravityAttr = gravityAttr;
            MagicAttr = magicAttr;
            Locked = locked;
            State = CellState.Normal;
            MagicState = CellMagicState.None;
            // 基岩=纯地形（TASK-016）：构造即固化。WorldGrid.AddCell 是全库唯一 new Cell 入口
            //（LevelLoader 初始装载必经），在此收口 = 「编辑器画出基岩 → 导出 → 装载即免疫」，无误配路径。
            Frozen = type == CellType.Bedrock;
        }
    }
}
