using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TapCom.EditorTools
{
    /// <summary>
    /// CI 出包底座（本地 .bat / GitHub self-hosted runner / Jenkins 通用）：
    ///   Unity -batchmode -nographics -projectPath <项目> -buildTarget StandaloneWindows64
    ///     -executeMethod TapCom.EditorTools.CiBuild.BuildWindows64 -logFile <日志>
    /// 链路：导表(CSV→JSON，失败即中断) → Addressables 内容构建 → BuildPlayer。
    /// 成功 Exit(0)，任何一步失败 Exit(1)，触发层按退出码判成败。
    /// 产物：Build/StandaloneWindows64/<时间戳>/；时间戳优先取 -buildTimestamp 启动参数，缺省用本机当前时间。
    /// </summary>
    public static class CiBuild
    {
        private const string OutputRoot = "Build/StandaloneWindows64";

        private static string _buildTimestamp;

        /// <summary>构建时间戳：workflow 经 -buildTimestamp 传入（与 artifact 名同源），本地直跑取当前时间。</summary>
        private static string BuildTimestamp
        {
            get
            {
                if (!string.IsNullOrEmpty(_buildTimestamp))
                {
                    return _buildTimestamp;
                }
                var args = Environment.GetCommandLineArgs();
                for (var i = 0; i < args.Length - 1; i++)
                {
                    if (string.Equals(args[i], "-buildTimestamp", StringComparison.OrdinalIgnoreCase))
                    {
                        _buildTimestamp = args[i + 1];
                        return _buildTimestamp;
                    }
                }
                _buildTimestamp = DateTime.Now.ToString("yyyyMMdd-HHmm");
                return _buildTimestamp;
            }
        }

        public static void BuildWindows64()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                {
                    throw new Exception(
                        $"[CiBuild] 当前平台 {EditorUserBuildSettings.activeBuildTarget} ≠ StandaloneWindows64，" +
                        "请用 -buildTarget StandaloneWindows64 启动参数（脚本内不切平台，避免 batchmode 中途域重载）");
                }

                Debug.Log($"[CiBuild] build timestamp: {BuildTimestamp}");

                StepExportTables();
                StepBuildAddressables();
                StepBuildPlayer();

                Debug.Log($"[CiBuild] SUCCESS 全链路完成（{BuildTimestamp}），总耗时 {watch.Elapsed.TotalSeconds:F0}s");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError($"[CiBuild] FAILED: {e.Message}");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>导表：复用导表工具的命令行入口，失败其内部 throw，自然中断本链。</summary>
        private static void StepExportTables()
        {
            Debug.Log("[CiBuild] step 1/3 导表 CSV→JSON ...");
            TableTool.Editor.TableExportWindow.ExportAllForCommandLine();
        }

        /// <summary>构建 Addressables 内容（AddressableAssetsData 当前激活 profile）。</summary>
        private static void StepBuildAddressables()
        {
            Debug.Log("[CiBuild] step 2/3 构建 Addressables 内容 ...");
            AddressableAssetSettings.BuildPlayerContent(out var result);
            if (!string.IsNullOrEmpty(result.Error))
            {
                throw new Exception($"[CiBuild] Addressables 内容构建失败: {result.Error}");
            }
        }

        /// <summary>打包：场景取 Build Settings 启用项，输出 Build/StandaloneWindows64/产品名.exe。</summary>
        private static void StepBuildPlayer()
        {
            Debug.Log("[CiBuild] step 3/3 BuildPlayer ...");
            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled && !string.IsNullOrEmpty(s.path))
                .Select(s => s.path)
                .ToArray();
            if (scenes.Length == 0)
            {
                throw new Exception("[CiBuild] Build Settings 没有启用的场景");
            }

            var rootDir = Path.Combine(Path.GetDirectoryName(Application.dataPath), OutputRoot);
            if (Directory.Exists(rootDir))
            {
                Directory.Delete(rootDir, true); // 整根清理：磁盘有界；历史版本靠 Actions artifact（14 天）
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(OutputRoot, BuildTimestamp, $"{PlayerSettings.productName}.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception(
                    $"[CiBuild] BuildPlayer 失败: {report.summary.result}（{report.summary.totalErrors} 个错误，见上方日志）");
            }
            Debug.Log($"[CiBuild] 出包完成: {options.locationPathName}（{report.summary.totalSize / (1024 * 1024)} MB）");
        }
    }
}
