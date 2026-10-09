using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace TableTool.Editor
{
    /// <summary>单张表的解析、类型推断与导出结果。</summary>
    public class TableResult
    {
        public string TableName;
        public string CsvPath;
        public int DataRows;
        public List<string> FieldNames = new List<string>();
        public List<string> FieldTypes = new List<string>();
        public List<string> Errors = new List<string>();
        public bool Ok => Errors.Count == 0;
    }

    /// <summary>CSV → JSON 转换核心：编码识别、单行头类型自动推断、JSON 序列化、文件导出。</summary>
    public static class TableConverter
    {
        public const string CsvRoot = "Assets/Config";
        public const string OutputRoot = "Assets/AddressableAssets/Configs";

        static readonly string[] ArraySeparator = { ";" };

        // ---------- 对外入口 ----------

        /// <summary>扫描 CsvRoot 下所有 CSV 路径（按文件名排序）。</summary>
        public static List<string> CollectCsvFiles()
        {
            var root = Path.GetFullPath(CsvRoot);
            var list = new List<string>();
            if (!Directory.Exists(root)) return list;
            list.AddRange(Directory.GetFiles(root, "*.csv"));
            list.Sort(StringComparer.OrdinalIgnoreCase);
            return list;
        }

        /// <summary>解析并转换一张表；write=true 时写出 JSON 文件。失败只记 Errors，不抛异常。</summary>
        public static TableResult Convert(string csvPath, bool write)
        {
            var result = new TableResult
            {
                CsvPath = csvPath,
                TableName = Path.GetFileNameWithoutExtension(csvPath),
            };
            try
            {
                var rows = CsvParser.Parse(ReadCsvText(csvPath));
                Validate(result, rows);
                if (!result.Ok) return result;

                var data = rows.GetRange(1, rows.Count - 1);
                result.DataRows = data.Count;

                var kinds = InferColumnKinds(result.FieldNames.Count, data);
                for (int c = 0; c < result.FieldNames.Count; c++)
                    result.FieldTypes.Add(KindName(kinds[c]));

                if (write)
                {
                    var json = BuildJson(result.FieldNames, data, kinds);
                    Directory.CreateDirectory(OutputRoot);
                    File.WriteAllText(Path.Combine(OutputRoot, result.TableName + ".json"),
                        json, new UTF8Encoding(false));
                }
                return result;
            }
            catch (Exception e)
            {
                result.Errors.Add($"转换异常：{e.Message}");
                return result;
            }
        }

        // ---------- 校验 ----------

        static void Validate(TableResult result, List<string[]> rows)
        {
            if (rows.Count < 2)
            {
                result.Errors.Add("表格为空：至少需要 1 行字段名 + 1 行数据");
                return;
            }

            // 第 1 行 = 字段名
            var header = rows[0];
            var names = new List<string>();
            for (int c = 0; c < header.Length; c++)
            {
                string name = (header[c] ?? "").Trim();
                if (name.Length == 0)
                {
                    result.Errors.Add($"第 {c + 1} 列字段名为空");
                    continue;
                }
                if (names.Contains(name)) result.Errors.Add($"字段名重复：{name}");
                names.Add(name);
            }
            result.FieldNames = names;

            // 主键（第一列）查重 + 越界数据检查
            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                string key = Cell(row, 0);
                if (string.IsNullOrEmpty(key))
                {
                    result.Errors.Add($"第 {r + 1} 行主键为空（第一列）");
                    continue;
                }
                if (!keys.Add(key)) result.Errors.Add($"主键重复：{key}（第 {r + 1} 行）");

                if (row.Length > names.Count)
                    for (int c = names.Count; c < row.Length; c++)
                        if (!string.IsNullOrEmpty(row[c]))
                        {
                            result.Errors.Add($"第 {r + 1} 行第 {c + 1} 列超出字段范围（表头共 {names.Count} 列）");
                            break;
                        }
            }
        }

        // ---------- 类型推断 ----------

        enum ScalarKind { Null, Bool, Long, Double, String }

        struct ColumnKind
        {
            public bool IsArray;
            public ScalarKind Element;
        }

        /// <summary>列内任意非空单元格含 ';' 即视为数组列；元素类型取全列合并。</summary>
        static ColumnKind[] InferColumnKinds(int colCount, List<string[]> data)
        {
            var kinds = new ColumnKind[colCount];
            for (int c = 0; c < colCount; c++)
            {
                ScalarKind acc = ScalarKind.Null;
                bool isArray = false;
                for (int r = 0; r < data.Count; r++)
                {
                    string cell = Cell(data[r], c);
                    if (cell == null) continue;
                    string[] parts = cell.Split(ArraySeparator, StringSplitOptions.None);
                    isArray |= parts.Length > 1;
                    foreach (var part in parts)
                        acc = Merge(acc, Classify(part));
                }
                if (acc == ScalarKind.Null) acc = ScalarKind.String; // 整列全空 → string
                kinds[c] = new ColumnKind { IsArray = isArray, Element = acc };
            }
            return kinds;
        }

        static ScalarKind Classify(string s)
        {
            if (s.Length == 0) return ScalarKind.Null;
            if (bool.TryParse(s, out _)) return ScalarKind.Bool;
            if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)) return ScalarKind.Long;
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out _)) return ScalarKind.Double;
            return ScalarKind.String;
        }

        /// <summary>类型合并：Null 吸收；Long+Double → Double；其余混合（含 bool 混杂）→ String。</summary>
        static ScalarKind Merge(ScalarKind a, ScalarKind b)
        {
            if (a == ScalarKind.Null) return b;
            if (b == ScalarKind.Null) return a;
            if (a == b) return a;
            if ((a == ScalarKind.Long && b == ScalarKind.Double) || (a == ScalarKind.Double && b == ScalarKind.Long))
                return ScalarKind.Double;
            return ScalarKind.String;
        }

        static string KindName(ColumnKind kind)
        {
            string name = kind.Element switch
            {
                ScalarKind.Bool => "bool",
                ScalarKind.Long => "int",
                ScalarKind.Double => "float",
                _ => "string",
            };
            return kind.IsArray ? name + "[]" : name;
        }

        // ---------- JSON 序列化 ----------

        /// <summary>输出主键索引对象：{"1001":{字段:值,...},...}，2 空格缩进。</summary>
        static string BuildJson(List<string> names, List<string[]> data, ColumnKind[] kinds)
        {
            var sb = new StringBuilder(4096);
            sb.AppendLine("{");
            for (int r = 0; r < data.Count; r++)
            {
                sb.Append("  ").Append(Escape(Cell(data[r], 0) ?? string.Empty)).Append(": {");
                for (int c = 0; c < names.Count; c++)
                {
                    if (c > 0) sb.Append(',');
                    sb.AppendLine();
                    sb.Append("    ").Append(Escape(names[c])).Append(": ")
                      .Append(CellJson(Cell(data[r], c), kinds[c]));
                }
                sb.AppendLine();
                sb.Append("  }");
                if (r < data.Count - 1) sb.Append(',');
                sb.AppendLine();
            }
            sb.AppendLine("}");
            return sb.ToString();
        }

        /// <summary>按列类型把单元格序列化为 JSON 值；空单元格 → null。</summary>
        static string CellJson(string cell, ColumnKind kind)
        {
            if (cell == null) return "null";
            if (!kind.IsArray) return ScalarJson(cell, kind.Element);

            var parts = cell.Split(ArraySeparator, StringSplitOptions.None);
            var sb = new StringBuilder();
            sb.Append('[');
            for (int i = 0; i < parts.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(parts[i].Length == 0 ? "null" : ScalarJson(parts[i], kind.Element));
            }
            sb.Append(']');
            return sb.ToString();
        }

        static string ScalarJson(string s, ScalarKind kind)
        {
            switch (kind)
            {
                case ScalarKind.Bool:
                    return bool.TryParse(s, out var b) ? (b ? "true" : "false") : "null";
                case ScalarKind.Long:
                    return long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l)
                        ? l.ToString(CultureInfo.InvariantCulture) : "null";
                case ScalarKind.Double:
                    return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
                        ? d.ToString("R", CultureInfo.InvariantCulture) : "null";
                default:
                    return Escape(s);
            }
        }

        /// <summary>JSON 字符串转义（含引号、反斜杠、控制字符）。</summary>
        static string Escape(string s)
        {
            var sb = new StringBuilder(s.Length + 2);
            sb.Append('"');
            foreach (var ch in s)
            {
                switch (ch)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (ch < 0x20) sb.Append("\\u").Append(((int)ch).ToString("x4"));
                        else sb.Append(ch);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        // ---------- 工具与编码 ----------

        /// <summary>取第 r 行第 c 列的值（越界视为空）；统一 Trim，空串归一为 null。</summary>
        static string Cell(string[] row, int c)
        {
            if (c >= row.Length) return null;
            string s = (row[c] ?? "").Trim();
            return s.Length == 0 ? null : s;
        }

        /// <summary>读 CSV：UTF-8 BOM → 严格 UTF-8 → GB18030（中文 Windows 上 Excel 另存 "CSV" 默认 ANSI 编码）。</summary>
        static string ReadCsvText(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return new UTF8Encoding(false).GetString(bytes, 3, bytes.Length - 3);
            try
            {
                return new UTF8Encoding(false, true).GetString(bytes); // 严格 UTF-8 校验
            }
            catch (DecoderFallbackException) { }

            if (TryGetGbEncoding(out var gb)) return gb.GetString(bytes);
            throw new Exception("文件不是 UTF-8 编码，且当前 Unity 运行时无法识别 GB 编码；请在 Excel 中「另存为 → CSV UTF-8」后重试");
        }

        static Encoding _gbEncoding;
        static bool _gbProbed;

        /// <summary>反射注册 CodePagesEncodingProvider（netstandard2.1 profile 下该类型可能缺失，只探测一次，失败则降级报错）。</summary>
        static bool TryGetGbEncoding(out Encoding gb)
        {
            gb = _gbEncoding;
            if (_gbProbed) return gb != null;
            _gbProbed = true;
            try
            {
                var providerType = Type.GetType(
                    "System.Text.CodePagesEncodingProvider, System.Text.Encoding.CodePages", throwOnError: false);
                var instance = providerType?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                typeof(Encoding).GetMethod("RegisterProvider", BindingFlags.Public | BindingFlags.Static)
                    ?.Invoke(null, new[] { instance });
                gb = _gbEncoding = Encoding.GetEncoding("GB18030");
                return true;
            }
            catch
            {
                gb = _gbEncoding = null;
                return false;
            }
        }
    }
}
