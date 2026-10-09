using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TableTool.Editor
{
    /// <summary>导表工具窗口：表列表 + 类型预览 + 校验错误 + 一键导出。</summary>
    public class TableExportWindow : EditorWindow
    {
        [MenuItem("Tools/导表工具")]
        public static void Open() => GetWindow<TableExportWindow>("导表工具");

        /// <summary>命令行/CI 入口：Unity -batchmode -executeMethod TableTool.Editor.TableExportWindow.ExportAllForCommandLine</summary>
        [MenuItem("Tools/导表工具/命令行导出全部")]
        public static void ExportAllForCommandLine()
        {
            int ok = 0, fail = 0;
            foreach (var path in TableConverter.CollectCsvFiles())
            {
                var result = ExportTable(path);
                if (result.Ok) ok++; else fail++;
            }
            AssetDatabase.Refresh();
            Debug.Log($"[导表] 命令行导出完成：成功 {ok}，失败 {fail}");
            if (fail > 0) throw new Exception($"[导表] 有 {fail} 张表导出失败，错误见上方日志（batchmode 将以非零码退出）");
        }

        /// <summary>导出一张表：写 JSON → 导入资产 → 注册 Addressables；结果写 Console。</summary>
        static TableResult ExportTable(string csvPath)
        {
            var result = TableConverter.Convert(csvPath, write: true);
            if (result.Ok)
            {
                string jsonPath = Path.Combine(TableConverter.OutputRoot, result.TableName + ".json")
                    .Replace('\\', '/');
                AssetDatabase.ImportAsset(jsonPath, ImportAssetOptions.ForceUpdate);
                string error = TableAddressableRegistrar.Register(jsonPath, result.TableName);
                if (error == null)
                    Debug.Log($"[导表] {result.TableName}.json 导出成功（{result.DataRows} 行 · Addressables 地址：Configs/{result.TableName}）");
                else
                    Debug.LogWarning($"[导表] {result.TableName}.json 已写出，但{error}");
            }
            else
            {
                foreach (var e in result.Errors)
                    Debug.LogError($"[导表] {result.TableName}：{e}");
            }
            return result;
        }

        class Entry
        {
            public string Path;
            public string Name;
            public TableResult Result;
        }

        readonly List<Entry> _entries = new List<Entry>();
        int _selected = -1;
        Vector2 _listScroll, _detailScroll;
        string _lastSummary;

        void OnEnable() => Rescan();

        // 策划改表后切回窗口时自动刷新
        void OnFocus() => Rescan();

        void Rescan()
        {
            _entries.Clear();
            foreach (var path in TableConverter.CollectCsvFiles())
            {
                var entry = new Entry { Path = path, Name = Path.GetFileNameWithoutExtension(path) };
                entry.Result = TableConverter.Convert(path, write: false);
                _entries.Add(entry);
            }
            if (_selected >= _entries.Count) _selected = _entries.Count - 1;
            _lastSummary = _entries.Count == 0
                ? $"未找到表格：请把 CSV（Excel 另存为「CSV UTF-8」）放到 {TableConverter.CsvRoot}/"
                : $"共 {_entries.Count} 张表，点「导出全部」输出到 {TableConverter.OutputRoot}/";
        }

        void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("重新扫描", EditorStyles.toolbarButton, GUILayout.Width(70))) Rescan();
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(_selected < 0 || _entries.Count == 0))
                    if (GUILayout.Button("导出选中", EditorStyles.toolbarButton, GUILayout.Width(70)))
                        Export(_entries[_selected]);
                using (new EditorGUI.DisabledScope(_entries.Count == 0))
                    if (GUILayout.Button("导出全部", EditorStyles.toolbarButton, GUILayout.Width(70))) ExportAll();
            }

            EditorGUILayout.Space(4);
            if (_entries.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    $"把策划表格（Excel 中「另存为 → CSV UTF-8」）放到 {TableConverter.CsvRoot}/ 目录，每个文件 = 一张表。\n" +
                    "约定：第 1 行 = 字段名；第 2 行起 = 数据；第一列 = 主键；单元格内用分号 ; 分隔表示数组。",
                    MessageType.Info);
                EditorGUILayout.LabelField(_lastSummary, EditorStyles.wordWrappedMiniLabel);
                return;
            }

            DrawList();
            EditorGUILayout.Space(4);
            DrawDetail();
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(_lastSummary, EditorStyles.wordWrappedMiniLabel);
        }

        void DrawList()
        {
            float height = Mathf.Min(180, 24 * _entries.Count + 8);
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUI.skin.box, GUILayout.Height(height));
            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                var rect = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
                if (GUI.Button(rect, GUIContent.none)) _selected = i;
                if (i == _selected) EditorGUI.DrawRect(rect, new Color(0.22f, 0.45f, 0.85f, 0.30f));

                bool ok = entry.Result.Ok;
                var iconStyle = ok ? EditorStyles.label : EditorStyles.boldLabel;
                GUI.Label(new Rect(rect.x + 6, rect.y, 22, rect.height), ok ? "√" : "×", iconStyle);
                GUI.Label(new Rect(rect.x + 28, rect.y, rect.width * 0.4f, rect.height),
                    entry.Name, EditorStyles.boldLabel);
                GUI.Label(new Rect(rect.x + rect.width * 0.42f, rect.y, rect.width * 0.30f, rect.height),
                    $"{entry.Result.DataRows} 行 · {entry.Result.FieldNames.Count} 列", EditorStyles.miniLabel);
                if (!ok)
                    GUI.Label(new Rect(rect.x + rect.width * 0.74f, rect.y, rect.width * 0.24f, rect.height),
                        $"{entry.Result.Errors.Count} 个错误", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndScrollView();
        }

        void DrawDetail()
        {
            if (_selected < 0 || _selected >= _entries.Count)
            {
                EditorGUILayout.HelpBox("点击上方表名查看字段与推断类型。", MessageType.None);
                return;
            }

            var result = _entries[_selected].Result;
            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll, GUI.skin.box, GUILayout.ExpandHeight(true));

            EditorGUILayout.LabelField($"{result.TableName}（{result.DataRows} 行数据）", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            if (!result.Ok)
                foreach (var error in result.Errors)
                    EditorGUILayout.HelpBox(error, MessageType.Error);

            if (result.FieldNames.Count > 0)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("字段名", "推断类型", EditorStyles.miniBoldLabel);
                for (int c = 0; c < result.FieldNames.Count; c++)
                    EditorGUILayout.LabelField(result.FieldNames[c],
                        c < result.FieldTypes.Count ? result.FieldTypes[c] : "");
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField(
                    $"主键 = 第一列 · 输出：{TableConverter.OutputRoot}/{result.TableName}.json",
                    EditorStyles.miniLabel);
            }
            EditorGUILayout.EndScrollView();
        }

        void Export(Entry entry)
        {
            entry.Result = ExportTable(entry.Path);
        }

        void ExportAll()
        {
            int ok = 0, fail = 0;
            foreach (var entry in _entries)
            {
                Export(entry);
                if (entry.Result.Ok) ok++; else fail++;
            }
            AssetDatabase.Refresh();
            _lastSummary = fail == 0
                ? $"导出完成：{_entries.Count} 张全部成功 → {TableConverter.OutputRoot}/"
                : $"导出完成：成功 {ok} 张，失败 {fail} 张（错误见 Console 与下方详情）";
            ShowNotification(new GUIContent(_lastSummary));
            Repaint();
        }
    }
}
