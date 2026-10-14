using TapCom.Core;

namespace TapCom.Presentation
{
    /// <summary>
    /// 事件 → 声音 ID 映射（M1 代码常量，对齐 RecipeTable M1 先例；CSV 化另任务）。
    /// 常量值 = Wwise 工程 Event 名（引擎内按名字 hash 匹配 bank，改名即失配）。
    /// 2026-10-13 bank 扩充（27 事件）后接通投掷/四配方；跳跃/脚步/瞄准循环等
    /// 无对应游戏事件，待设计增量（active_task TASK-013）。
    /// </summary>
    public static class AudioCatalog
    {
        private const string MusicPlay = "Play_BGM";

        private const string MusicStop = "Stop_BGM";

        private const string SeedThrow = "Play_Seed_Throw";

        public static string For(in SeedThrownEvent evt) => SeedThrow;

        /// <summary>四配方（RecipeTable）→ 魔法音效。</summary>
        public static string For(in SpellCastEvent evt) => evt.Type switch
        {
            SpellType.Zero => "Play_Magic_LowGravity",
            SpellType.Blast => "Play_Magic_Explode",
            SpellType.Attract => "Play_Magic_Attract",
            SpellType.Static => "Play_Magic_Static",
            _ => null,
        };

        public static string For(in GameStateChangedEvent evt) => evt.To switch
        {
            GameState.Playing => MusicPlay,
            GameState.TitleScreen => MusicStop,
            _ => null,
        };
    }

    /// <summary>
    /// 音频模块（tech-spec §6 Presentation/Audio）：订阅游戏事件 → 查 AudioCatalog → 调后端。
    /// 纯消费者不拥有游戏状态（铁律 #6）；玩法代码零改动。生命周期 = EventBus 所在 Play 轮次，
    /// 订阅不跨轮回残留，无需退订（对齐 MagicCircuitSimulator）。
    /// </summary>
    public class AudioManager
    {
        private readonly IAudioBackend _backend;

        public AudioManager(EventBus bus, IAudioBackend backend)
        {
            _backend = backend;
            bus.Subscribe<SeedThrownEvent>(OnSeedThrown);
            bus.Subscribe<SpellCastEvent>(OnSpellCast);
            bus.Subscribe<GameStateChangedEvent>(OnStateChanged);
        }

        private void OnSeedThrown(SeedThrownEvent evt) => PlayMapped(AudioCatalog.For(evt));

        private void OnSpellCast(SpellCastEvent evt) => PlayMapped(AudioCatalog.For(evt));

        private void OnStateChanged(GameStateChangedEvent evt) => PlayMapped(AudioCatalog.For(evt));

        private void PlayMapped(string soundId)
        {
            if (soundId == null)
            {
                return;
            }
            _backend.Play(soundId);
        }
    }
}
