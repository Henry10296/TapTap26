# Design — TASK-008：游戏初始化与开始界面

> 用户指令（2026-10-10）：「先把游戏初始化和开始界面做出来」。上游依据：tech-spec §1（GameState + TitleScreen 增补）、§6 UI。
> M1（TASK-007 Magic Circuit）挂起，本任务先行。

## 1. 流程（目标态）

```
启动 → CoreDriver 装配（BeforeSceneLoad）→ GameState: Initializing → TitleScreen
     → TitleScreenUI 自动构建标题画面（AfterSceneLoad，零场景配置）
     → 玩家点「开始游戏」→ TitleScreen → Loading
     → LevelBootstrap（订阅 Loading 进入）装载 LevelData → GameState: Playing（标题面板隐藏）
```

## 2. 决策

| 决策 | 内容 | 理由 |
|---|---|---|
| GameState 增补 `TitleScreen` | 枚举 + 白名单：Initializing→[TitleScreen,Loading]，TitleScreen→[Loading] | 标题是生命周期一等状态；tech-spec §1 已增补登记 |
| 同场景覆盖式标题（非多场景流转） | 标题 = 全屏 Panel 覆盖，点开始后隐藏 | 单场景现状（关卡编辑场景即游戏场景）最小改动；多场景流转归 M3 Level |
| ~~UI 代码构建~~ → **v2 作废（2026-10-10 用户纠偏「我自己做场景」）**：TitleScreenUI 改场景挂载控制器（Panel/开始按钮/退出按钮三引用），场景用户手搭，自动装配器删除 | 见 §6 | 用户自制场景资产 |
| Legacy Text（非 TMP） | 占位字体 LegacyRuntime.ttf | TMP 需 Essentials 导入前置，占位期少一个失败源 |
| LevelBootstrap 改事件驱动 | Start 只隐藏 Tilemap + 订阅；Loading 进入时装载 → 转 Playing | 装载时机从「场景加载即玩」改为「玩家点开始」，与状态机闭环 |
| 退出按钮 | Editor 里 isPlaying=false / 打包后 Application.Quit | 标准配置 |

## 3. 变量注册表（新增/改动）

| 名称 | 类型 | 归属 | Confidence | 锚点 |
|---|---|---|---|---|
| `GameState.TitleScreen` | 枚举新值 | Core | High | 本文档（tech-spec §1 增补） |
| AllowedTransitions 新增两条 | Initializing→[TitleScreen,Loading]、TitleScreen→[Loading] | Core | High | 本文档 |
| `TitleScreenBootstrap.Install()` | RuntimeInitializeOnLoadMethod(AfterSceneLoad) | Presentation | High | 本文档 |
| `TitleScreenUI` | MonoBehaviour（自建 Canvas 1920×1080 ScaleWithScreenSize + EventSystem[InputSystemUIInputModule]） | Presentation | High | 本文档 |
| LevelBootstrap.OnStateChanged | Loading 进入时装载一次（_loaded 防重入）+ 转 Playing | Gameplay | High | 本文档 |

## 4. 非目标

多场景流转（Boot/Title/Game 分场景）、读档、设置界面、暂停菜单——M3/M4；正式美术 UI——M4。

## 5. 验收

| # | 验证点 | 预期信号 |
|---|---|---|
| 1 | 进 Play | 标题画面出现（深底 + 「魔法回路」+ 两按钮）；场景画面被覆盖 |
| 2 | Console | `GameState: Initializing -> TitleScreen`（不再有旧直通链两条） |
| 3 | 点「开始游戏」 | 标题隐藏 + `TitleScreen -> Loading` → Tile 消失/CellActor 出现 + `Loading -> Playing` |
| 4 | 点「退出游戏」 | 退出 Play |
| 5 | F1 调试键 | 标题下按 F1 仍拒绝（TitleScreen -> Initializing rejected） |
| 6 | 编译 | 零红字 |

## 6. v2 修订（2026-10-10）

- 用户拍板「我自己做场景，你只告诉我怎么做」→ 删 `TitleScreenBootstrap` 自动装配；`TitleScreenUI` 重写为场景组件：`[SerializeField] _panel/_startButton/_quitButton`，OnEnable 订阅+接按钮，Start 按 Current==TitleScreen 补显示。
- 编译错误修复（用户报 5 个 CS 错，本会话代码缺陷）：EventBus 现为实例类，LevelBootstrap/TitleScreenUI 里 4 处静态调用改 `Services.EventBus.*`；LevelBootstrap 1 处误用 `WorldManager.TrySetState` 改取 `GameManager`。
- 场景搭建教程口述给用户（Canvas/Scaler/Panel/两个 Legacy Button/挂脚本拖引用/EventSystem 换 InputSystemUIInputModule）。
