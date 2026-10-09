# Design — TASK-006：Cell 家族定版 + Tilemap 关卡编辑器 MVP

> 上游依据：tech-spec §3/§6/§7；TASK-002 拍板（蓝红黄）；2026-10-09 用户四问拍板 + 追加（岩石惰性但受引力/爆炸影响；要做 Tilemap 地图编辑器；要做预制体）。
> namespace 沿仲裁集，新增 `TapCom.Presentation`（cellActor 归 Presentation 域，仲裁四件套里原本没有 Presentation 类型）。

## 1. 任务

- Task ID / 日期：TASK-006 / 2026-10-09
- 一句话目标：定版首批 Cell 家族（惰性岩石 + 蓝红黄魔法格，全部数据层组合、零新 CellType），配 cellActor 占位预制体、LevelData 资产、Tilemap→LevelData 编辑器窗口。

## 2. 拍板记录（2026-10-09 AskUser）

| 问题 | 拍板 |
|---|---|
| 魔法格建模 | **类型+属性维度**：CellType 仍只有 Rock，颜色走 MagicAttr（None/Blue/Red/Yellow） |
| 岩石角色 | **惰性**（不参与回路、不反应），**但受引力和爆炸影响**（可被解锁掉落、可被爆炸摧毁）→ 岩石 GravityAttr = Dynamic；Static 保留给未来基岩类 |
| 魔法格重力 | **Dynamic**（可被重力效果解锁掉落） |
| 做到哪层 | **数据+占位表现**；追加：**做 Tilemap 地图编辑器** + 预制体（CellActor.prefab 单预制体运行时着色） |

## 3. 模块边界

| 模块 | 读/写 | 说明 |
|---|---|---|
| Gameplay | 写 | LevelData/CellDef/LevelLoader（Script/Gameplay/） |
| Presentation | 写（新域） | CellActor 占位（Script/Presentation/，namespace TapCom.Presentation） |
| Editor | 写 | CellAssetBuilder + TilemapLevelWindow（Assets/Editor/） |
| World（TASK-005） | 读 | AddCell API 消费方 |
| Core | 不碰 | WorldManager 已注册 |

## 4. 变量注册表

| 名称 | 类型 | 归属 | 用途 | Confidence | 锚点 |
|---|---|---|---|---|---|
| `CellDef` | struct {Pos:Vector3Int, Type, Gravity, Magic, Locked} | Gameplay | 关卡里一个格子的静态定义（=tech-spec §7 Cell Definition） | High | tech-spec §7 示例字段 |
| `LevelData` | ScriptableObject {List\<CellDef\> Cells} | Gameplay | 关卡静态数据资产；编辑器生成，运行时只读 | High | tech-spec §7「Tilemap 转换成 Level Data」 |
| `CellActor` | MonoBehaviour : RequireComponent(SpriteRenderer) | Presentation | cellActor 占位：`Bind(Cell)` 定位 + 着色（岩石灰 / 蓝 / 红 / 黄）；只读 Cell 绝不回写（铁律 #2） | High | tech-spec §3 cellActor、§6 Character/World 表现原则 |
| `LevelLoader.Load(manager, worldId, data, actorPrefab, parent)` | static → WorldGrid | Gameplay | LevelData → WorldManager.Create + AddCell 循环 + 实例化 CellActor.Bind | High | tech-spec §2 加载 |
| `CellAssetBuilder.Build()` | MenuItem Tools/魔法回路/生成Cell占位资产 | Editor | 生成：Arts/Placeholder/CellSquare.png（32×32 白块，PPU=32 → 1格1单位）+ Tile_{rock,blue,red,yellow}.asset（按色 tint）+ Prefab/CellActor.prefab | High | 项目规范 Prefab/Arts 目录约定 |
| `TilemapLevelWindow` | EditorWindow，MenuItem Tools/魔法回路/关卡编辑器 | Editor | ①创建编辑用 Grid+Tilemap ②按 **Tile 命名约定** `cell_rock/cell_blue/cell_red/cell_yellow` 把 Tilemap 转成 Assets/Levels/{id}.asset；未识别名跳过并计数警告 | High | tech-spec §7 Level Editor（Tilemap 是编辑器工具，铁律 #7） |
| Tile 命名约定 | 字符串契约：`cell_<kind>` | Editor | Tile → CellDef 映射的唯一依据（避免 Tile 资产上挂自定义数据） | High | 本表（新契约，双线可见） |

## 5. Invariants

1. Tilemap 只存在于编辑器侧；运行时只认 LevelData/WorldGrid（铁律 #7 原文）。
2. CellActor 只读 Cell；表现不拥有状态（铁律 #2）。
3. LevelData 是静态数据，与运行时严格分离（铁律 Data 分离）。
4. 占位期 1 格 = 1 Unity 单位（PPU 32、贴图 32px），z 恒 0。

## 6. 测试计划

| 验证点 | 方式 | 预期信号 |
|---|---|---|
| 占位资产生成 | 菜单 `生成Cell占位资产` | Console done 日志；Arts/Placeholder/ 5 个资产 + Prefab/CellActor.prefab 出现 |
| 编辑器画图→转换 | 画几个格子 → `关卡编辑器 → Tilemap→LevelData` | `[TilemapLevel] saved: n defs` + Assets/Levels/ 资产 |
| 运行时装载 | Play 后（临时验证：菜单或后续测试场景调 LevelLoader） | WorldGrid.Count=n + 场景出现 n 个着色方块 |
| 编译 | Unity 聚焦 | 零红字 |

## 7. Open Questions / 非目标

- 非目标：Magic Circuit（下一任务）、CSV 关系（#9）、cellActor 动画/销毁同步（归 Presentation 完整版 + 事件订阅）、Addressables 装载（LevelData 暂走直接引用）。
- `Assets/Levels/` 是新目录，项目 CLAUDE.md 目录约定里没有——用户可指定挪去（如 AddressableAssets/Levels）。
- 岩石「受爆炸影响」的效果语义归判定表（tech-spec #2 已记），本任务不实现。

## 8. 任务拆解

| # | 任务 | 文件 | 验收 |
|---|---|---|---|
| 1 | LevelData + CellDef + LevelLoader | Script/Gameplay/{LevelData, LevelLoader}.cs | 编译过 |
| 2 | CellActor 占位 | Script/Presentation/CellActor.cs | 编译过 |
| 3 | Editor 两件 | Editor/{CellAssetBuilder, TilemapLevelWindow}.cs | §6 表前三行 |

## 9. v2 修订（2026-10-09 用户纠偏「做的不对」）

| v1（作废） | v2（现行） | 原因（用户原话） |
|---|---|---|
| Editor 脚本自动生成 CellActor.prefab + 贴图 + Tile | **资产用户手做**（预制体教程口述 + Create 菜单），脚本零代劳 | 「我自己来制作预制体，你教我」 |
| Tile 按命名约定 cell_* 字符串识别 | **CellTile 自定义 Tile 资产自带格子定义**（Create > 魔法回路 > CellTile，Inspector 填 Type/Gravity/Magic），导出读数据不看名字 | 「笔刷的格子可以读取对应的格子」 |
| 按钮临时建 LevelGrid | 「场景关卡化」一键给当前场景铺 Grid+Tilemap，笔刷走 Unity 原生 Tile Palette | 「编辑器应该是把每个场景变成 tilemap 的，制作者可以用笔刷制作，然后可以保存和导出数据」 |

- 资产删除：Editor/CellAssetBuilder.cs（含 .meta）。
- CellActor 新增 `_tintByCell` 开关（用户美术自带配色时关运行时着色）。
