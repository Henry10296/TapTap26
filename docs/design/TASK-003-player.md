# Design：TASK-003 角色模块（Player）

> 模板：`.claude/templates/design-template.md`。计划审批记录：2026-10-08（plan mode 批准 + 两项拍板：新 Input System / Hero 静态单帧）。
> 铁律：本文变量注册表之外，Coding 不准创造。

## 1. 任务

- Task ID / 日期：TASK-003 / 2026-10-08
- 一句话目标（Intake 拍板记录）：用户指令「先把角色这里开发完」——Play Mode 下角色可移动/跳跃/瞄准/投掷，`SeedThrownEvent` 发出并被 mock 自证；M1（魔法回路）顺延。
- 对标：GDD M2「角色运动」+ tech-spec §2 Player。

## 2. 目标 / 非目标

- 目标：见上；库存随投掷扣减；场景一键搭建工具。
- 非目标：Magic Circuit 真实实现（M1）；角色动画（Presentation/Character，静态单帧拍板）；WorldGrid/Cell；UI；GameManager 状态机；asmdef；配置表迁移；不手改 SampleScene.unity。

## 3. 涉及模块与边界

| 模块 | 读/写 | 说明 |
|---|---|---|
| Gameplay/Player | 写 | playerInput + playerActor + 运动/库存/投掷 |
| Core/Event | 写 | 最小 EventBus + SeedThrownEvent 契约（铁律 #3） |
| Core/Tick | 暂不建 | 单用户不造抽象；Player 挂 Update，M1 多系统时收口（拍板：计划 OQ#1 采纳推荐） |
| MagicCircuit | 写（mock） | 仅 MockSeedThrownLogger = M1 handoff 对接点 |
| Cell/Level/Presentation/Data | 不碰 | 越界不代劳 |

## 4. 设计

### 4.1 架构 / 流程

```
PlayerInputReader（新 Input System，InputSystem_Actions 的 Player map）
  Move(已有 WASD) / Jump(已有 Space) / Throw(新增 鼠标左键) / Aim(新增 <Mouse>/position)
    → PlayerMotor：Rigidbody2D.linearVelocity 移动 + 跳跃；地面检测 OverlapBox + Ground 层
    → PlayerThrower：Aim → ScreenToWorldPoint → 方向；TryConsume 库存 → EventBus.Publish(SeedThrownEvent)
        → MockSeedThrownLogger：Console 日志自证（M1 handoff 点）
PlayerActor：Awake GetComponent 注册四组件
```

- 事件契约事实修正：Player map 已有 `Attack`（绑鼠标左键）与 `Look`（绑 `<Pointer>/delta`）——语义均不符投掷/瞄准，不复用；新增 Throw/Aim 两个 action。
- 素材事实修正：PNG 为**双后缀** `Platformer Hero Idle-1.png.png`（Glob 实测），脚本路径按此写死；文件名清理记 handoff 不代劳。

### 4.2 变量注册表

| 名称 | 类型 | 归属模块 | 用途 | Confidence | Validation-State | 证据锚点 |
|---|---|---|---|---|---|---|
| `TapCom.Core.EventBus` | **实例 class**（Publish 快照遍历 + 单 handler 异常隔离，沿用用户加固实现）；Subscribe/Unsubscribe/Publish | Core/Event | 低耦合通信；注册进容器随 Play 轮次重建，无跨轮回残留 | High | Verified（2026-10-08 用户拍板：实例 + Services 门面，推翻早前 static 记录） | tech-spec §1 Event + AskUserQuestion 选项 |
| `TapCom.Core.Services` | static 门面：`Container` / `EventBus` accessor / Initialize / Shutdown | Core/Event | 服务获取统一入口；CoreDriver 装配、OnDestroy 时 Shutdown | High | Verified（同上拍板） | 同上 |
| `SeedThrownEvent` | struct {Type, Origin(Vector2), Direction(Vector2)} | Core/Event | 投掷请求载荷 | High | Verified | tech-spec §9 |
| `SeedMagicType` | enum {Blue,Red,Yellow} | Core/Event（随契约，避免 Core→Gameplay 反向依赖；M1 迁 Data 再议） | 种子魔法属性 | High | Verified | tech-spec §4 规则2 |
| `PlayerActor` | MonoBehaviour；属性 InputReader/Motor/Inventory/Thrower | Gameplay/Player | 组件容器 | High | Verified | tech-spec §2 |
| `PlayerInputReader` | MonoBehaviour；MoveInput/JumpPressed/ThrowPressed/AimScreenPosition | Gameplay/Player | 输入采集；类名避让 UnityEngine.InputSystem.PlayerInput | High | Verified + 命名为本设计新增 | tech-spec §2 |
| `PlayerMotor` | moveSpeed=5 / jumpVelocity=8 / groundMask / 地面检测尺寸 全 SerializeField | Gameplay/Player | 移动+跳跃 | High（机制）/ Med（数值） | 机制 Verified；数值 Inference（Play 调） | tech-spec §2 |
| `PlayerInventory` | blue=2/red=2/yellow=4 SerializeField；GetCount/TryConsume | Gameplay/Player | 种子库存 | High | Verified | tech-spec §2 增补（截图 蓝2红2黄4） |
| `PlayerThrower` | currentSeedType=Blue（M2 单类型，切换非目标） | Gameplay/Player | 瞄准+扣库存+发请求 | High | Verified | tech-spec §2 |
| `TapCom.MagicCircuit.MockSeedThrownLogger` | MonoBehaviour；OnEnable 捕获 `Services.EventBus` 引用再订阅（防 teardown 期访问已 Shutdown 容器） | MagicCircuit（mock） | 订阅日志 | Med | Inference（本任务新增，M1 替换） | — |
| `TapCom.EditorTools.PlayerTestSceneSetup` | Editor 菜单 `Tools/角色/搭建M2测试场景` | Editor | 建层/地面/挂件/贴图导入 | Med | Inference（Unity 6 实测为准）；**顺延**（用户：先不要做物体） | — |
| `Rigidbody2D.linearVelocity` | Unity 6 API | PlayerMotor | 速度驱动 | Med | **Inference**（6000.0 应有；编译错则回退 velocity） | Unity 6 changelog |
| 命名空间根 `TapCom.*` | Core/Gameplay/MagicCircuit/EditorTools → TapCom.{Core,Gameplay,MagicCircuit,EditorTools} | 全部 | 2026-10-08 用户实时裁决；并行 TASK-004 的 MagicCircuit.* 裁决同时作废，全树统一 | High | Verified（用户 AskUserQuestion 选项） | active_task 证据链 |
| `TapCom.Presentation.PlayerCharacter` | MonoBehaviour（RequireComponent Animator） | Presentation/Character | 动画状态 + 朝向翻转；只读 Motor 状态（铁律 #6）；依赖方向 Presentation→Gameplay 单向，PlayerActor 不反向登记 | High | 新建（2026-10-09 增量） | tech-spec §6 Character |
| `PlayerMotor.Velocity` | Vector2 只读属性 | Gameplay/Player | 表现层读速度用 | High | 新建（2026-10-09 增量） | — |
| `Player.controller` 参数契约 | `Speed`(float)=\|横向速度\|；`Grounded`(bool)=脚底检测 | Arts/Test/Animation | 动画状态机驱动参数 | High | Verified（YAML 实读：原 0 参数 + 无条件 AnyState→Idle，已替换） | Player.controller |
| controller 迁移表 | AnyState→Jump(Grounded false, dur 0)；→Idle(Grounded true 且 Speed<0.1)；→Walk(Grounded true 且 Speed≥0.1)；CanTransitionToSelf=0 | Arts/Test/Animation | 三态互斥切换 | Med | **Verified**（2026-10-09 Unity 重序列化确认接受；场景链路四点查证全通：Animator 挂 Player / Controller 绑定 / PlayerCharacter 挂载 / 迁移参数完好） | Player.controller + SampleScene.unity grep |
| `TapCom.Presentation.PlayerCameraRig` | MonoBehaviour（挂 Main Camera）；跟随（指数平滑，z 锁定）+ 动态缩放（移动→_moveOrthoSize 放大，静止→_idleOrthoSize 拉回，阈值 _moveSpeedThreshold=0.1 判定 Motor.Velocity.magnitude）；Target 留空按名字兜底找 "Player" | Presentation/Camera | tech-spec §6 Camera：玩家视角；只读状态做表现（铁律 #6） | High | 新建（2026-10-09 增量）；数值待 Play 调 | — |
| InputSystem_Actions Player map 增 `Throw`/`Aim` | JSON asset 编辑 | Core 输入资产 | 投掷/瞄准绑定 | High | Verified（asset 实读：Move/Jump/Attack/Look 现状） | Assets/InputSystem_Actions.inputactions |

### 4.3 契约（事件 / 公共接口）

| 名称 | 签名 | 发布方 → 订阅方 | 语义 |
|---|---|---|---|
| `SeedThrownEvent` | (SeedMagicType Type, Vector2 Origin, Vector2 Direction) | PlayerThrower → MockSeedThrownLogger（M1: MagicCircuit） | 一次投掷请求；落点/连接/效果全下游负责 |
| `EventBus`（实例，经 `Services.EventBus`） | `Subscribe<T>(Action<T>)` / `Unsubscribe<T>` / `Publish<T>(T)` | 各系统 | 无线程/优先级；总线随容器在 Play 轮次重建（替代原 Clear 方案）；Publish 中订阅/退订安全 |

### 4.4 数据表

不触配置表管线（Player 参数 SerializeField；迁移 OQ#2）。

### 4.5 Invariants

- 铁律 #3/#4：Player 只发 SeedThrownEvent，无落点/连接/效果计算；mock 不反向改 Player。
- 铁律 #6：零动画/特效/UI 代码；SpriteRenderer 静态单帧。
- 铁律 #11：Rigidbody2D freezeRotation、gameplay z=0。
- 目录：运行时 `Assets/Script/{Core,Gameplay,MagicCircuit}/`，Editor `Assets/Editor/`。

## 5. 测试计划（Play Mode 由用户执行）

| 验证点 | 方式 | 预期信号 |
|---|---|---|
| 1 一键搭建 | 菜单 `Tools/角色/搭建M2测试场景` | Console 汇总报告：Ground 层/地面/挂件/贴图，无红字 |
| 2 移动 | A/D | 左右位移，松开即停无漂移 |
| 3 跳跃 | Space | 起跳回落；空中连按不二段跳 |
| 4 投掷 | 鼠标左键 | `[MockSeedThrown] type=Blue …`；`[PlayerInventory] Blue 2→1` |
| 5 方向 | 鼠标左/右侧各投 | dir.x 符号随侧翻转 |
| 6 空库存 | Blue 打空再按 | `[PlayerInventory] 拒绝` 日志，无 MockSeedThrown 日志 |
| 7 资产改动回读 | inputactions 手改后回 Unity | 无导入报错（失败回退 git checkout 该文件） |

## 6. 风险与回退

- .inputactions JSON 手改格式错 → `git checkout` 回退；备选新建专用 asset。
- Hero PNG 非 Sprite 导入模式 → setup 脚本强制 TextureImporter（sprite/single + reimport）。
- linearVelocity 若 6000.0.53f1 不存在 → 回退 `rb.velocity`（编译错即改）。
- Editor 建 Ground 层走 TagManager SerializedObject 标准写法 → 首跑验收 #1 为准，失败即修。
- 相机 tag 非 MainCamera → setup 内校验修正并报告（OQ#3）。

## 7. Open Questions

1. ~~TickManager 本轮建否~~ → 拍板：不建（单用户不造抽象，M1 收口）。
2. Player 参数迁 CSV：M1/M3 统一时定。
3. Camera.main tag 校验已进 setup 脚本；模板相机实际 tag 待 Play 验证。
4. ~~命名空间根~~ → **拍板 `TapCom.*`**（2026-10-08 用户实时裁决；本文早前 MagicCircuit.* 记录作废。并行 TASK-004 会话曾裁 MagicCircuit.*，以用户裁决为准）。
5. [新] PNG 双后缀（`*.png.png`）命名清理——记 handoff，本轮不碰（改名会断引用）。
6. [新] M2 后续：种子类型切换键（1/2/3）、投掷轨迹预览（UI，§10-#10）——不在本轮。

## 8. 任务拆解（Task）

| # | 任务 | 改哪些文件 | 验收 |
|---|---|---|---|
| 1 | Design 落盘 + active_task 立项 | 本文件、active_task.md | 完成 |
| 2 | Core 事件层 | Script/Core/EventBus.cs、GameEvents.cs | 编译通过 |
| 3 | Player 四组件 | Script/Gameplay/Player/*.cs ×4 | 编译通过，组件只经 PlayerActor/EventBus 交互 |
| 4 | mock | Script/MagicCircuit/MockSeedThrownLogger.cs | 编译通过，订阅 SeedThrown |
| 5 | 输入资产 + Editor 工具 | InputSystem_Actions.inputactions、Editor/PlayerTestSceneSetup.cs | 验收 #1 |
| 6 | 用户 Play Mode 验收 | — | 验收 #2-#6，结果回写 active_task |
| 7 | M1 handoff | active_task.md | SeedThrownEvent 契约 + mock 替换点写清 |
