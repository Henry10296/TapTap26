# Design — TASK-004：Core 模块

> 按 `.claude/templates/design-template.md` 产出。上游依据：`docs/tech-spec.md` §1（Core）、§8 铁律。
> Unity 6000.0.53f1 / C# 9（块级 namespace，不用 C# 10+ 语法）。

## 1. 任务

- Task ID / 日期：TASK-004（编号让位并行任务 TASK-003-Player，active_task 同步）/ 2026-10-08
- 一句话目标：落地 Core 运行框架——GameManager（生命周期 + GameState 状态机 + Service 容器）、TickManager（统一驱动）、EventBus（低耦合通信），并提供启动即自证的运行时日志。
- 对标：里程碑 M1 前置基建；tech-spec §1 原文为唯一需求源。

## 2. 目标 / 非目标

- 目标：
  1. GameState 六状态机（初始化/加载/游戏中/暂停/胜利/失败）+ 非法迁移拒绝；
  2. Service 容器注册所有单例管理器；
  3. TickManager 统一管理运行时更新（优先级排序）；
  4. EventBus 类型化发布/订阅；
  5. 自动 Bootstrap（无需手工搭场景），启动链 Initializing→Loading→Playing 日志自证。
- 非目标（明确不做）：
  - Level 加载真实关卡（M3）；暂停/胜负的真实触发方（M2/M3 才有调用方）；
  - asmdef 隔离（tech-spec §10 #8 待拍板，先进 Assembly-CSharp）；
  - NUnit 测试基建（按用户测试纪律：Play Mode 运行时信号自证优先）；
  - Tick 细分频率（每 N 秒 tick）：v1 用「注册/不注册」表达频率差异，环境模拟走事件驱动（tech-spec §1 Tick 原文）；真实系统出现细分频率需求时再扩展。

## 3. 涉及模块与边界

| 模块 | 读/写 | 说明 |
|---|---|---|
| Core | 写 | 本任务全部产出 |
| 其他模块 | 无 | 不触碰；Player/Magic/Cell 后续任务自己向 Core 注册 |

## 4. 设计

### 4.1 架构 / 流程

```
[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]
  → CoreBootstrap.Install() 创建 "Core" GameObject（DontDestroyOnLoad）
  → CoreDriver.Awake():
      new EventBus            → Services.Register
      new TickManager         → Services.Register
      new GameManager(bus)    → Services.Register
      GameManager.TrySetState(Initializing → Loading → Playing)   // v1 直通链，真实加载 M3 接管
  → CoreDriver.Update(): Services.Get<TickManager>().Tick(Time.deltaTime)
```

### 4.2 变量注册表（Coding 只准出现以下 public 面）

| 名称 | 类型 | 归属 | 用途 | Confidence | Validation-State | 锚点 |
|---|---|---|---|---|---|---|
| `MagicCircuit.Core` | namespace | Core | 全部 Core 类型的命名空间。**裁决（2026-10-08）**：与并行任务 TASK-003-Player 统一——对方 8 文件已锁定 `MagicCircuit.{Core,Gameplay}`，本任务 6 文件让步改名，原 `TapCom.Core` 作废；**【已作废 2026-10-08】**用户实时裁决改统一 `TapCom.*`（全树翻转，见 active_task 证据链） | High | 已核验（对方文件 grep 实证） | 本表 |
| `GameState` | enum {Initializing, Loading, Playing, Paused, Victory, Defeat} | Core | tech-spec §1 六状态 | High | tech-spec §1 原文 | tech-spec.md §1 |
| `GameTransitionException` | class : Exception | Core | 非法迁移报错类型 | High | 新建 | 本表 |
| `GameManager.Current` | GameState 属性 | Core | 当前状态（ctor 无参——EventBus 静态化后不再注入） | High | 新建 | 本表 |
| `GameManager.TrySetState(GameState)` | bool 方法 | Core | 校验+迁移+发事件；非法=log error+return false | High | 新建 | 本表 |
| `GameManager.AllowedTransitions` | static readonly Dictionary | Core | 迁移白名单表 | High | 新建 | 本表 |
| `ServiceContainer` | class | Core | 类型键单例注册表 | High | 新建 | 本表 |
| `ServiceContainer.Register<T>(T)` | void 方法 | Core | 重复注册同类型 = 抛 InvalidOperationException（fail fast） | High | 新建 | 本表 |
| `ServiceContainer.Get<T>()` | T 方法 | Core | 缺失 = 抛 InvalidOperationException（fail fast） | High | 新建 | 本表 |
| `ServiceContainer.TryGet<T>(out T)` | bool 方法 | Core | 非致命查询 | High | 新建 | 本表 |
| `EventBus` | **static class，namespace `MagicCircuit`（外层）** | Core | 类型化 pub/sub。**裁决（2026-10-08）**：TASK-003-Player 已锁定静态调用形态（PlayerThrower:47 / MockSeedThrownLogger:10），且本会话曾误覆盖对方 EventBus.cs——恢复静态语义；放外层命名空间使 MagicCircuit.* 全系无 using 可见；**【已作废 2026-10-08】**用户实时裁决：实例 class + `TapCom.Core`，注册进 ServiceContainer，经 `Services` 门面获取，随 Play 轮次重建 | High | 已核验（调用点 grep 实证） | 本表 |
| `EventBus.Subscribe<T>(Action<T>)` | void 方法 | Core | 订阅 | High | 新建 | 本表 |
| `EventBus.Unsubscribe<T>(Action<T>)` | void 方法 | Core | 退订 | High | 新建 | 本表 |
| `EventBus.Publish<T>(T)` | void 方法 | Core | 发布；handler 异常 catch + LogException 不中断其他 handler；快照遍历支持 Publish 中订阅/退订 | High | 新建 | 本表 |
| `ITickable` | interface | Core | {int Priority; void Tick(float dt);} | High | 新建 | 本表 |
| `TickManager` | class | Core | 注册表 + 按 Priority 稳定排序 + 快照遍历 Tick(dt)；由 CoreDriver 直接驱动（不实现 ITickable——它自己就是驱动源，挂接口是死抽象；2026-10-08 Coding 时回改） | High | 新建 | 本表 |
| `CoreBootstrap` | static class | Core | Install 入口（RuntimeInitializeOnLoadMethod） | High | 新建 | 本表 |
| `CoreDriver` | class : MonoBehaviour | Core | 持有并驱动 TickManager；DontDestroyOnLoad | High | 新建 | 本表 |
| `GameStateChangedEvent` | struct {GameState From, To} | Core | 状态迁移事件（后续 Level/Presentation 订阅） | High | 新建 | 本表 |

事件命名沿 tech-spec §1 Event 原文语义（种子投掷/环境变化事件是 Magic/Player 任务的契约，本任务不定义）。

### 4.3 契约（事件 / 公共接口）

| 名称 | 签名 | 发布方 → 订阅方 | 语义 |
|---|---|---|---|
| GameStateChangedEvent | struct | GameManager → 任意 | 每次成功迁移发一条；订阅方只读 |

### 4.4 数据表

不触配置表管线。

### 4.5 Invariants

1. Service 容器键 = 类型，单例；重复注册/缺失解析都 fail fast（哑配置要炸在启动时，不要运行中哑炸）。
2. EventBus.Publish 中 handler 可以订阅/退订（快照遍历保证）。
3. TickManager.Tick 中可注销 ITickable（快照遍历保证）。
4. 非法状态迁移：拒绝 + error 日志，不抛出到调用方（GameManager.TrySetState 返回 bool）；状态机白名单之外无暗门。
5. Core GameObject 唯一：重复 Install 直接跳过（域重载/多场景安全）。

## 5. 测试计划

> 按用户测试纪律：真实运行环境（Unity Play Mode）运行时信号自证，不写临时脚本。

| 验证点 | 方式 | 预期信号（Console） |
|---|---|---|
| Bootstrap 自动装配 | 进 Play Mode | `[Core] bootstrap installed (Instance-xxxx)` |
| Service 注册链 | Play Mode 启动 | `[Core] services ready: EventBus, TickManager, GameManager` |
| 状态机直通链 | Play Mode 启动 | `[Core] GameState: Initializing -> Loading -> Playing` 两条日志 |
| Tick 驱动 | Play Mode 运行数秒 | `[Core] tick alive: t=…s (… ticks)` 心跳日志（前 5s 每秒一条、之后每 30s 一条——Coding 时定，避免永久刷屏） |
| 非法迁移拒绝 | Play Mode 里按 **F1**（CoreDriver 调试键，Input System `Keyboard.current`） | `[Core] rejected state transition Playing -> Initializing` + TrySetState 返回 false |
| 退域重载不重复 | 关闭再进 Play Mode / Enter Play Settings 域重载 | 不出现两个 Core 实例日志 |

## 6. 风险与回退

- 风险：RuntimeInitializeOnLoadMethod 在域重载关闭时静态残留 → 用「场景里查重 Core 对象」防。
- 回退：全部新文件，无存量依赖；出问题删 Assets/Script/Core/ 即完全回退。

## 7. Open Questions

- 无阻塞项。asmdef（tech-spec §10 #8）维持待拍板，本任务按「先 Assembly-CSharp」执行。

## 8. 任务拆解

| # | 任务 | 改哪些文件 | 验收 |
|---|---|---|---|
| 1 | 状态机 + 服务容器 + 事件总线 | Assets/Script/Core/{GameState, GameManager, ServiceContainer, EventBus}.cs | 编译 0 error |
| 2 | Tick 系统 | Assets/Script/Core/Tick/{ITickable, TickManager}.cs | 编译 0 error |
| 3 | Bootstrap + 驱动 | Assets/Script/Core/CoreBootstrap.cs | §5 验收表前 4 行 Console 信号 |
| 4 | Play Mode 验收 | （用户执行） | §5 全表 |
