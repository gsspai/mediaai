using System.Globalization;

namespace App.Api.Utilities
{
    /// <summary>
    /// 日指标的录入校验。手工录入与 CSV 导入必须走同一套规则，
    /// 否则「手工能录、导入不能录」会变成最难解释的客服问题。
    /// 所有方法返回中文原因，返回 null 表示通过。
    /// </summary>
    public static class MetricRules
    {
        public const string DateFormat = "yyyy-MM-dd";

        private const int MaxBackYears = 2;

        /// <summary>
        /// 解析统计日期：格式、未来日期、最早回溯两年。
        /// </summary>
        public static bool TryParseDate(string? raw, out DateOnly date, out string error)
        {
            error = string.Empty;
            date = default;

            if (string.IsNullOrWhiteSpace(raw))
            {
                error = "缺少统计日期。";
                return false;
            }

            if (!DateOnly.TryParseExact(raw.Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                error = $"日期格式应为 yyyy-MM-dd，收到「{raw.Trim()}」。";
                return false;
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (date > today)
            {
                error = $"不能录入未来日期的数据（{date:yyyy-MM-dd}）。";
                return false;
            }

            if (date < today.AddYears(-MaxBackYears))
            {
                error = $"只接受近两年的数据（{date:yyyy-MM-dd} 过早），更早的记录请走历史归档。";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 数值校验：非负，且互动数之和不得超过播放数（播放为 0 时跳过）。
        /// </summary>
        public static string? ValidateCounts(long views, long likes, long comments, long shares)
        {
            if (views < 0 || likes < 0 || comments < 0 || shares < 0)
            {
                return "播放、点赞、评论、转发都不能为负。";
            }

            if (views > 0 && likes + comments + shares > views)
            {
                return "互动数之和不能超过播放数，请检查是否填错了列。";
            }

            return null;
        }
    }
}
