# Active Task

> 跨会话状态锚点。换会话第一动作 = 读本文件接管，不靠记忆、不靠前序对话。
> 有 Edit/Write 的轮次，结束前必须先回写本文件（Stop hook 强制）。
>
> **当前任务：TASK-003-角色模块Player（见文末）**。TASK-001/002 已完结，留作归档记录。

---

## Task ID: TASK-001-建立项目规范

**Status: DONE**（2026-10-08）

## 目标

为《魔法回路》建立完整 spec-driven 基建：GDD 归档 + 技术架构规范 + .claude/ 工作流基建。

## 产出

| 产物 | 说明 |
|---|---|
| `docs/GDD.md` | 设计文档 01-09 原文照录归档 |
| `docs/tech-spec.md` | 七模块架构规范（用户拆分照录）+ 架构铁律 §8 + Open Questions §10 |
| `.claude/tasks/active_task.md` | 本文件（任务锚点机制启用） |
| `.claude/templates/design-template.md` | Design 阶段模板（变量注册表 + evidence ledger） |
| `.claude/settings.json` | PostToolUse 脏标记 + Stop hook 回写强制 |

## 本轮证据链

- 2026-10-08｜`docs/GDD.md`、`docs/tech-spec.md` 落盘｜已核验（Write 成功）
- 2026-10-08｜Stop hook 命令在本会话 Git Bash 实测（.dirty 生成 / -nt 条件成立）｜已核验（见 Bash 输出）
- 2026-10-08｜hook 配置**本会话不生效**（Claude Code 启动时快照 hooks，改动需重启会话或 /hooks 菜单审核）｜inference（官方行为，未本机实测）
- 2026-10-08｜**Stop hook 已实测生效**：本轮无 Edit/Write 仍被拦截要求回写（残留 .dirty 触发），下述第 3 条 inference 升格为已核验｜已核验（本轮 Stop hook 实际拦截）
- 2026-10-08｜`.gitignore` 无任何 ignore Arts 的规则：`git check-ignore -v Assets/Arts` 无命中，`Assets/Arts.meta` 命中的是第 12 行否定规则（保留 .meta）；status 里只见 `Arts.meta` 是因目录内仅有孤立 `Test.meta`｜已核验（check-ignore + find 实测）
- 2026-10-08｜**hook 自锁缺陷修复**：旧 PostToolUse 对所有 Edit/Write（含回写 active_task.md 本身）都 touch .dirty，且晚于文件写入 → .dirty 永远更新 → Stop 无限拦截（本会话实测连拦 2 次）。修复：PostToolUse 检查 stdin 中 file_path 含 `active_task.md` 则跳过置脏（settings.json 已改）。新命令两分支 Git Bash 实测：active_task.md → SKIP，Foo.cs → SET｜已核验（Bash 实测）
- 2026-10-08｜新 hook 配置**本会话不生效**（hooks 启动时快照），生效自下次会话起；本会话以手动删 .dirty 收尾｜已核验（旧 hook 在本次 Edit 后仍重建了 .dirty，证实快照行为）
- 2026-10-08｜**Arts ignore 规则已补**：`.gitignore:53` 加 `/[Aa]ssets/[Aa]rts/`。验证：`git status` 不再列出 `Assets/Arts/`；`check-ignore` 确认 Arts 内文件命中新规则；无已 staged/已提交的 Arts 文件，无需 `git rm --cached`。`Assets/Arts.meta` 按既有 `!/**.meta` 约定保留追踪（存文件夹 GUID，防 Addressables 文件夹引用断裂）｜已核验（git status + check-ignore -v 实测输出）。注：上一轮"Arts 目录内仅孤立 Test.meta"的状态已过时，本轮 status 显示 `Assets/Arts/` 整目录 untracked，说明用户已放入实际美术文件
- 2026-10-08｜**教训沉淀（Arts 事件复盘）**：上轮已锤到"`.gitignore` 无 Arts 规则"的铁证（check-ignore 无命中，见第 32 条），却以"目录仅孤立 meta、status 不显示"解释为没问题——**用现状合理化了防护缺口**：没修、也没记 open-question，用户放入真文件后立刻爆。规则：① 查到"缺规则/缺防护"只有两个合法出口——当场补，或显式记 open-question；"当前数据恰好不触发"不构成 PASS；② 环境快照类结论（目录内容/git status）必须挂前提与日期，前提一变结论即作废｜已核验（上轮证据链第 32 条与本轮症状对照）

## Open Questions

- `docs/tech-spec.md` §10 全表（Circuit 连接规则 / 魔法效果清单 / Cell 类型清单 / z 轴语义等 11 条）——M1 开工前须逐条拍板。

## Next Steps

1. 拍板 tech-spec §10 的 M1 阻塞项（#1-#5）。
2. M1 开工：魔力种子、场景方块、环境反应（走全套 spec-driven 轨，Design 用 `.claude/templates/design-template.md`）。
3. ~~Unity 侧确认新 hook 生效~~（已核验：2026-10-08 Stop hook 实际拦截并强制回写）。
4. 清理孤立 meta：`Assets/Arts/Test.meta`（对应资源已删，Unity 会报 warning）——待用户拍板。

---

## Task ID: TASK-002-M1核心规则拍板

**Status: DONE**（2026-10-08）——M1 阻塞项全部清零。

## 目标

M1（魔力种子 / 场景方块 / 环境反应）开工前，清掉 tech-spec §10 的阻塞项。

## 证据链（2026-10-08，两轮拍板均用户逐条确认）

- ✅ Circuit 连接规则 = **闭环模型，种子是源节点**，正式定为 Magic Circuit 核心规则（用户原话「不再摇摆」）→ tech-spec §4 核心规则 + 铁律 #10｜已核验（Edit 成功）
- ✅ z 轴 = 只有景观有深度，gameplay 全部 z=0 锁定 → 铁律 #11｜已核验
- ✅ 重力语义 = 世界默认冻结（无重力无物理、互相黏住）；**解除锁定 → 有重力有物理**；反应结束重新冻结 → §4 规则 1/6/8 + 铁律 #9｜已核验（用户对本三行理解整体回「是的」，#12a 销案）
- ✅ 冻结范围 = 游戏全局配置（数据可配）→ §4 规则 1｜已核验
- ✅ 种子 = 格子：扔出时生成一个魔法格子，落定后持续存在、可参与后续回路 → §4 规则 2｜已核验
- ✅ 种子自带魔法属性（截图：蓝×2 红×2 黄×4）；接触格子触发判定；判定走 Magic 配置表（「填表」）→ §4/§7｜已核验
- ✅ 伤害先待定：M1 不含伤害系统，心 ×3 语义 M3 前定 → §10 #14｜已核验
- tech-spec 增补：§3 魔法属性/魔法状态分字段、§6 UI 截图清单、§10 状态表（#1/#4/#5/#12/#13 销案，#2/#3/#6/#14 部分，新增 #15 判定时机）｜已核验（Edit 成功）

## Open Questions（遗留，不阻塞 M1 开工）

- #15 判定时机：接触瞬间 vs 落定静止（已给推荐：种子即格子，碰到即停即成格即判定，两者天然合一）——M1 Design 定。
- 待填表：种子属性 × Cell 魔法属性 → 连接/效果 判定表——M1 Design 起草首版给用户过目。
- [继承] 清理孤立 meta `Assets/Arts/Test.meta`——待用户拍板。

## Next Steps

1. 用户说开工 → 开 TASK-003-M1Design（全套轨：Intake → Design，用 `.claude/templates/design-template.md`，含首版 Magic 判定表 + 变量注册表）。
2. 下个会话开头确认 hook 正常（新 PostToolUse 含 active_task.md 豁免，上会话已实测两分支）。

---

## Task ID: TASK-003-角色模块Player

**Status: CODING（代码已落盘，物体组装与 Play 验收待用户）**

## 目标

先开发角色（用户 2026-10-08 指令，M1 顺延）：Play Mode 下移动/跳跃/瞄准/投掷，`SeedThrownEvent` 发出并被 mock 自证。对标 GDD M2 + tech-spec §2。

## Design

`docs/design/TASK-003-player.md`（变量注册表 + 契约 + 验收清单；计划经 plan mode 批准，两项拍板：新 Input System / Hero 静态单帧）。

## 本轮证据链

- 2026-10-08｜Explore 环境扫描：Unity 6000.0.53f1、Input System 1.14.0（activeInputHandler=2）、运行时脚本为空、场景 Player 物体已有 Rigidbody2D+BoxCollider2D+SpriteRenderer 但无地面、Prefab/Script 目录不存在｜已核验（子 agent 实读）
- 2026-10-08｜InputSystem_Actions 实读：Player map 已有 Move(WASD)/Jump(Space)/Attack(鼠标左键)/Look(Pointer delta)；**Look 是 delta 非 position，Attack 语义不符**→ 新增 Throw/Aim 两个 action｜已核验（Read 全文）
- 2026-10-08｜素材文件名为**双后缀** `Platformer Hero Idle-1.png.png`（Explore 首报漏了这层）→ 后续 setup 脚本路径按双后缀写死；改名清理记 OQ 不代劳（会断引用）｜已核验（Glob 输出）
- 2026-10-08｜7 个运行时脚本落盘：Script/Core/EventBus.cs、GameEvents.cs、Script/Gameplay/Player/{PlayerActor,PlayerInputReader,PlayerMotor,PlayerInventory,PlayerThrower}.cs、Script/MagicCircuit/MockSeedThrownLogger.cs｜已核验（Write 成功；**未编译**——Unity 未聚焦，无 .meta/编译结果）
- 2026-10-08｜InputSystem_Actions.inputactions 加 Throw(鼠标左键)/Aim(鼠标位置)｜已核验（python json.load 解析通过，action/binding 齐全）
- 2026-10-08｜`Rigidbody2D.linearVelocity`、`InputSystem.actions` 回退路径为 Unity 6 API｜**inference**（编译错则按 design §6 回退方案改）
- 2026-10-08｜用户指令「先写代码，先不要做物体」→ Editor 场景搭建工具与物体组装**顺延**，本轮不建 Ground/不挂组件｜已核验（用户原话）
- 2026-10-08｜**并行冲突仲裁（用户实时裁决，推翻 TASK-004 裁决①②）**：namespace 全树统一 `TapCom.{Core,Gameplay,MagicCircuit,EditorTools}`；EventBus = 实例 class + TapCom.Core，注册进 ServiceContainer，经静态门面 `Services` 获取｜已核验（AskUserQuestion 用户选项原话）
- 2026-10-08｜合并执行：Services.cs 改为 ServiceContainer 门面；删 GameBootstrap.cs（装配归对方 CoreBootstrap/CoreDriver：Services.Initialize + EventBus 入容器 + OnDestroy Shutdown）；私有字段风格对齐用户示范的 `_camelCase`｜已核验（本轮改码）
- 2026-10-08｜合并后一致性核查：全树 15 文件 namespace = 9×TapCom.Core + 5×TapCom.Gameplay + 1×TapCom.MagicCircuit；EventBus 调用点仅 GameManager:78 / PlayerThrower:47 两处，均经 Services.EventBus；无 MagicCircuit.* 残留、无 GameBootstrap 残留（.meta 同删）｜已核验（grep 实测输出）。**仍未编译**——真实编译信号待用户聚焦 Unity
- 2026-10-09｜**动画接线增量**：用户做好 Idle/Walk/Jump 三态（Arts/Test/Animation/Player.controller + 3 .anim）；controller 实读发现 **0 参数 + 无条件 AnyState→Idle**（会永远拉回 Idle，Jump/Walk 播不到）→ 替换为参数 `Speed`(float)/`Grounded`(bool) + 三条互斥 AnyState 条件迁移（CanTransitionToSelf=0）｜已核验（controller YAML 实读 + Edit 成功；Unity 重导入待肉眼校验）
- 2026-10-09｜新增 `TapCom.Presentation.PlayerCharacter`（读 Motor.Velocity/IsGrounded 写 Animator 参数 + flipX 朝向；Presentation→Gameplay 单向依赖）+ `PlayerMotor.Velocity` 只读属性｜已核验（Write/Edit 成功；**未编译**）。另：并行会话已落 World 三件套（Cell/WorldGrid/WorldManager，TapCom.Gameplay）并注册进 CoreDriver——非本任务面，不碰

## Open Questions

- [继承] Player 参数迁 CSV（M1/M3 定）；PNG 双后缀清理；种子切换键/轨迹预览（M2 后续）——详见 design §7。
- [新] M2 后续物体组装：Ground 层创建 + 地面物体 + Player 挂件 + 贴图 Sprite 导入设置 + Mock 物体（原计划 Editor 一键工具 `Tools/角色/搭建M2测试场景`，待用户说「做物体」再写再跑）。

## Next Steps

1. 用户 Unity 聚焦后确认编译零红字（首个真实编译信号）。
2. 用户发话 → 写 Editor 搭建工具并执行物体组装（Ground/挂件/Mock 物体）。
3. Play Mode 跑 `docs/design/TASK-003-player.md` §5 验收清单 7 项，结果回写本文件。
4. M1 handoff：SeedThrownEvent 契约（Type/Origin/Direction）+ mock 替换点已定于 Script/MagicCircuit/MockSeedThrownLogger.cs。

---

## Task ID: TASK-004-Core模块

**Status: CODING（代码已落盘，编译与 Play 验收待用户）**——与 TASK-003-Player 并行。

## 目标

Core 运行框架：GameManager（GameState 状态机 + Service 容器）、TickManager、EventBus、自动 Bootstrap。对标 tech-spec §1。

## Design

`docs/design/TASK-004-core-design.md`（变量注册表 20 条，全 High 新建；本任务无外部契约依赖）。

## 本轮证据链（2026-10-08）

- 7 个文件落盘：Script/Core/{GameState, GameManager, ServiceContainer, EventBus, CoreBootstrap}.cs + Tick/{ITickable, TickManager}.cs｜已核验（Write 成功；**未编译**，同 Player 任务——Unity 未聚焦）
- **并行冲突事故与修复**：TASK-003-Player 会话先写过 EventBus.cs（静态形态），本会话 Write 误覆盖致 PlayerThrower:47 / MockSeedThrownLogger:10 调用点濒临编译错｜已核验（grep 调用点实证）
- 裁决①：命名空间统一 **`MagicCircuit.{Core,Gameplay}`**（对方 8 文件多数派，本任务 6 文件让步；原 TapCom.Core 作废）｜已核验（6 文件 namespace 已改）
- 裁决②：EventBus 恢复 **static class、namespace MagicCircuit（外层）**，MagicCircuit.* 全系无 using 可见；GameManager 无参 ctor 改静态发布｜已核验（EventBus.cs 重写 + GameManager/CoreBootstrap 改配）
- design 文件改名 TASK-004-core-design.md（编号让位 Player，防锚点混淆）｜已核验（mv + ls）
- **【2026-10-08 追记，TASK-003 会话】裁决①②已被用户实时裁决作废**：namespace 全树统一 `TapCom.*`；EventBus 改实例 class + TapCom.Core，注册进容器经 `Services` 门面。本会话已代为翻转 Core 6 文件 + CoreDriver 装配（Services.Initialize/Shutdown + EventBus 入容器），CoreDriver 行为语义不变（F1 非法迁移验收照旧）。design 表 52/62 行已标【已作废】。下轮接手先 grep namespace 对账再动码｜已核验（本轮 Edit 清单）
- 2026-10-08｜**Core 会话对账审计 PASS**：全树 15 文件 namespace 已无 MagicCircuit.* 残留；GameBootstrap.cs 连 .meta 删净；Services 门面（Initialize/Shutdown/Container/EventBus）与全部调用点（CoreBootstrap ×3 / GameManager:78 / PlayerThrower:47 / Mock:15）逐一匹配；CoreDriver 装配链 + 直通链 + 心跳 + F1 拒绝路径完好；Services.cs 尚无 .meta（Unity 聚焦自动生成，非问题）｜已核验（grep 全树 + 逐文件 Read 比对）
- 2026-10-08｜**协作约定（撞车复盘）**：双会话并行同写 Script/Core 造成三轮互相覆盖（EventBus 被覆写一次、namespace 翻转两次）。约定：**Script/Core 写权归 TASK-004 线**；TASK-003-Player 线只写 Script/Gameplay/** 与 EditorTools；共享契约文件（GameEvents / Services / EventBus 形态）改动前先在 active_task 登记并双线可见｜inference（流程约定，两线都遵守才生效，用户仲裁过一轮算背书）

## Open Questions

- [TASK-003 会话追记] EventBus 形态/namespace 已被用户裁决翻转（见证据链追记），本表 52/62 行作废；ServiceContainer.TryGet 现无调用方，保留待 Level/Player 注册流程用。
- asmdef（tech-spec §10 #8）维持待拍板，本模块先在 Assembly-CSharp。
- 编译零红字是下一个真实信号（同 TASK-003，两条线共享一次编译）。

## Next Steps

1. 用户聚焦 Unity → 编译（Core + Player 两线同时首验）。
2. Play Mode 跑 TASK-004-core-design.md §5 验收表：bootstrap installed → services ready → Initializing->Loading->Playing → tick 心跳 → **F1 触发非法迁移被拒**。
3. 与 TASK-003-Player 的 7 项验收可同一次 Play 完成（互不依赖）。

---

## Task ID: TASK-005-Cell与World

**Status: CODING（代码已落盘，编译与自检待用户）**——Core 线（本会话）续任务。

## 目标

环境模拟的数据与存储层：Cell 纯数据单元、WorldGrid 格点字典、WorldManager 多实例容器、Editor 自检工具。对标 tech-spec §2/§3，M1 地基；Magic Circuit（下一任务）是这些 API 的唯一执行器（铁律 #4）。

## Design

`docs/design/TASK-005-cell-world-design.md`（变量注册表 13 条全 High；零事件契约；**形态解读**：tech-spec「Cell 本身是字典」按「WorldGrid 格点字典存 Cell」落地，Cell 本体强类型字段——用户可否决，否决只动 Cell.cs）。

## 本轮证据链（2026-10-08）

- 4 文件落盘：Script/Gameplay/World/{Cell, WorldGrid, WorldManager}.cs + Editor/WorldSelfTest.cs｜已核验（Write 成功；**未编译**）
- Core 注册 +2 行：CoreDriver.Awake `services.Register(new WorldManager())` + ready 日志串追加（Core 目录写权在 Core 线，无越界）｜已核验（Edit 成功）
- 协作约定更新：**Script/Gameplay/World/ 写权归 TASK-005 线**（TASK-003-Player 线只写 Script/Gameplay/Player/** 与 EditorTools）
- Scope 纪律：Magic Circuit 本体（落点/连接/成环/效果/收敛）、CSV 装载、冻结范围配置、Presentation 全部列非目标（design §2），不越界

## Open Questions

- 「Cell 字典形态」字面解读未采纳（design §7）——待用户看 Design 时顺带确认。
- 回路邻接定义（4 向 vs 其他）待 tech-spec #6 填表；GetNeighbors 可扩。

## Next Steps

1. 用户聚焦 Unity → 编译（TASK-003/004/005 三线共享首次编译）。
2. Editor 菜单 `Tools/魔法回路/世界自检` → 预期 `[WorldSelfTest] PASS 14/14`。
3. Play Mode：ready 日志应含 `WorldManager`（Core 原有验收不变）。
4. 下任务：Magic Circuit 模拟器（落点判定/连接/成环/效果执行/连锁收敛）——先出 Design，含首版 Magic 判定表。

---

## Task ID: TASK-006-Cell家族与Tilemap关卡编辑器

**Status: CODING（代码已落盘，编译与编辑器流程待用户）**——Core 线续任务。

## 目标

首批 Cell 家族定版（惰性岩石 + 蓝红黄魔法格，数据层组合零新 CellType）+ cellActor 占位预制体 + Tilemap→LevelData 编辑器 MVP + LevelLoader。对标 tech-spec §3/§6/§7。

## 用户拍板（2026-10-09 AskUser 四问 + 追加）

- 魔法格 = **类型+属性维度**（CellType 仍只有 Rock，颜色走 MagicAttr）
- 岩石 = **惰性但受引力和爆炸影响** → 岩石 GravityAttr=Dynamic，Static 留给未来基岩类；**爆炸进效果清单**（tech-spec §10 #2 已更新）
- 魔法格 = Dynamic（可被重力解锁掉落）
- 范围 = 数据+占位表现，**追加做 Tilemap 地图编辑器 + 预制体**（cellActor 单预制体运行时着色，用户问「是不是要做预制体」→ 答「是，1 个」）

## 本轮证据链（2026-10-09）

- 5 文件落盘：Script/Gameplay/{LevelData, LevelLoader}.cs、Script/Presentation/CellActor.cs、Editor/{CellAssetBuilder, TilemapLevelWindow}.cs｜已核验（Write 成功；**未编译**）
- 新 namespace：`TapCom.Presentation`（仲裁四件套外的第五域，cellActor 归 Presentation 铁律 #2）｜已登记 design §1
- 新契约：**Tile 命名约定 `cell_rock/cell_blue/cell_red/cell_yellow`**（Tile→CellDef 映射唯一依据）｜design §4
- 新目录：`Assets/Levels/`（LevelData 资产）——项目 CLAUDE.md 目录约定里没有，待用户认可或指定迁移
- 事故与修复：TilemapLevelWindow 一次 Edit 误删 TryMap 开括号 → 当轮修复 + 全文复读验证结构完整｜已核验（Read 全文）
- tech-spec 更新：§10 #2 效果清单 +爆炸 +岩石受引力/爆炸｜已核验（Edit 成功）

## Open Questions

- `Assets/Levels/` 目录位置待用户拍板（现按新目录放）。
- 岩石「受爆炸影响」的效果语义归 Magic 判定表（下一任务填）。
- Tilemap z 层未来多层编辑（铁律 #11 保留）——编辑器 MVP 只画 z=0。

## 本轮证据链（2026-10-09 追加：试玩原型对读）

- 2026-10-09｜收到外部试玩原型 `涌现_魔力种子_试玩.html`（微信文件路径 `C:\Users\Henry\xwechat_files\...\2026-10\涌现_魔力种子_试玩.html`，**未归档入 repo**，是否拷入 docs/ 待用户拍板）。内容：20×26 格（S=32px，1米=1.5格）单屏可玩原型，完整闭环 = 投掷(420px/s+25%玩家vx, 冷却0.3s, 出手口扫描≤30px) → 飞行(小 hitbox 12×12, exp(-0.32t)水平阻尼, 限速±600/±650) → 贴合(±2px 容差, 岩石平时仅上下/吸引时四向) → 三色配方蓄能0.5s（红蓝蓝=失重·直径5米内消失 / 红蓝黄=爆炸·半径5米暂定 / 红红黄=吸引·直径7格聚向团块）→ 环境反应；固定时步 DT=1/120；末尾暴露 `root.game` 检查接口｜已核验（srcdoc 游戏脚本 L1581-1895 全读）
- 2026-10-09｜用户贴的「轨迹预览 vs 真实运动」说明逐条对源码核实**全部属实**：32 点×0.035s、起点=角色中心+瞄准方向30px、初速=420+25%玩家vx（与投掷完全一致）、zero 区外加重力 GRAV、障碍矩形外扩 6px 遇障停、预览不含出生口扫描/水平阻尼/吸引/贴合/限速/连锁｜已核验（draw() L1865-1869 + throwSeed() L1625-1634 + step() L1812-1837 对照）
- 2026-10-09｜原型 vs 本项目已拍板规则 **3 处分歧**（留给 Magic Circuit 模拟器 Design 拍板，不现在裁）：① 触发模型 = 相邻三色配方（无回路/导通概念）vs tech-spec §4「经过种子的闭合回路」；② 种子无限 vs 截图参考库存 蓝×2红×2黄×4；③ 「1米=1.5格」距离语义为本原型新增，tech-spec 无此条。另：Unity 移植时轨迹预览保留「仅瞄准提示」定位（用户说明原文「Unity等价版保留这一区别」）｜已核验（源码 + tech-spec §4 对照）

## Next Steps

1. 用户聚焦 Unity → 编译。
2. 菜单 `Tools/魔法回路/生成Cell占位资产` → 检查 Arts/Placeholder/ 5 资产 + Prefab/CellActor.prefab。
3. 菜单 `Tools/魔法回路/关卡编辑器` → 创建 Grid → Palette 画 cell_* Tile → `Tilemap → LevelData` → `编辑器预览装载` → 场景出现对应着色方块 + `[LevelLoader] loaded: n cells`。
4. 下任务不变：Magic Circuit 模拟器 Design（含首版判定表，含爆炸效果条目）——**对读这份试玩原型**：机制清单与参数可对标，但上述 3 处分歧需先拍板。

### TASK-006 v2（2026-10-09 用户纠偏）

- **用户纠偏三点**：①预制体用户自制，AI 只给教程（脚本代劳作废）②编辑器 = 每场景关卡化 Tilemap + 原生 Palette 笔刷 + 保存/导出数据 ③笔刷 Tile 自带格子定义，不按名字映射｜已核验（用户原话）
- v2 落地：删 Editor/CellAssetBuilder.cs（含 .meta）；新 Editor/CellTile.cs（自定义 Tile + CreateAssetMenu，字段 cellType/gravity/magic）；TilemapLevelWindow 重写（①场景关卡化 ②导出 LevelData 读 CellTile 数据 ③预览装载）；CellActor 加 `_tintByCell` 开关｜已核验（Write/Edit 成功，未编译）
- 教程已口述给用户（预制体 4 步 / CellTile 3 步 / 画关卡 5 步）——用户首次自制 Unity 资产，卡住即回此节补细节
