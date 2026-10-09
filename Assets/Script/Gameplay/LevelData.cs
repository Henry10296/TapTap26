using System;
using System.Collections.Generic;
using UnityEngine;

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

    /// <summary>
    /// 关卡静态数据资产（tech-spec §7 Level Data）：编辑器生成、运行时只读，与运行时状态严格分离。
    /// </summary>
    [CreateAssetMenu(fileName = "LevelData", menuName = "魔法回路/LevelData")]
    public class LevelData : ScriptableObject
    {
        public List<CellDef> Cells = new List<CellDef>();
    }
}
