using System;
using System.Collections.Generic;
using UnityEngine;

namespace TapCom.Gameplay
{
    /// <summary>
    /// 关卡序列配置资产（TASK-015）：数字（索引+1）对应一个字符串 id，按顺序加载。
    /// GameBootstrap 引用本资产注入 LevelManager；id 用于管理/展示，装载走 Level 资产引用。
    /// </summary>
    [CreateAssetMenu(fileName = "LevelSequence", menuName = "魔法回路/LevelSequence")]
    public class LevelSequence : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            /// <summary>关卡字符串 id（如 Level_00、Level_01）。</summary>
            public string LevelId;

            /// <summary>对应关卡资产（导表编辑器导出产物）。</summary>
            public LevelData Level;
        }

        public List<Entry> Entries = new List<Entry>();

        /// <summary>按序取 LevelData 数组（注入 LevelManager.SetSequence 用）。</summary>
        public LevelData[] ToArray()
        {
            var levels = new LevelData[Entries.Count];
            for (var i = 0; i < Entries.Count; i++)
                levels[i] = Entries[i].Level;
            return levels;
        }
    }
}
