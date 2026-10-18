namespace TapCom.MagicCircuit
{
    /// <summary>
    /// M1 模拟参数（TASK-007 design §4.2 参数表）：原型 px 值 ÷32 换算，1 格 = 1 Unity unit。
    /// 改参数改这里（后续 CSV 化 handoff 的落点）；数值语义全部对标 涌现_魔力种子_试玩.html。
    /// </summary>
    public static class MagicParams
    {
        // —— 步进 ——
        /// <summary>固定模拟步长（原型 DT=1/120）。</summary>
        public const float TickDt = 1f / 120f;

        /// <summary>每帧最大模拟步数（卡帧不追帧，原型上限 8 步同款）。</summary>
        public const int MaxStepsPerFrame = 8;

        // —— 投掷 / 弹道 ——
        public const float ThrowSpeed = 13.125f;        // 420 px/s
        public const float PlayerVxInheritRatio = 0.25f;
        public const float Gravity = 23.4375f;          // 750 px/s²
        public const float HorizontalDampingRate = 0.32f;
        public const float VelCapX = 18.75f;            // ±600 px/s
        public const float VelCapY = 20.3125f;          // ±650 px/s

        public const float MuzzleScanDistance = 0.9375f; // 30 px 出手口扫描
        public const float MuzzleScanStep = 0.0625f;     // 2 px
        public const float SeedHitboxHalf = 0.1875f;     // 飞行 hitbox 12×12 px

        // —— 四配方（GDD 规格表 2026-10-11：范围为直径，代码用半径）——
        /// <summary>虚空：消失直径 5 / 失重直径 5 → 半径 2.5（原 3.75 为原型值）。</summary>
        public const float ZeroFieldRadius = 2.5f;

        /// <summary>爆炸：直径 7 → 半径 3.5（原 7.5 为原型暂定半径，实测掀飞整排地面）。</summary>
        public const float BlastRadius = 3.5f;

        /// <summary>引力：直径 9 → 半径 4.5（原 3.5）。</summary>
        public const float AttractRadius = 6f;

        /// <summary>静止：变化直径 5 → 半径 2.5（GDD 新配方）。</summary>
        public const float StaticRadius = 2.5f;

        public const float FieldDuration = 5f;           // 失重/吸引场共用（GDD 魔法持续时长 5）
        public const float ChargeTime = 0.5f;

        public const float BlastImpulseBase = 7.8125f;    // 250 px/s
        public const float BlastImpulseFalloff = 8.125f;  // 260 px/s·(1-d/r)
        public const float BlastUpKick = 3.4375f;         // 110 px/s 向上踢（Unity y-up 取正）

        public const float AttractSeekSpeed = 13.125f;    // 420 px/s（实际取 min(该值, d·6)）
        public const float AttractArriveDistance = 0.35f; // 吸引到位判定（Coding 增补登记）

        // —— 贴合 / 解锁 ——
        public const float DetachGraceTime = 0.22f;      // 解锁宽限，防解锁即回贴
        public const float SnapFaceTolerance = 0.8f;     // 原型 0.8S（M1 贴合走面邻格，参数保留给滑动贴合）
        public const float SnapDistanceTolerance = 0.95f;

        // —— 玩家 / 边界 ——
        public const float ZeroDriftSpeed = 5.9375f;     // 190 px/s 失重漂移
        public const float OutOfBoundsY = -50f;          // 低于此线移除（Coding 增补登记）
    }
}
