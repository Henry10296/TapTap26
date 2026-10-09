using UnityEngine;

namespace TapCom.Core
{
    /// <summary>种子魔法属性（蓝/红/黄，tech-spec §4 核心规则 2）。</summary>
    public enum SeedMagicType
    {
        Blue,
        Red,
        Yellow,
    }

    /// <summary>一次种子投掷请求（tech-spec §9 事件流 SeedThrown）。
    /// 铁律 #3/#4：Player 只发布请求；落点、连接、魔法效果全部由 Magic Circuit 系统负责。</summary>
    public struct SeedThrownEvent
    {
        /// <summary>种子魔法属性。</summary>
        public SeedMagicType Type;

        /// <summary>投掷起点（玩家位置，z=0）。</summary>
        public Vector2 Origin;

        /// <summary>投掷方向（单位向量）。</summary>
        public Vector2 Direction;
    }
}
