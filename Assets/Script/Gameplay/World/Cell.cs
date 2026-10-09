using System.Collections.Generic;
using UnityEngine;

namespace TapCom.Gameplay
{
    /// <summary>Cell 类型（M1 单类型；加类型 = 填枚举 + 数据行，tech-spec §10 #3）。</summary>
    public enum CellType
    {
        Rock,
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
        }
    }
}
