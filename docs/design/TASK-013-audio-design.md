# TASK-013 音频模块 Design（骨架 + 事件驱动 + mock 后端）

> Intake 拍板（2026-10-11，用户三选）：① 骨架先行 + mock 后端（Wwise 集成包装好后只补真实后端）② 订阅现有游戏事件（逻辑层零改动）③ 骨架 + 投掷/施法/游戏状态三类事件接点。

---

## 1. 任务

- Task ID / 日期：TASK-013 / 2026-10-11
- 一句话目标：音频模块骨架——订阅现有游戏事件，按映射表查声音 ID，交后端播放；后端本期为日志 mock，Wwise 接入点收口成接口。
- 对标：tech-spec §6（Presentation/Audio 子系统，音频走配置表）+ §9 事件流（逻辑变化 → Presentation 表现变化含 Audio）；GDD M4 提前打地基（用户指令插队，同 TASK-008 模式）。

## 2. 目标 / 非目标

**目标**
1. `IAudioBackend` 播放接口 + `MockAudioBackend`（日志自证）。
2. `AudioManager`：订阅 3 个现有事件 → `AudioCatalog` 查映射 → 调后端。
3. `AudioCatalog`：事件 → 声音 ID 映射（M1 代码常量，对齐 RecipeTable M1 先例；CSV 化另任务）。
4. CoreBootstrap 装配（+2 行），ready 日志含 AudioManager。

**非目标（明确不做）**
- 不装 Wwise / 不写真实 Wwise 后端（等用户装集成包 + 建 Wwise 工程，另任务接后端）。
- 不接 `CellWorldChangedEvent`（落格声等未拍板，记 Open Question）。
- 不做 CSV 音频配置表（tech-spec「音频走配置表」的落地另任务，本期 AudioCatalog 即未来表的代码形态）。
- 不做 3D 空间声 / AkGameObj / Listener 场景配置（Wwise 侧人工活）。
- 不产任何音频资产 / 场景物体（项目铁律：一律人工配置）。
- Victory/Defeat/Loading/Paused 状态不映射声音（真实触发源 M3）。

## 3. 涉及模块与边界

| 模块 | 读/写 | 说明 |
|---|---|---|
| Presentation（新 Audio/ 子目录） | 写 | AudioManager / AudioCatalog / IAudioBackend / MockAudioBackend，namespace `TapCom.Presentation` |
| Core（EventBus / Services） | 读 | 只订阅与注入，零改动 |
| Core（CoreBootstrap.cs） | 写 | 装配 +2 行（写权惯例：Core 线，登记 active_task，TASK-005/007/009 同款先例） |
| Gameplay / MagicCircuit | 读 | 事件 struct 类型引用（SeedThrownEvent / SpellCastEvent / GameStateChangedEvent），零改动 |

依赖方向：Presentation → Core/Gameplay/MagicCircuit（只订阅现有事件，符合既有单向层序，TASK-011 拍板）。

## 4. 设计

### 4.1 架构 / 流程

```
玩法层发布（已存在，零改动）
  PlayerThrower.cs:69        → SeedThrownEvent
  MagicCircuitSimulator.cs:372 → SpellCastEvent
  GameManager.cs:82           → GameStateChangedEvent
        ↓ EventBus（实例，Services.EventBus）
AudioManager（ctor 订阅，随 Play 轮次存活）
        ↓ 查 AudioCatalog（switch 映射，M1 代码常量）
        ↓ 声音 ID（string）
IAudioBackend.Play(id)
  ├─ MockAudioBackend：Debug.Log("[Audio][mock] play {id}")   ← 本期
  └─ WwiseAudioBackend：AkSoundEngine.PostEvent(id, go)        ← 未来另任务，只补此类 + 装配换一个词
```

- AudioManager 为**纯 C# class**（对齐 MagicCircuitSimulator 惯例）：无逐帧需求，不占 TickManager；ctor(EventBus, IAudioBackend) 注入，ctor 内 Subscribe；EventBus 随容器 Play 轮次重建（EventBus.cs:10 注释），订阅不跨轮回残留，无需退订。
- 声音 ID 命名对齐 Wwise Event 惯例（`Play_` / `Music_` 前缀）：将来 Wwise 工程 Event 用同名，后端直接透传，Catalog 零改。

### 4.2 变量注册表

| 名称 | 类型 | 归属模块 | 用途 | Confidence | Validation-State | 证据锚点 |
|---|---|---|---|---|---|---|
| `IAudioBackend` | interface | TapCom.Presentation | 播放后端收口；本期唯一方法 | High | Verified（新接口，形态对齐 AkSoundEngine.PostEvent(event, go) 签名前位） | 本文件 |
| `IAudioBackend.Play(string eventId)` | method | 同上 | 播一个声音 ID（2D 全局声，M1 无位置声/Stop/RTPC） | High | Verified（新方法） | 本文件 |
| `MockAudioBackend` | class : IAudioBackend | 同上 | 日志自证后端 | High | Verified（新类） | 本文件 |
| `AudioCatalog` | static class | 同上 | 事件 → 声音 ID 映射；CSV 化时只改此文件 | High | Verified（新类） | 本文件 |
| `AudioCatalog.MusicTitle / MusicGame` | const string = `Music_Title` / `Music_Game` | 同上 | BGM ID | High | Verified（新常量） | 本文件 |
| `AudioCatalog.Throw` | const string = `Play_Throw` | 同上 | 投掷音 | High | Verified（新常量） | 本文件 |
| `AudioCatalog.SpellZero / SpellBlast / SpellAttract` | const string = `Play_Spell_Zero/Blast/Attract` | 同上 | 三配方音 | High | Verified（新常量） | 本文件 |
| `AudioManager` | class | TapCom.Presentation | 订阅 + 查表 + 调后端 | High | Verified（新类） | 本文件 |
| `AudioManager.ctor(EventBus, IAudioBackend)` | ctor | 同上 | 注入 + 订阅 3 事件 | High | Verified（EventBus.Subscribe 形态实读） | EventBus.cs:17-26 |
| 映射表：SeedThrown → Throw；SpellCast.Type → Spell*；GameStateChanged.To==TitleScreen → MusicTitle；To==Playing → MusicGame | switch | AudioCatalog | M1 全部映射（其余事件/状态不映射 = 静默） | High | Verified（映射源事件实读） | GameEvents.cs 全文 |

复用（零改动，仅引用）：`EventBus.Subscribe<T>`（EventBus.cs:17）、`SeedThrownEvent`（GameEvents.cs:24）、`SpellCastEvent`（GameEvents.cs:40）、`SpellType`（GameEvents.cs:15）、`GameStateChangedEvent`（GameManager.cs:8）。

### 4.3 契约（事件 / 公共接口）

| 名称 | 签名 | 发布方 → 订阅方 | 语义 |
|---|---|---|---|
| 订阅 SeedThrownEvent | `void OnSeedThrown(in 结构只读)` | PlayerThrower（已有）→ AudioManager | 每次投掷 → Play_Throw |
| 订阅 SpellCastEvent | 同上 | MagicCircuitSimulator:372（已有）→ AudioManager | 每次配方释放 → Play_Spell_{Type} |
| 订阅 GameStateChangedEvent | 同上 | GameManager:82（已有）→ AudioManager | To==TitleScreen→Music_Title；To==Playing→Music_Game；其余静默 |
| 新增 IAudioBackend.Play | `void Play(string eventId)` | AudioManager → 后端实现 | 本期唯一新增 public 接口 |

不发布任何新事件（AudioManager 纯消费者）。

### 4.4 数据表

不触配置表管线（AudioCatalog 代码常量，CSV 化另任务）。

### 4.5 Invariants

1. **Audio 不拥有游戏状态**（tech-spec §6 铁律 #6）：只消费事件，不反查玩法数据。
2. **玩法代码零改动**：音频接入不要求任何玩法系统多发事件/改一行。
3. **后端可整体替换**：换 Wwise = 新增一个 IAudioBackend 实现 + 装配行换一个词，AudioManager/Catalog 不动。
4. **无订阅跨轮回残留**：AudioManager 生命周期 = EventBus 所在 Play 轮次，不写退订（对齐 MagicCircuitSimulator）。

## 5. 测试计划（mock 日志自证，用户 Play 实测）

| 验证点 | 方式 | 预期信号 |
|---|---|---|
| 装配 | Play 任意场景 | ready 日志含 `AudioManager` |
| BGM-标题 | Start.unity Play 出标题 | `[Audio][mock] play Music_Title` |
| BGM-游戏 | 点「开始」进 Playing | `play Music_Game` |
| 投掷音 | 投一颗种子 | `play Play_Throw` |
| 配方音 | 凑三连（如 1红2蓝） | `play Play_Spell_Zero`（爆炸/吸引同理） |
| 拒绝路径静默 | F1 非法迁移 | 迁移被拒日志，**无**音频日志（拒绝路径不 Publish，GameManager.cs:74-77） |
| Esc 回标题 | Esc | `play Music_Title` 再次出现 |

## 6. 风险与回退

| 风险 | 处置 |
|---|---|
| GameEvents.cs 正被并行线扩展（本轮实测：CellWorldChangedEvent.MoverId 两次读取间新增） | 本任务订阅的 3 个事件均只读既有字段，struct 加字段不破坏订阅者；Coding 前实时 grep 事件定义再写 |
| CoreBootstrap 写权归 Core 线 | 只追加装配 2 行 + 日志串，登记 active_task（多线先例同款） |
| Wwise 集成包装好后接口不合身 | 接口仅 1 方法，改动面 = 新 Backend 类；真实 PostEvent 需要 gameObject 上下文时，接口加 `PlayAt(string, GameObject)` 属加法不破坏 |
| BGM 重复触发（同一状态重入重播） | M1 状态机白名单下 TitleScreen/Playing 重入均经真实迁移（同一状态迁移会被拒），mock 无害；真实后端任务里若需防重播再议 |

## 7. Open Questions（不阻塞 Coding）

1. `CellWorldChangedEvent`（种子落格 / 格子消失声）是否接——高频声源，未拍板，先不接。
2. Victory / Defeat / Paused 状态音——M3 胜负判定落地后接。
3. Wwise 集成包版本与安装（用户人工活）→ 真实 WwiseAudioBackend 接入另任务。
4. CSV 音频表（表名 / 字段 / 导出）另任务——AudioCatalog 即未来表结构雏形。

## 8. 任务拆解

| # | 任务 | 改哪些文件 | 验收 |
|---|---|---|---|
| 1 | IAudioBackend + MockAudioBackend | 新 `Script/Presentation/Audio/AudioBackend.cs` | 编译过 |
| 2 | AudioCatalog + AudioManager | 新 `Script/Presentation/Audio/AudioManager.cs` | 编译过 |
| 3 | 装配 | 改 `Script/Core/CoreBootstrap.cs`（+2 行 + ready 日志串） | ready 日志含 AudioManager |
| 4 | Play 验收 | 无代码 | §5 表 7 项用户实测 |
