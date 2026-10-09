# Design — TASK-005：Cell 与 World（数据与存储层）

> 按 `.claude/templates/design-template.md` 产出。上游依据：tech-spec §2 World/WorldGrid、§3 Cell、§4 核心规则（TASK-002 拍板）、铁律 #9/#11。
> Unity 6000.0.53f1 / C# 9 / namespace 沿 TASK-003 仲裁：`TapCom.*`。

## 1. 任务

- Task ID / 日期：TASK-005 / 2026-10-08
- 一句话目标：落地环境模拟的数据与存储层——Cell 纯数据单元、WorldGrid 格点字典（含邻接查询）、WorldManager 多实例管理，附 Editor 自检工具自证。
- 对标：里程碑 M1（魔力种子、场景方块、环境反应）的地基；Magic Circuit（下一任务）是这些 API 的唯一执行器。

## 2. 目标 / 非目标

- 目标：
  1. Cell 数据单元：tech-spec §3 全字段（位置/类型/当前状态/魔法状态/连接关系/锁定 + 重力属性/魔法属性）；
  2. WorldGrid：`Dictionary<Vector3Int, Cell>` 格点存取 + 4 邻接查询 + 增删查枚举；
  3. WorldManager：多 WorldGrid 实例容器（每关一实例，加载/清理/初始化，tech-spec §2 原文）；
  4. WorldManager 进 Services 容器（CoreDriver 装配，Core 目录写权在本会话，无越界）；
  5. Editor 自检菜单：真实断言跑通增删查/邻接/占用，Console 输出 PASS/FAIL。
- 非目标（明确不做）：
  - Magic Circuit 本体（落点判定/连接规则/成环/效果执行/连锁收敛）——下一任务；
  - CSV/Level Data 装载（Level Editor 与导表管线关系是 tech-spec §10 #9，M3 前）；本任务世界内容由代码/自检工具构造；
  - 冻结范围全局配置的读取（Q12 是收敛逻辑的参数，归 Magic Circuit 任务）；
  - Presentation（cellActor 预制件、动画）；
  - 8 邻接 / 对角连接（回路规则 #6 填表后才定，先 4 邻接，加廉价）。

## 3. 涉及模块与边界

| 模块 | 读/写 | 说明 |
|---|---|---|
| Gameplay（World 子域） | 写 | Cell/WorldGrid/WorldManager 新目录 `Script/Gameplay/World/` |
| Core | 写 1 行 | CoreDriver.Awake 注册 WorldManager 进 Services |
| Editor | 写 | `Assets/Editor/WorldSelfTest.cs` 自检工具 |
| MagicCircuit | 读（未来） | 下一任务的执行器，本任务只定契约不实现 |
| Player（TASK-003 线） | 不碰 | 协作约定继续生效 |

## 4. 设计

### 4.1 架构 / 流程

```
WorldManager (Services 单例)
  └─ Dictionary<string, WorldGrid>   // 每关一实例（tech-spec §2）
       WorldGrid
        └─ Dictionary<Vector3Int, Cell>   // 「Cell 本身是字典」的字典落点：格点存取
             Cell (sealed class, 纯数据)    // 无 GameObject、无物理、无表现（铁律 #1/#2）
```

- Magic Circuit（未来）只经 WorldGrid API 改环境；Player 永不直接改（铁律 #3/#4）。
- z 维度：类型用 Vector3Int 保留，本任务所有内容 z=0（铁律 #11：深度只属于景观）。

### 4.2 变量注册表

| 名称 | 类型 | 归属 | 用途 | Confidence | Validation-State | 锚点 |
|---|---|---|---|---|---|---|
| `TapCom.Gameplay`（World 子域扩展） | namespace | Gameplay | World 三件套；Player 线已用此 namespace，互不冲突 | High | 已核验（对方文件同 namespace 在用） | 本表 |
| `CellType` | enum { Rock } | Gameplay | Cell 类型；M1 单类型（tech-spec §10 #3：类型=数据行，加类型=填表） | High | tech-spec §3/§7 Rock 实证 | tech-spec §7 |
| `CellGravityAttr` | enum { Static, Dynamic } | Gameplay | 重力属性（静态规则：能否被重力效果解锁；Q5 拍板） | High | tech-spec §7 编辑器示例 Static 实证 | tech-spec §7 |
| `CellMagicAttr` | enum { None, Blue, Red, Yellow } | Gameplay | 魔法属性（静态：与哪种种子反应；蓝/红/黄 TASK-002 截图拍板） | High | 用户拍板记录 | tech-spec §3 增补 |
| `CellState` | enum { Normal, MagicAffected, Unstable, Collapsed, Vanished } | Gameplay | 当前状态（运行时生命周期，tech-spec §3 状态机原文） | High | tech-spec §3 原文 | tech-spec §3 |
| `CellMagicState` | enum { None, Connected } | Gameplay | 魔法状态（运行时：是否连入回路；语义最小集，回路任务扩展） | High | 新建（tech-spec §3 要求字段存在，取值未定→最小集） | 本表 |
| `Cell` | sealed class | Gameplay | 纯数据单元：Position(Vector3Int) / Type / State / MagicState / GravityAttr / MagicAttr / Locked(bool) / Connections(List\<Vector3Int\>) | High | 字段=tech-spec §3 全清单；**形态注记**：原文「Cell 本身是一个字典」按「格点字典存 Cell」落地（WorldGrid 层），Cell 本体用强类型字段——比 string-key 字典可调试、可编译期检查；如需字面字典形态用户可否决 | 本表 |
| `WorldGrid` | class | Gameplay | `Dictionary<Vector3Int, Cell>`；API：AddCell / RemoveCell / TryGetCell / IsOccupied / GetNeighbors(4 向) / Cells 枚举 / Count | High | 新建 | 本表 |
| `WorldGrid.AddCell` | Cell 方法（重复位置抛 InvalidOperationException） | Gameplay | 占位即建格；定义冲突 fail fast（沿 Core Invariant 1 风格） | High | 新建 | 本表 |
| `WorldGrid.GetNeighbors` | IReadOnlyList\<Cell\>（快照数组） | Gameplay | XY 4 向（±1,0)/(0,±1)，z 不动；越界与空位跳过 | High | 新建（回路规则未定前的基础邻接） | 本表 |
| `WorldManager` | class | Gameplay | `Dictionary<string, WorldGrid>`；Create(id) / TryGet / Get(缺失抛) / Destroy(id) / Clear / Count | High | tech-spec §2 WorldManager 原文（大地图容器装 worldgrid、每关一实例、加载清理初始化） | tech-spec §2 |
| `WorldManager` 注册进 Services | `services.Register(new WorldManager())` | Core | CoreDriver.Awake 装配行 + ready 日志串追加 WorldManager | High | 新建 | 本表 |
| `WorldSelfTest` | static class，`[MenuItem("Tools/魔法回路/世界自检")]` | EditorTools | 断言式自检：建格/增删/查重拒绝/邻接/占用/多实例隔离；Console `[WorldSelfTest] PASS n/n` | High | 新建（用户测试纪律：真实环境自证） | 本表 |

### 4.3 契约（事件 / 公共接口）

本任务零事件。对 Magic Circuit 的接口契约 = WorldGrid 公共 API（见注册表）；种子落格 = 未来 `AddCell`（种子是格子，TASK-002 拍板）。

### 4.4 数据表

不触 CSV 管线（非目标 #2）。

### 4.5 Invariants（本任务引入/依赖）

1. **Cell 不是 GameObject**：无 MonoBehaviour、无 Renderer、无物理组件；表现归 Presentation（铁律 #1/#2）。
2. **格点唯一**：一位置一 Cell；AddCell 撞位抛异常（哑配置炸在调用时）。
3. **Connections 只存已存在格点的位置**：RemoveCell 时从所有邻接 Connections 里清除对它的引用（边集一致性）。
4. z 轴本层不产生任何语义：所有 API 天然支持 z，内容恒 z=0（铁律 #11）。
5. WorldManager 的 grid 实例互不共享 Cell 对象（实例隔离）。

## 5. 测试计划

| 验证点 | 方式 | 预期信号 |
|---|---|---|
| 数据层全量自检 | Editor 菜单 `Tools/魔法回路/世界自检` | `[WorldSelfTest] PASS 14/14`（14 条断言覆盖下方 10 项清单，含抛错 / 缺位负路径） |
| Core 装配带出 WorldManager | Play Mode | ready 日志含 `WorldManager` |
| 编译 | Unity 聚焦 | Console 零红字（与 TASK-003/004 共享首次编译） |

自检断言清单（WorldSelfTest 内）：① AddCell 后 Count=1 ② 重复位置 AddCell 抛 ③ TryGet/IsOccupied 命中 ④ RemoveCell 后占位清除 ⑤ 4 邻接命中/边界跳过 ⑥ RemoveCell 清理邻接 Connections ⑦ 多实例隔离（A 格操作不影响 B）⑧ Destroy/Clear 生效 ⑨ Cell 字段赋值往返 ⑩ z≠0 位置可存（类型支持，内容不用）。

## 6. 风险与回退

- 风险：`TapCom.Gameplay` 与 Player 线同 namespace——仅追加新类型于新子目录，无文件交叉，冲突概率极低；冲突表现为重复类名编译错，一眼可查。
- 回退：全部新文件 + CoreDriver 两行改动；删 `Script/Gameplay/World/` + `Editor/WorldSelfTest.cs` + 还原 CoreDriver 即完全回退。

## 7. Open Questions

- 「Cell 本身是字典」的字面形态解读（string-key 弱类型字典）本任务未采纳，按强类型字段落地——用户可否决，否决则 Cell 重构为字典壳（改动仅 Cell.cs 一文件）。
- 回路连接的邻接定义（4 向 vs 其他）待 #6 填表拍板；GetNeighbors 语义届时可扩。

## 8. 任务拆解

| # | 任务 | 改哪些文件 | 验收 |
|---|---|---|---|
| 1 | 枚举 + Cell | Script/Gameplay/World/Cell.cs | 编译过 |
| 2 | WorldGrid + WorldManager | Script/Gameplay/World/{WorldGrid, WorldManager}.cs | 编译过 |
| 3 | Core 注册 | Script/Core/CoreBootstrap.cs（+2 行） | Play ready 日志含 WorldManager |
| 4 | Editor 自检 | Editor/WorldSelfTest.cs | 菜单跑出 PASS 10/10 |
