# Design — TASK-009：LevelManager 落位

> 用户指令（2026-10-10）：「开始界面这里应该进行游戏初始化（加载一系列的manager），然后 LevelManager」。上游依据：tech-spec §5（LevelManager：初始化、加载、清理）。
> 本任务把关卡装载从 LevelBootstrap 直连 WorldManager 的近路，改归 LevelManager 正位。

## 1. 状态流与职责对齐（用户指令 → 状态机）

```
Initializing（Core 装配：ServiceContainer/EventBus/TickManager/GameManager/WorldManager/LevelManager 全部注册进 Services）
   → TitleScreen（标题界面显示；此时一系列 manager 已就绪）
   → 点「开始游戏」
   → Loading（LevelManager.Load：清理旧关卡 → LevelLoader 装载数据+表现）
   → Playing（LevelBootstrap 装载完转状态）
```

## 2. 决策

| 决策 | 内容 | 理由 |
|---|---|---|
| LevelManager = Services 单例 | CoreDriver 注册（沿「Service 容器注册所有单例管理器」铁律） | tech-spec §1/§5 |
| 场景引用不入 manager | LevelData/prefab/Tilemap 根仍由场景组件 LevelBootstrap 持有，调 Load 时传入 | manager 可被多场景/多关卡复用；重载场景时 manager 无脏引用 |
| Load 前自动 Unload | 清旧 WorldGrid + 销毁表现父物体 | 支持将来重开关卡；v1 单次装载时无感 |
| 计时/目标/胜负判断 | 不做（M3 归 LevelManager 扩展） | tech-spec §5 运行时状态那半句是 M3 范围 |

## 3. 变量注册表

| 名称 | 类型 | 归属 | Confidence | 锚点 |
|---|---|---|---|---|
| `LevelManager` | class，Services 单例 | Gameplay | High | tech-spec §5 原文 |
| `LevelManager.CurrentWorldId` | string 属性（未装载=null） | Gameplay | High | 本文档 |
| `LevelManager.Load(worlds, worldId, data, actorPrefab, actorParent)` | → WorldGrid；装载前自动清理旧关卡 | Gameplay | High | 本文档 |
| `LevelManager.Unload(worlds)` | 清 WorldGrid + 销毁表现父物体 | Gameplay | High | 本文档 |
| CoreDriver 装配行 +1 | `services.Register(new LevelManager())` + ready 日志追加 | Core | High | 本文档 |
| LevelBootstrap.OnStateChanged 改调 LevelManager | 不再直连 WorldManager/LevelLoader | Gameplay | High | 本文档 |

## 4. 非目标

关卡计时、目标判断、胜负判断、多关卡队列、异步加载画面——M3。

## 5. 验收

| # | 验证点 | 预期信号 |
|---|---|---|
| 1 | 编译 | 零红字 |
| 2 | Play → 点开始 | Console ready 日志含 `LevelManager`；装载链与 TASK-008 一致（标题→Loading→方块出现→Playing） |
| 3 | 场景侧零改动 | LevelBootstrap 三个引用不变，功能等价 |
