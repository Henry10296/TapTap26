# Design — TASK-011：分层与依赖重构（四个 smell 清零）

> 用户 2026-10-10「开始」——授权执行此前列出的四项设计问题修复。
> 行为不变承诺：启动 → 标题 → 开始 → 装载 → Playing 全链对外表现一致。

## 1. 目标架构（依赖单向：Presentation → Gameplay → Core）

```
CoreDriver（唯一组合根，构造注入一切）
  GameManager(bus)                ← bus 构造注入，不再内部摸 Services
  LevelManager(worlds, game, bus) ← 构造注入；只管数据装载 + 状态推进 + 发事件
  LevelLoader（纯静态数据装载）     ← 不再 Instantiate 任何表现物
TitleScreen 点开始 → Loading
  LevelBootstrap（场景引用容器）→ LevelManager.Load(worldId, data)
  LevelManager：装数据 → 发 LevelLoadedEvent{WorldId, World} → TrySetState(Playing)
  LevelActorBinder（Presentation，场景组件）← 订阅 LevelLoadedEvent → 生成/清理 CellActor
```

## 2. 四 smell → 修法

| # | smell | 修法 |
|---|---|---|
| 1 | Gameplay 生成 Presentation | LevelLoader 砍掉 prefab/Instantiate；新 `LevelActorBinder`（Presentation）订阅事件生成+清理 actor；manager 不再持有/销毁 Transform |
| 2 | Service Locator 满天飞 | manager 层全部构造注入（bus/game/worlds）；**仅场景组件**（TitleScreenUI/LevelBootstrap/ActorBinder）允许 Services 取用——Unity 组件无法构造注入的现实妥协 |
| 3 | manager 传参互调 + 越界销毁 | LevelManager ctor 注入 WorldManager；表现清理归 ActorBinder |
| 4 | 状态驱动权错位 | Loading→Playing 由 LevelManager 装载完成后推进；LevelBootstrap 不再碰 TrySetState |

## 3. 变量注册表（改动/新增）

| 名称 | 变化 | 归属 | 锚点 |
|---|---|---|---|
| `GameManager(GameManager ctor)` | 恢复 `ctor(EventBus bus)`，Publish 走注入 | Core | 本文档 |
| `LevelLoadedEvent` | 新 readonly struct {WorldId, World}，**定义在 Gameplay**（引用 WorldGrid，避免 Core→Gameplay 反向依赖） | Gameplay | 本文档 |
| `LevelLoader.Load(worlds, worldId, data)` | 砍 prefab/parent 参数，纯数据 | Gameplay | 本文档 |
| `LevelManager(WorldManager, GameManager, EventBus)` | ctor 注入；Load(worldId, data)；发事件；推进 Playing；Unload 只清数据 | Gameplay | 本文档 |
| `LevelActorBinder` | 新 MonoBehaviour（Presentation）：拖 prefab，订阅 LevelLoadedEvent 生成/清空 actor | Presentation | 本文档 |
| `LevelBootstrap` | 字段砍掉 _cellActorPrefab；只做：隐藏 Tilemap + Loading 时调 LevelManager.Load | Gameplay | 本文档 |
| `CoreDriver` 装配 | ctor 注入链 + LevelManager 注册 | Core | 本文档 |
| `LevelGallery` | 移到 Presentation（namespace+目录），自生 actor（陈列工具本身是表现侧工具） | Presentation | 本文档 |

## 4. 非目标

Services 门面本身保留（场景组件取用入口）；EventBus/GameState 机制不动；LevelGallery 功能不变。

## 5. 验收

| # | 验证点 | 预期 |
|---|---|---|
| 1 | 编译 | 零红字 |
| 2 | 全链 | 标题→开始→装载→Playing 行为与重构前一致 |
| 3 | 场景 | LevelBootstrap 的 prefab 引用槽消失（正常）；场景需新挂 LevelActorBinder（拖 prefab）——人工步骤见会话输出 |
| 4 | 陈列场景 | LevelGallery 照常并排装载 |
