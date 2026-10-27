using System.Text;

namespace App.Api.Utilities
{
    /// <summary>
    /// 一行 CSV 数据。LineNumber 是文件里的物理行号（1 开始，含表头），
    /// 报错时必须给这个数字，否则运营在一千行里找不到是哪一行。
    /// </summary>
    public class CsvRow
    {
        public int LineNumber { get; init; }

        public string[] Fields { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// 只做 CSV 解析的最小实现，按 RFC 4180 处理双引号包裹与转义：
    /// 引号内的逗号、换行都算普通字符，"" 表示一个引号本身。
    /// 不引入第三方库，是因为这是唯一需要 CSV 的地方。
    /// </summary>
    public static class CsvTable
    {
        public static List<CsvRow> Parse(string content)
        {
            var rows = new List<CsvRow>();
            if (string.IsNullOrEmpty(content)) return rows;

            // Excel 导出的 UTF-8 BOM 会把第一列表头变成「\uFEFFplatform」，必须先清掉。
            if (content[0] == '\uFEFF') content = content.Substring(1);

            var fields = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;
            var line = 1;
            var rowStartLine = 1;
            var rowStarted = false;

            for (var i = 0; i < content.Length; i++)
            {
                var ch = content[i];

                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        // "" → 一个转义引号；单个 " → 引号区结束。
                        if (i + 1 < content.Length && content[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        if (ch == '\n') line++;
                        field.Append(ch);
                    }

                    continue;
                }

                switch (ch)
                {
                    case '"':
                        if (!rowStarted) { rowStarted = true; rowStartLine = line; }
                        if (field.Length == 0) inQuotes = true;
                        else field.Append('"');
                        break;

                    case ',':
                        if (!rowStarted) { rowStarted = true; rowStartLine = line; }
                        fields.Add(field.ToString());
                        field.Clear();
                        break;

                    case '\r':
                        break;

                    case '\n':
                        if (rowStarted || fields.Count > 0 || field.Length > 0)
                        {
                            fields.Add(field.ToString());
                            field.Clear();
                            AddIfNotBlank(rows, new CsvRow { LineNumber = rowStartLine, Fields = fields.ToArray() });
                            fields.Clear();
                        }

                        rowStarted = false;
                        line++;
                        break;

                    default:
                        if (!rowStarted) { rowStarted = true; rowStartLine = line; }
                        field.Append(ch);
                        break;
                }
            }

            if (rowStarted || fields.Count > 0 || field.Length > 0)
            {
                fields.Add(field.ToString());
                AddIfNotBlank(rows, new CsvRow { LineNumber = rowStartLine, Fields = fields.ToArray() });
            }

            return rows;
        }

        /// <summary>
        /// 去掉全空行（Excel 另存为 CSV 常在末尾留逗号或空行），但保留行号不改。
        /// </summary>
        private static void AddIfNotBlank(List<CsvRow> rows, CsvRow row)
        {
            if (row.Fields.Any(value => !string.IsNullOrWhiteSpace(value)))
            {
                rows.Add(row);
            }
        }
    }
}
