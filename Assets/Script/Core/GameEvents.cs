using UnityEngine;
using TapCom.Gameplay;

namespace TapCom.Core
{
    /// <summary>种子魔法属性（蓝/红/黄，tech-spec §4 核心规则 2）。</summary>
    public enum SeedMagicType
    {
        Blue,
        Red,
        Yellow,
    }

    /// <summary>魔法效果类型（GDD 四配方规格：虚空/爆炸/引力/静止）。</summary>
    public enum SpellType
    {
        Zero,
        Blast,
        Attract,
        Static,
    }

    /// <summary>一次种子投掷请求（tech-spec §9 事件流 SeedThrown）。
    /// 铁律 #3/#4：Player 只发布请求；落点、贴合、配方、效果全部由 Magic Circuit 系统负责。</summary>
    public struct SeedThrownEvent
    {
        /// <summary>种子魔法属性。</summary>
        public SeedMagicType Type;

        /// <summary>投掷起点（ThrowPoint 头顶抛点，z=0；与轨迹预览出点同源，TASK-007 抛点契约）。</summary>
        public Vector2 Origin;

        /// <summary>投掷方向（单位向量）。</summary>
        public Vector2 Direction;

        /// <summary>玩家当前速度（模拟器取 x·0.25 作水平继承，原型 L1633）。</summary>
        public Vector2 PlayerVelocity;
    }

    /// <summary>魔法效果释放瞬间（表现钩子：圈/火花/屏感）。蓄能中的高亮走模拟器查询拉取，不发事件。</summary>
    public struct SpellCastEvent
    {
        public SpellType Type;

        /// <summary>效果中心（世界坐标，z=0）。</summary>
        public Vector2 Center;
    }

    /// <summary>字典格增删类型（离散变化；持续运动经模拟器查询拉取，不逐步发事件）。
    /// Changed = 格子在原位变化（属性改写/冻结标记，如引力三块转岩石、静止配方固定）。</summary>
    public enum CellChangeType
    {
        Added,
        Removed,
        Changed,
    }

    /// <summary>字典格增删通知（Presentation 订阅维护 position→view）。
    /// MoverId：Removed 时 = 该格解锁而成的运动格身份（表现层据此把同一本体从静止视图转入运动视图，不换皮）；
    /// Added 时 = 落定运动格的身份（表现层销毁其运动本体）；0 = 与运动格无关（种子落定/消失配方等）。
    /// 注：CellMagicAttr 属 Gameplay（无 asmdef，单程序集容忍跨引用；asmdef 拍板时归位 tech-spec §10 #8）。</summary>
    public struct CellWorldChangedEvent
    {
        public CellChangeType Type;

        public Vector3Int Position;

        public CellMagicAttr Attr;

        public int MoverId;
    }

    /// <summary>失重场查询（模拟器实现；PlayerMotor 只依赖 Core，不反向依赖 MagicCircuit——依赖倒置）。</summary>
    public interface IZeroFieldQuery
    {
        /// <summary>世界坐标是否处于失重场内。</summary>
        bool InZeroField(Vector2 worldPos);
    }
}
