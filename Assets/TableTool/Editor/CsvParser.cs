using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TableTool.Editor
{
    /// <summary>RFC4180 CSV 解析：支持引号内的逗号/换行、"" 转义；跳过全空行。</summary>
    public static class CsvParser
    {
        /// <summary>解析 CSV 文本为「行 × 列」矩阵。</summary>
        public static List<string[]> Parse(string text)
        {
            var rows = new List<string[]>();
            var row = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;
            int i = 0, n = text.Length; 

            while (true)
            {
                if (i >= n) // 文件结束：flush 最后一个字段/行
                {
                    row.Add(field.ToString());
                    rows.Add(row.ToArray());
                    break;
                }

                char c = text[i];   
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < n && text[i + 1] == '"') { field.Append('"'); i += 2; }
                        else { inQuotes = false; i++; }
                    }
                    else { field.Append(c); i++; }
                }
                else if (c == '"') { inQuotes = true; i++; }
                else if (c == ',') { row.Add(field.ToString()); field.Clear(); i++; }
                else if (c == '\r' || c == '\n')
                {
                    row.Add(field.ToString()); field.Clear();
                    rows.Add(row.ToArray()); row.Clear();
                    i += (c == '\r' && i + 1 < n && text[i + 1] == '\n') ? 2 : 1;
                }
                else { field.Append(c); i++; }
            }

            // 跳过完全空白的行（Excel 常见的尾随空行）
            rows.RemoveAll(r => r.All(string.IsNullOrEmpty));
            return rows;
        }
    }
}
