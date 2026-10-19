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

        /// <summary>瞄准目标点（鼠标指针投回 z=0 平面；弹道解算的穿过点，2026-10-13 拍板「弹道终于指针」）。</summary>
        public Vector2 Target;

        /// <summary>玩家当前速度（模拟器取 x·0.25 作水平继承，原型 L1633）。</summary>
        public Vector2 PlayerVelocity;

        /// <summary>投掷初速（TASK-016：PlayerThrower 序列化字段，模拟器解算用它保证预览=真实；
        /// &lt;=0 时模拟器回退 MagicParams.ThrowSpeed 兼容旧发布方）。</summary>
        public float Speed;
    }

    /// <summary>种子库存变化（TASK-018 HUD：数量→文本；SetCount 唯一发布点，Start 全量三发做初始绑定）。</summary>
    public struct SeedCountChangedEvent
    {
        public SeedMagicType Type;

        /// <summary>变化后的余量。</summary>
        public int Count;
    }

    /// <summary>当前待投种子属性变化（TASK-018 HUD：ChoiceMark 指针跟随；1/2/3 键与滚轮同源）。</summary>
    public struct SeedTypeChangedEvent
    {
        public SeedMagicType Type;

        /// <summary>true=用户切色（1/2/3 键/滚轮 → 播 Play_Seed_Switch）；false=程序同步（投出后自动换牌 → 静默，仅 HUD 消费）。2026-10-19 用户需求：切色音不进投掷流程。</summary>
        public bool ByUser;
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

        /// <summary>当前格类型（2026-10-18：静止配方圈内格原地转基岩，类型变化随事件到达；表现层按类型重绑贴图。默认 Rock）。</summary>
        public CellType CellType;
    }

    /// <summary>失重场查询（模拟器实现；PlayerMotor 只依赖 Core，不反向依赖 MagicCircuit——依赖倒置）。</summary>
    public interface IZeroFieldQuery
    {
        /// <summary>世界坐标是否处于失重场内。</summary>
        bool InZeroField(Vector2 worldPos);
    }

    /// <summary>失败原因（TASK-015：一滴血，摔落/挤压即失败）。</summary>
    public enum FailReason
    {
        /// <summary>摔落高度超过阈值。</summary>
        Fall,

        /// <summary>被方块挤压（含落定格压入玩家碰撞箱）。</summary>
        Crush,

        /// <summary>关卡限时耗尽（TASK-017，LevelTimer 发布）。</summary>
        TimeUp,
    }

    /// <summary>到达终点（GoalSpot 触发发布；UI 开胜利结算，状态推进归订阅方）。</summary>
    public struct VictoryTriggeredEvent
    {
        /// <summary>终点世界坐标（格心，z=0）。</summary>
        public Vector2 GoalPos;
    }

    /// <summary>玩家失败（摔落/挤压；UI 开失败结算，状态推进归订阅方）。</summary>
    public struct FailTriggeredEvent
    {
        public FailReason Reason;
    }

    /// <summary>玩家起跳瞬间（音频钩子；JumpPressed 边沿触发，一按一发）。</summary>
    public struct PlayerJumpedEvent { }

    /// <summary>玩家非致死落地瞬间（音频钩子；ImpactSpeed = 落地前下落速度，变体选择归 Wwise Random Container）。</summary>
    public struct PlayerLandedEvent
    {
        public float ImpactSpeed;
    }

    /// <summary>加速下落风声（音频钩子；空中下落速度过阈值一次性触发，接地后复位）。</summary>
    public struct PlayerFastFallEvent { }

    /// <summary>脚步循环开关（音频钩子；true=开始走播循环，false=停走发 Stop）。</summary>
    public struct PlayerWalkChangedEvent
    {
        public bool Walking;
    }

    /// <summary>右键瞄准按下/松开沿（音频钩子；Aiming=true 开蓄力循环音，false 发 Stop）。</summary>
    public struct PlayerAimChangedEvent
    {
        public bool Aiming;
    }

    /// <summary>UI 按钮点击（音频钩子；各按钮处理器开头发布）。</summary>
    public struct UiClickedEvent { }

    /// <summary>开场演出开始（CG 图点击继续后由 UI 发布；相机收到开始运镜）。</summary>
    public struct IntroStartedEvent { }

    /// <summary>开场运镜完成（相机发布；UI 收到后推进 Cg → Playing）。</summary>
    public struct IntroFinishedEvent { }
}
