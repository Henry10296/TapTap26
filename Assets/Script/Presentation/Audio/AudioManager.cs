using TapCom.Core;

namespace TapCom.Presentation
{
    /// <summary>
    /// 事件 → 声音 ID 映射（M1 代码常量，对齐 RecipeTable M1 先例；CSV 化另任务）。
    /// 常量值 = Wwise 工程 Event 名（引擎内按名字 hash 匹配 bank，改名即失配）。
    /// 2026-10-13 bank 扩充（27 事件）后接通投掷/四配方；2026-10-18 按音效设计表接通
    /// 玩家动作（跳/落地/脚步循环/加速下落/被砸）与种子落格（TASK-013「待设计增量」落地）。
    /// </summary>
    public static class AudioCatalog
    {
        private const string MusicPlay = "Play_BGM";

        private const string MusicStop = "Stop_BGM";

        private const string SeedThrow = "Play_Seed_Throw";

        // —— 玩家动作 + 种子落格（2026-10-18 音效设计表；Walk 是 Loop，必须配 bank 里的 Stop_Player_Walk）——
        private const string PlayerJump = "Play_Player_Jump";

        private const string PlayerLand = "Play_Player_Land";

        private const string PlayerWalk = "Play_Player_Walk";

        private const string PlayerWalkStop = "Stop_Player_Walk";

        private const string PlayerFastFall = "Play_Player_FastFall";

        private const string PlayerCrushed = "Play_Player_Crushed";

        private const string SeedLand = "Play_Seed_Land";

        // —— 第二批（2026-10-18 全表接线；Event 名以 bank Main.txt 为准，设计表 Play_ULClick 系笔误实为 Play_UI_Click）——
        private const string SeedSwitch = "Play_Seed_Switch";

        /// <summary>配方确认音（设计表「关键反馈：魔法激活确认音」；SpellCast 时与配方音叠加，public 供 handler 直发）。</summary>
        public const string SpellConfirm = "Play_Seed_Trigger";

        private const string SeedAim = "Play_Seed_Aim";

        private const string SeedAimStop = "Stop_Seed_Aim";

        private const string RockFall = "Play_Rock_Fall";

        private const string RockLand = "Play_Rock_Land";

        private const string RockCrumble = "Play_Rock_Crumble";

        private const string LevelComplete = "Play_Level_Complete";

        private const string LevelFailed = "Play_Level_Failed";

        private const string LevelReset = "Play_Level_Reset";

        private const string AmbientPlay = "Play_Ambient_Ruin";

        private const string AmbientStop = "Stop_Ambient_Ruin";

        private const string UiClick = "Play_UI_Click";

        public static string For(in SeedThrownEvent evt) => SeedThrow;

        public static string For(in PlayerJumpedEvent evt) => PlayerJump;

        public static string For(in PlayerLandedEvent evt) => PlayerLand;

        /// <summary>脚步循环开关：开=播 Loop，关=发 Stop（bank 配对事件）。</summary>
        public static string For(in PlayerWalkChangedEvent evt) => evt.Walking ? PlayerWalk : PlayerWalkStop;

        public static string For(in PlayerFastFallEvent evt) => PlayerFastFall;

        /// <summary>失败音分诊：被砸→压扁音；摔落/超时→通用失败音（设计表「倒计时归零与种子耗尽共用」；种子耗尽失败代码未实现，挂 open-question）。</summary>
        public static string For(in FailTriggeredEvent evt) => evt.Reason == FailReason.Crush ? PlayerCrushed : LevelFailed;

        /// <summary>格变化四分诊（2026-10-18 全表）：种子落格→石粒声；运动格砸地→Rock_Land；消失散落→碎裂；解锁坠始→风声；原位改写不发声。</summary>
        public static string For(in CellWorldChangedEvent evt) => evt.Type switch
        {
            CellChangeType.Added => evt.MoverId == 0 ? SeedLand : RockLand,
            CellChangeType.Removed => evt.MoverId == 0 ? RockCrumble : RockFall,
            _ => null,
        };

        /// <summary>切色音（1/2/3 键+滚轮；三色音高差异归 Wwise 侧）。</summary>
        public static string For(in SeedTypeChangedEvent evt) => SeedSwitch;

        public static string For(in VictoryTriggeredEvent evt) => LevelComplete;

        /// <summary>瞄准循环开关：按住=蓄力 Loop，松开=Stop（bank 配对事件）。</summary>
        public static string For(in PlayerAimChangedEvent evt) => evt.Aiming ? SeedAim : SeedAimStop;

        public static string For(in UiClickedEvent evt) => UiClick;

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
            GameState.Loading => MusicStop, // reset/换关全过 Loading（LevelManager.Reload/Advance），停旧曲让 Playing 从头播（2026-10-18 用户需求：BGM 跟着重开）
            GameState.Victory or GameState.Defeat => MusicStop, // 结算即停（2026-10-18 拍板按设计表「通关/死亡时停止」）
            _ => null,
        };

        /// <summary>环境底噪（设计表：遗迹滴水+嗡鸣低音量持续）：跟 BGM 同状态机，结算即停（2026-10-18 拍板）。</summary>
        public static string ForAmbient(in GameStateChangedEvent evt) => evt.To switch
        {
            GameState.Playing => AmbientPlay,
            GameState.TitleScreen or GameState.Loading or GameState.Victory or GameState.Defeat => AmbientStop,
            _ => null,
        };

        /// <summary>重开音：R 重开（Playing→Loading）与败重试（Defeat→Loading）发；下一关（Victory→Loading）与首次开始（TitleScreen→Loading）不发。</summary>
        public static string ForReset(in GameStateChangedEvent evt) =>
            evt.To == GameState.Loading && (evt.From == GameState.Playing || evt.From == GameState.Defeat) ? LevelReset : null;
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
            bus.Subscribe<PlayerJumpedEvent>(OnPlayerJumped);
            bus.Subscribe<PlayerLandedEvent>(OnPlayerLanded);
            bus.Subscribe<PlayerWalkChangedEvent>(OnPlayerWalkChanged);
            bus.Subscribe<PlayerFastFallEvent>(OnPlayerFastFall);
            bus.Subscribe<FailTriggeredEvent>(OnFail);
            bus.Subscribe<CellWorldChangedEvent>(OnCellWorldChanged);
            bus.Subscribe<SeedTypeChangedEvent>(OnSeedTypeChanged);
            bus.Subscribe<VictoryTriggeredEvent>(OnVictoryTriggered);
            bus.Subscribe<PlayerAimChangedEvent>(OnPlayerAimChanged);
            bus.Subscribe<UiClickedEvent>(OnUiClicked);
        }

        private void OnSeedThrown(SeedThrownEvent evt) => PlayMapped(AudioCatalog.For(evt));

        /// <summary>配方音 + 确认音叠加（设计表 Play_Seed_Trigger「关键反馈：魔法激活确认音」）。</summary>
        private void OnSpellCast(SpellCastEvent evt)
        {
            PlayMapped(AudioCatalog.For(evt));
            PlayMapped(AudioCatalog.SpellConfirm);
        }

        /// <summary>状态变化多音并发：BGM + 环境底噪 + 重开音各自独立判定（null 跳过）。</summary>
        private void OnStateChanged(GameStateChangedEvent evt)
        {
            PlayMapped(AudioCatalog.For(evt));
            PlayMapped(AudioCatalog.ForAmbient(evt));
            PlayMapped(AudioCatalog.ForReset(evt));
        }

        private void OnPlayerJumped(PlayerJumpedEvent evt) => PlayMapped(AudioCatalog.For(evt));

        private void OnPlayerLanded(PlayerLandedEvent evt) => PlayMapped(AudioCatalog.For(evt));

        private void OnPlayerWalkChanged(PlayerWalkChangedEvent evt) => PlayMapped(AudioCatalog.For(evt));

        private void OnPlayerFastFall(PlayerFastFallEvent evt) => PlayMapped(AudioCatalog.For(evt));

        private void OnFail(FailTriggeredEvent evt) => PlayMapped(AudioCatalog.For(evt));

        private void OnCellWorldChanged(CellWorldChangedEvent evt) => PlayMapped(AudioCatalog.For(evt));

        private void OnSeedTypeChanged(SeedTypeChangedEvent evt) => PlayMapped(AudioCatalog.For(evt));

        private void OnVictoryTriggered(VictoryTriggeredEvent evt) => PlayMapped(AudioCatalog.For(evt));

        private void OnPlayerAimChanged(PlayerAimChangedEvent evt) => PlayMapped(AudioCatalog.For(evt));

        private void OnUiClicked(UiClickedEvent evt) => PlayMapped(AudioCatalog.For(evt));

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
