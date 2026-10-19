# 项目规范 — TapCom26 / My project (1)

Unity **本地单机游戏**：无联网、无服务端、无账号系统，所有逻辑与数据本地运行。
本文件优先级高于全局 CLAUDE.md；冲突时以本文件为准。

## 工作纪律（硬约束，优先级最高）

- **不做用户没有明说过的事，尤其是写代码**：AI 只执行明确要求的事；动手写代码前必须逐次询问确认，写码不求快、不求全、不"顺手"扩展。
- **"开始 / 继续"只授权当下正在讨论的那一件事**，不外溢到其他任务、其他文件、其他模块；范围外发现的问题记 open-question，不动手。
- AI 自查到越界 = 立刻停下汇报，不掩饰、不补做。

## 目录约定（Assets/ 下）

| 目录 | 职责 | 放什么 |
|---|---|---|
| `Script/` | 运行时脚本 | 游戏逻辑 C# 代码（会打进游戏包） |
| `Config/` | 配置源表 | 策划手填的 CSV 表（见下方管线） |
| `Arts/` | 资产 | 美术资源（贴图 / 模型 / 音频 / 动画等） |
| `Prefab/` | 预制体 | `.prefab` 一律放这里 |
| `Editor/` | 编辑器 | `UnityEditor` 扩展代码（仅编辑器可用，不进包） |

- 目录尚未建齐时，**新文件按上表归位后再提交**，不要散落在 Assets 根目录。
- 现有 `Assets/TableTool/Editor/` 是导表工具，属编辑器代码，保持原位。

## 配置表管线（已存在，勿绕过）

```
Assets/Config/*.csv  --导表工具-->  Assets/AddressableAssets/Configs/*.json  --Addressables-->  运行时
  (策划手填源表)                       (生成产物，勿手改)
```

- **改配置 = 改 CSV 再导出**。`AddressableAssets/Configs/` 下的 JSON 是生成产物，手改会被下次导出覆盖。
- 导出入口：Unity 菜单 `Tools/导表工具`（窗口内「导出全部」）；命令行/CI 用 `TableTool.Editor.TableExportWindow.ExportAllForCommandLine`。
- CSV 约定：第 1 行 = 字段名，第 2 行起 = 数据，第一列 = 主键，单元格内用 `;` 分隔表示数组；Excel 需另存为「CSV UTF-8」。
- 加载走 Addressables（`com.unity.addressables`），不用 `Resources.Load`。
- 路径常量以 `Assets/TableTool/Editor/TableConverter.cs` 的 `CsvRoot` / `OutputRoot` 为准，改动即全管线改动。

## CI 出包管线（2026-10-19 建，勿绕过）

```
push Dev_Pao --> GitHub Actions --> self-hosted runner(本机) --> 同步本地资产 --> 导表/Addressables/打包 --> zip --> GitHub Release --> 清本地
```

- workflow 与运维细节（runner 注册、Library 缓存清理、run 体 ASCII 纪律）：`.github/workflows/build-windows.yml` 头部注释；构建脚本：`Assets/Editor/CiBuild.cs`。
- 取包只认仓库 Release 页（tag `build-<时间戳>` 的 zip）；本地与 runner 工作区不留包。
- 出包前提：本机 runner 在线（run.cmd 交互窗口，勿装服务）；D 盘资产在原位——同步步校验，缺失直接红。

## 其他

- `Assets/TutorialInfo/`、`Assets/Readme.asset` 是 Unity 模板残留，与游戏无关，不要往里加东西。
- **资产与场景物体一律人工配置**（2026-10-10 拍板）：AI 不写自动生成场景物体/预制体/美术资产的脚本，只写逻辑代码 + 给出人工操作步骤；场景、预制体、贴图、UI 由人手动搭建。
- 分支约定：开发在 `Dev_Pao`，主分支 `master`。
