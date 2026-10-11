# TASK-007 Design — 种子成格与三连配方（Magic Circuit 首 slice）

> 2026-10-09。Intake 拍板记录见 active_task.md TASK-007 节；本文 Coding 前须用户审过。
> 铁律：§4.2 变量注册表没有的变量/字段/接口，Coding 阶段不准创造。

## 1. 任务

- Task ID：TASK-007 / 2026-10-09
- 一句话目标：按试玩原型机制（`涌现_魔力种子_试玩.html`，**原型=规格**）实现「投掷冲量 → 弹道飞行 → 贴合成格 → 三连配方触发 → 失重/爆炸/吸引 → 连锁」竖 slice。
- 对标：原型 srcdoc 游戏脚本 L1581-1895 + GDD M1；tech-spec §4/§9（触发模型 2026-10-09 已改版，见 §9 本文末）。

## 2. 目标 / 非目标

**目标**
1. SeedThrownEvent → 模拟器接管：冲量初速飞行、贴合落定成格（进 WorldGrid，永久存在）。
2. 三连配方触发：新落定块为 source、两步邻域枚举 L 形三连、精确色数匹配、多组随机选一、每组一次、蓄能 0.5s 可取消。
3. 三效果执行：失重（范围消失+压制场）/ 爆炸（三块消失+范围解锁+冲量弹飞）/ 吸引（锚团+吸附加固）。
4. 解锁格运动：重力下落、回落再贴合（连锁的物理载体）。
5. 失重场玩家漂移（PlayerMotor 接入）。
6. 无限种子开关（PlayerInventory）。
7. **轨迹预览（2026-10-09 用户拍板进 M1）**：按住右键 → 点状抛物线（32 点）+ 落点标记，**挂在 Player 头顶锚点**。与真实弹道**共用同一步进函数**——M1 只有重力+碰撞（含失重场查询），预览=精确落点；M2 加阻尼/吸引后退化为提示，代码注释保留「预览≠保证」标注位（用户 2026-10-09 原始说明）。

**画面定版（2026-10-09 用户拍板）**：主角显示 + 格子 + 右键点状抛物线/落点标记，即全部；其余表现一律占位。

**Tile 与运行时的关系（澄清，非新机制）**：CellTile = 编辑器笔刷/模具，运行时世界 = WorldGrid 字典 + CellActor 占位方块（LevelLoader 装载时实例化）。**扔出的种子落定 = 运行时 AddCell + 实例化 CellActor，与画出的格子同款，不经过 Tilemap**。装载后 Tilemap 物体应关闭（否则同格双重渲染）——M1 测试场景手动关，后续可自动化。

**非目标**：背包/库存 UI（M1 无限种子无 UI，用户 2026-10-09 确认先不做）；判定表 CSV 化（代码常量，handoff 另任务）；Cell 状态机完整生命周期（M1 只用 Normal，消失即 Remove）；伤害/胜负/心；关卡编辑器改动；正式美术/粒子。

## 3. 涉及模块与边界

> 当前单线推进（TASK-004/005/006/007 同一会话线），历史并行写权约定按单线放宽；若再开并行会话须重新声明。

| 模块 | 读/写 | 说明 |
|---|---|---|
| Script/MagicCircuit/** | 写 | 模拟器主体（新），MockSeedThrownLogger 验收后删 |
| Script/Core/GameEvents.cs | 写 | +2 事件 +SeedThrownEvent.PlayerVelocity 字段 |
| Script/Core/CoreBootstrap.cs | 写 | 注册模拟器（1 行） |
| Script/Gameplay/Player/{PlayerMotor,PlayerInventory}.cs | 写 | 失重漂移分支 / 无限开关 |
| Script/Gameplay/World/** | **零改动** | 只调 AddCell/RemoveCell/TryGetCell 现有 API |
| Script/Presentation/** | 写 | CellActor +0.5 对齐修正；新 CircuitView（占位表现）；新 TrajectoryPreview（预览） |
| Script/Gameplay/Player/PlayerInputReader.cs + InputSystem_Actions | 写 | +AimHold action（按住右键）+ `AimHeld` 读取 |
| Assets/Editor/** | 读 | 不动；测试世界用代码摆 |

## 4. 设计

### 4.1 架构 / 流程

```
PlayerThrower --SeedThrownEvent--> MagicCircuitSimulator (ITickable, 固定步 1/120 内部累加器, 每帧≤8步)
  A. 飞行种子 SeedProjectile：vel.y -= G·dt（InZeroField 内免）；pos += vel·dt；点查格 AABB
     命中 → 贴合：贴合面邻格空 → AddCell(落定, Locked=true) → SeedLanded 日志 → evaluateRecipes(source)
                    └ 邻格全占 → 反弹继续飞（轴向速度置 0，原型同款）
  B. 运动格 MovingCell（解锁格出字典）：重力/阻尼/限速同飞行；接触 → 试贴邻空格 → AddCell 回字典
  C. 蓄能 pendingCasts：0.5s；任一块被移除即取消；到点 → cast
  D. 效果 cast（fired 去重在触发判定处已加）：
     zero   半径3.75格内全部格 RemoveCell（含三块）+ MagicField{Zero, 3.75格, 5s}
     blast  三块 RemoveCell；半径7.5格内其余格 → Locked=false 语义 = 出字典进 MovingCell + 冲量
     attract 半径3.5格内其余格 → MovingCell(吸引态)；三块留字典当锚(settled)；吸到 settled 团邻空格 → AddCell + settled.Add
  E. 失重场消费方：飞行种子/运动格重力（模拟器内）+ 玩家（IZeroFieldQuery → PlayerMotor 漂移）
表现（拉模式，避免逐步事件风暴）：CircuitView.Update 拉 FlyingSeeds/MovingCells 快照 + 订阅 CellWorldChangedEvent(Added/Removed) 维护 position→view。

轨迹预览：TrajectoryPreview（**挂在 Player 下 ThrowPoint 子物体**，用户 2026-10-09 拍板头顶出点）每帧读 AimHeld + 鼠标方向，调 **Ballistics.Step（与模拟器同一静态步进函数**：重力/InZeroField/碰撞查询全同源）采样 32 点画点串，命中即停并画落点标记（空格框）；不消耗种子、不发事件。

**抛点契约（预览=真实的物理前提）**：Player 下挂 `ThrowPoint` 子物体（头顶）；PlayerThrower 的 `SeedThrownEvent.Origin` 改为取 ThrowPoint 位置（原为 pivot，GameEvents.cs:20 注释同步），预览出点同源——两条线从同一点、同初速公式出发，弹道才逐点重合。
```

**关键架构决定**：解锁 = 格子**出 WorldGrid 字典**、进模拟器 MovingCell 列表（原型 detached group 同构）；落定/加固 = AddCell 回字典。推论 Invariant：**M1 期间字典内格子恒 Locked=true**，不存在「解锁但在字典」态。

**坐标契约**：格 (x,y) ⇔ 世界方块 [x,x+1)×[y,y+1)（Tilemap 对齐）。**修正**：CellActor.Bind 现放在整数点 (x,y)，与 Tilemap 视觉差半格 → 改为 (x+0.5, y+0.5)。

### 4.2 变量注册表

| 名称 | 类型 | 归属模块 | 用途 | Confidence | Validation-State | 证据锚点 |
|---|---|---|---|---|---|---|
| MagicCircuitSimulator | class : ITickable | TapCom.MagicCircuit | 模拟器主体：种子/运动格/场/蓄能 | High | New（原型同构） | 原型 step() L1812-1837 |
| SeedProjectile | class | TapCom.MagicCircuit | {Pos:Vector2, Vel:Vector2, Type} | High | New | spawnSeed L1624 |
| MovingCell | class | TapCom.MagicCircuit | {Origin:Vector3Int, Pos, Vel, Def快照, Attracted:bool} | High | New | detach L1600 |
| MagicField | class | TapCom.MagicCircuit | {Type, Center, Radius, Remaining, Duration, Settled:HashSet} | High | New | fields L1588 |
| SpellType | enum | TapCom.MagicCircuit | Zero/Blast/Attract | High | New | recipe() L1638 |
| RecipeTable | static class | TapCom.MagicCircuit | 判定表：TryMatch(红,蓝,黄)→SpellType?；M1 三行常量 | High | New | recipe() L1638 |
| MagicParams | static class | TapCom.MagicCircuit | 常量组（见下参数表） | High | New | 原型 L1586 等 |
| SeedThrownEvent.PlayerVelocity | Vector2 字段 | TapCom.Core | 投掷继承玩家速度（模拟器取 x·0.25） | High | 契约变更 | 原型 L1633 |
| SpellCastEvent | struct | TapCom.Core | {Type, Center:Vector2}；cast 时发布（表现钩子） | High | New | cast L1679 |
| CellWorldChangedEvent | struct | TapCom.Core | {Type:Added/Removed, Position:Vector3Int, Attr}；表现订阅 | High | New | — |
| CellChangeType | enum | TapCom.Core | Added/Removed | High | New | — |
| IZeroFieldQuery | interface | TapCom.Core | `bool InZeroField(Vector2 worldPos)`；模拟器实现，Motor 依赖 Core 不依赖 MagicCircuit | High | New | zeroAt L1636 |
| MagicCircuitSimulator.InZeroField / FlyingSeeds / MovingCells | public 只读 | TapCom.MagicCircuit | 查询 + Presentation 拉取 | High | New | — |
| PlayerMotor 失重分支 | Update 内分支 | TapCom.Gameplay.Player | InZeroField：vy=MoveInput.y·漂移速(含 W/S；Jump 亦向上)，y 阻尼；水平同现逻辑 | Med | Inference（Motor 结构已读，手感未跑） | 原型 L1828 |
| PlayerInventory._infiniteSeeds | bool SerializeField, 默认 true | TapCom.Gameplay.Player | TryConsume 无限时通过不减 | High | New | 用户拍板 |
| CellActor.Bind +0.5 偏移 | 一行修改 | TapCom.Presentation | 对齐 Tilemap 视觉 | High | Inference（Coordinate 对齐待肉眼） | CellActor.cs:31 |
| CircuitView | MonoBehaviour | TapCom.Presentation | 占位表现：Cube+着色（CellActor.prefab 就绪后换）；订阅 Added/Removed + 拉飞行/运动快照 | High | New | — |
| CoreDriver 注册模拟器 | 1 行 | TapCom.Core | services.Register(new MagicCircuitSimulator(...)) + TickManager.Register | High | New | CoreBootstrap.cs:44-48 |
| worldId 约定 "main" | const | TapCom.MagicCircuit | 模拟器经 WorldManager.Get("main") 懒解析（LevelLoader 调用方同 id） | Med | Inference（LevelLoader.Load worldId 为参数，约定未上码） | LevelLoader.cs:13 |
| Ballistics.Step | static 方法 | TapCom.MagicCircuit | 单步弹道推进（重力+InZeroField+移动+碰撞查询），模拟器与预览共用唯一实现 | High | New | 原型 step/预览同源 L1867 |
| ThrowPoint | Transform 子物体 | Player 预制体/场景 | 头顶抛点；PlayerThrower.Origin 与 TrajectoryPreview 出点同源 | High | New（拍板） | 2026-10-09 用户「挂在头上」 |
| PlayerThrower 出点改 ThrowPoint | 一处修改 | TapCom.Gameplay.Player | Origin = ThrowPoint.position（找不到则回退 pivot 并 LogError 一次） | High | 契约变更 | GameEvents.cs:20 |
| TrajectoryPreview | MonoBehaviour | TapCom.Presentation | 挂 Player；AimHeld 时采样 32 点画点串 + 落点标记（占位：小方块点+空格框） | High | New | 原型 draw() L1865-1869 |
| AimHold action / PlayerInputReader.AimHeld | Input action + bool | TapCom.Core/输入 | 按住右键（InputSystem_Actions Player map 增 binding <Mouse>/rightButton） | High | New | 原型 mousedown button===2 L1881 |
| PreviewParams | 常量组 | TapCom.Presentation | 32 点 × 0.035s 步长；**起点=头顶锚点+方向×0.9375 格**（原型为中心+30px，2026-10-09 用户改为头顶）；初速同投掷（ThrowSpeed+玩家 vx·0.25） | High | New | 原型 L1865-1867 |

**参数表（MagicParams，原型 px÷32 = 格，1 格 = 1 Unity unit）**

| 参数 | 值 | 来源 |
|---|---|---|
| TickDt / MaxStepsPerFrame | 1/120 / 8 | 原型 L1586/1890 |
| ThrowSpeed | 13.125 格/s (420px) | L1633 |
| PlayerVxInheritRatio | 0.25（只取 x） | L1633 |
| Gravity | 23.4375 格/s² (750px) | L1586 |
| HorizontalDamping | exp(−0.32·t) | L1820 |
| VelCapX / VelCapY | ±18.75 / ±20.3125 格/s | L1820 |
| MuzzleScanDist / Step | 0.9375 格 / 0.0625 格 (30/2px) | L1630 |
| SeedHitboxHalf | 0.1875 格 (12×12px) | L1595 |
| ZeroFieldRadius | 3.75 格 (120px) | L1681 |
| BlastRadius | 7.5 格 (240px，**暂定**) | L1685 |
| AttractRadius | 3.5 格 (112px) | L1690 |
| FieldDuration | 5 s（失重/吸引共用） | settings L1577 |
| ChargeTime | 0.5 s | L1658 |
| BlastImpulse | 7.8125 + 8.125·(1−d/r) 格/s，vy 再 −3.4375 | L1686 |
| AttractSeekSpeed | min(13.125, d·6) 格/s，blend dt·10 | L1721 |
| DetachGraceTime | 0.22 s（防解锁即回贴） | L1600 |
| SnapTolerance | 面向 ≤0.8 格 / 距离 ≤0.95 格 | L1733 |
| ZeroDriftSpeed | 竖 5.9375 格/s (190px)；水平沿用 _moveSpeed | L1828 |

### 4.3 契约（事件 / 公共接口）

| 名称 | 签名 | 发布方 → 订阅方 | 语义 |
|---|---|---|---|
| SeedThrownEvent（改） | +PlayerVelocity:Vector2 | PlayerThrower → Simulator | PlayerThrower 填 Motor.Velocity；模拟器算初速 |
| SpellCastEvent（新） | {SpellType, Vector2 Center} | Simulator → Presentation | 效果释放瞬间（火花/圈表现钩子） |
| CellWorldChangedEvent（新） | {CellChangeType, Vector3Int, CellMagicAttr} | Simulator → CircuitView | 字典格增/删离散通知 |
| IZeroFieldQuery（新） | InZeroField(Vector2):bool | Simulator 实现 → PlayerMotor 查 | 失重漂移判定 |
| 模拟器查询 | FlyingSeeds/MovingCells 只读枚举 | Simulator → CircuitView | 拉模式快照 |

### 4.4 数据表

M1 判定表为 RecipeTable 代码常量（3 行：1红2蓝→Zero / 1红1蓝1黄→Blast / 2红1黄→Attract，精确色数）。CSV 化走既有导表管线，**另任务 handoff**，不在本轮。

### 4.5 Invariants（本任务引入/依赖）

1. 运动体（种子/运动格）不占格点；落定才进 WorldGrid；M1 期间字典内格子恒 Locked=true。
2. 触发 source 必须是刚落定/新接触的魔法格；老邻居接触不触发（原型 L1743）。
3. 同一组三连（三格坐标集合）只触发一次（fired 集，string key 坐标排序）。
4. 蓄能中三格被任一移除 → 取消；格点世界里落定格不会动，故「分离」唯一来源是被效果移除。
5. 多组候选随机选一（原型 L1657）；蓄能格不参与新组合。
6. 效果修改一律经模拟器执行（铁律 #4：模拟器=执行器），Player 不直改 World。
7. 出字典运动格 ≥ DetachGraceTime 内不参与贴合。

## 5. 测试计划（Play Mode，日志+场景肉眼自证）

| 验证点 | 方式 | 预期信号 |
|---|---|---|
| 投掷落定 | 测试世界（代码摆岩石排）+ 鼠标瞄准投蓝 | `[MagicCircuit] seed landed (x,y)`，场景出现蓝块，WorldGrid Count +1 |
| 三连→失重 | 摆蓝蓝相邻，投红成 L 形 | 蓄能日志 0.5s → `cast zero`，范围格消失 + 失重圈（CircuitView 画圈） |
| 爆炸 | 摆红蓝相邻，投黄 | 三块消失 + 周围格弹飞 → 落回贴合，`cast blast` 日志 |
| 吸引 | 摆红红相邻，投黄 | 格被拉向锚团、贴合加固，`cast attract` 日志 |
| 连锁 | 失重清除后上方悬空格解锁下落 | `cell unlocked (x,y)` → 回落 `re-attached (x,y)` 日志 |
| fired 去重 | 同局再摆同组三连 | 第二次不触发（无 cast 日志） |
| 蓄能取消 | 蓄能 0.5s 内用爆炸打掉一块 | `charge cancelled` 日志 |
| 失重漂移 | 玩家进失重圈 | W/Space 升、S 降、松手缓停（肉眼） |
| 无限种子 | 连投 10 次 | Inventory 计数不减，无拒绝日志 |
| 出手口被挡 | 玩家贴墙投掷 | `throw blocked: muzzle occluded`，不消耗不发布 |
| 预览显示 | 按住右键移动鼠标 | 点状抛物线实时跟随，命中处落点标记；松开消失 |
| 预览=真实 | 瞄准后投出（含失重场内瞄准） | 实际弹道与预览点重合、落在标记格 |

## 6. 风险与回退

- **回落贴合手感**（最高风险）：滑动贴合做不稳 → 回退分级：R1 运动格只垂直下落+顶面贴合（原型岩石平时也以上下贴合为主）；R2 爆炸冲量砍成只有垂直分量。逐级降，不静默。
- **半格偏移修完视觉仍错**：占位阶段以碰撞为准，视觉偏差记录不阻塞验收。
- **TickManager 变步长**：模拟器内部累加器消化，不改 Core。
- **SeedThrownEvent 加字段**：影响 PlayerThrower 一处 + Mock 日志一处，编译影响面已知。

## 7. Open Questions（未拍板不进 Coding）

- 爆炸半径 7.5 格为原型「暂定」值，是否保留待用户调参。
- fired 集生命周期：M1 随 Play 会话不清（重载关卡不重置）——可接受？还是 LevelLoader.Load 时清（要动 TASK-006 文件 1 行）。
- 玩家被运动格挤压：原型是 Game Over；M1 无伤害系统，先不处理挤压（重叠时推开），语义 M3 对齐 §10 #14。
- 吸引 slot 争用：两运动格抢同一空格的仲裁（M1 先到先得），是否够用。

## 8. 任务拆解（Task）

| # | 任务 | 改哪些文件 | 验收 |
|---|---|---|---|
| 1 | 契约层 | GameEvents.cs（+2 事件+字段）、PlayerThrower.cs（填 PlayerVelocity）、MockSeedThrownLogger（日志补字段） | 编译过，投掷日志含新字段 |
| 2 | 常量与判定表 | MagicParams.cs、RecipeTable.cs（TapCom.MagicCircuit） | 单元可读、数值与 §4.2 参数表一致 |
| 3 | **代码测试世界** | Editor/TestWorldBuilder.cs（一键菜单：岩石排+颜色格+地面+装载进 WorldGrid，不依赖 Tilemap 导出链） | 菜单跑完日志 `test world ready: n cells`，Play 可见占位块 |
| 4 | 种子飞行+贴合落定 | MagicCircuitSimulator.cs（SeedProjectile/**Ballistics.Step 静态提取**/碰撞/贴合/AddCell） | 验收表第 1、10 行 |
| 5 | 触发判定+蓄能 | 模拟器 evaluateRecipes/pendingCasts/fired | 验收表第 2、6、7 行 |
| 6 | 三效果 cast | cast(Zero/Blast/Attract) + MagicField + IZeroFieldQuery | 验收表第 2、3、4 行 |
| 7 | 运动格物理 | MovingCell 重力/回落贴合/吸引寻位 | 验收表第 5 行 |
| 8 | Player 接入 | PlayerMotor（失重分支）、PlayerInventory（无限开关）、InputSystem_Actions+AimHold、PlayerInputReader.AimHeld | 验收表第 8、9 行 |
| 9 | 占位表现+预览 | CircuitView.cs、CellActor.cs（+0.5）、TrajectoryPreview.cs（调 Ballistics.Step，挂 ThrowPoint）、PlayerThrower 出点切 ThrowPoint | 场景肉眼：块同步 + 验收表第 11、12 行 |
| 10 | 装配+验收 | CoreBootstrap.cs 注册；跑 §5 全表；删 MockSeedThrownLogger（+.meta）。Tilemap→LevelData→装载链**并行验收**（用户已有 Tile 成果），通过后测试世界可换真关卡 | §5 十二项全过 |

---

## 9. 触发模型变更记录（tech-spec §4 同步）

- 2026-10-08 拍板「闭环模型，成环=经过种子的闭合回路」→ **2026-10-09 用户拍板作废**（原话路径：确认「按代码走」+「对，可以给一个冲量，就这样」）。新触发模型 = 原型三连配方：贴合即成格、新接触为 source、两步邻域 L 形三连、精确色数查 RecipeTable、蓄能 0.5s、每组一次、随机选组。连锁 = 效果改世界 → 解锁格运动 → 新接触 → 新判定（物理传链，无图遍历）。「种子是源节点」精神保留：source 恒为最新落定块。
- 判定时机 §10 #15 随本模型销案：贴合（=接触=落定）瞬间判定，三者合一。

## 10. Coding 增补登记（2026-10-10 落码时的增量，转写自本 design，未出范围）

- **SpellType 归属修正**：设计表原写 TapCom.MagicCircuit → 实际放 **TapCom.Core**（GameEvents.cs，SpellCastEvent 契约依赖；与 SeedMagicType 同位。Core 不得反向依赖 MagicCircuit）。
- **MagicParams 增补 2 项**：`OutOfBoundsY=-50`（出界移除线）、`AttractArriveDistance=0.35`（吸引到位判定）。
- **Ballistics.Result**：struct{Position, Velocity, Hit, HitCell, HitNormal}；贴合目标 = HitCell + 取整法线。
- **MagicCircuitSimulator 构造**：`new MagicCircuitSimulator(EventBus, WorldManager)`；Priority=100；World 属性懒解析 "main"（缺失告警一次）；CoreBootstrap 注册为本体 + IZeroFieldQuery 双键 + TickManager。
- **TestWorldInstaller（Gameplay）**：机制修正为 **LevelData 资产 + LevelLoader 复用链**（原表述「直接装载进 WorldGrid」）：Editor 菜单生成 Assets/Levels/TestWorld.asset + 场景 installer（SerializeField 引用）；Awake 装载 "main" + AddComponent&lt;CircuitView&gt;。actorPrefab=null（表现全归 CircuitView，避免双渲染）。
- **CircuitView/TrajectoryPreview 内部件**：Texture2D.whiteTexture 占位 sprite（4×4、ppu=4 ⇒ 1 格）；种子/运动格池化视图；蓄能高亮字典；预览常量 DotCount=32 / StepTime=0.035。
- **并行会话叠加说明**：落码期间并行线同时改了 PlayerMotor（GroundProbeGap 地面探针修正）、CellActor（+0.5 半格修正已由对方按本 design 完成）、CoreBootstrap（LevelManager/TASK-008）；本线增量已在其最新版本上叠加，无覆盖。
- **对账修正（落码后发现并行线 TASK-008/009/010/011 已改变架构，四处随改）**：
  1. CLAUDE.md 新铁律「游戏物体一律人工配置，AI 不写自动生成脚本」→ TestWorldBuilder 砍掉场景物体自动装配，只生成 LevelData 资产（数据文件，同关卡编辑器导出性质）；场景侧人工配置走使用步骤日志。
  2. LevelManager/LevelLoadedEvent/LevelActorBinder 已成装载正道 → **TestWorldInstaller.cs 删除**（重复建设）；测试世界走 LevelBootstrap 拖资产 → LevelManager.Load → Binder 的既有链。
  3. 模拟器 World 解析改 `LevelManager.CurrentWorldId`（原设计 worldId="main" 作废，MagicParams.WorldId 删）；ctor 增第三参 LevelManager。
  4. CircuitView 不再枚举初始格子（初始表现归 LevelActorBinder，防双渲染）；改订阅 LevelLoadedEvent 在关卡重载时清动态视图。
- **待编译**：全部改动未编译（Unity 未聚焦），首个真实编译信号待用户。
