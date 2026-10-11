using TapCom.Core;

namespace TapCom.MagicCircuit
{
    /// <summary>
    /// 三连配方判定表（GDD 四配方规格 2026-10-11）：
    /// 1红2蓝=虚空 / 1红1蓝1黄=爆炸 / 2红1黄=引力 / 2蓝1黄=静止。
    /// M1 代码常量；CSV 化（导表管线）另任务 handoff。
    /// </summary>
    public static class RecipeTable
    {
        /// <summary>按三连的精确色数匹配配方；不匹配返回 false。</summary>
        public static bool TryMatch(int red, int blue, int yellow, out SpellType spell)
        {
            if (red == 1 && blue == 2 && yellow == 0) { spell = SpellType.Zero; return true; }
            if (red == 1 && blue == 1 && yellow == 1) { spell = SpellType.Blast; return true; }
            if (red == 2 && blue == 0 && yellow == 1) { spell = SpellType.Attract; return true; }
            if (red == 0 && blue == 2 && yellow == 1) { spell = SpellType.Static; return true; }
            spell = default;
            return false;
        }
    }
}
