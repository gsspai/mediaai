using System.Globalization;
using App.Api.Accessors;
using App.Api.Utilities;
using App.ServiceInvoker.Attributes;
using App.ServiceInvoker.Interfaces;
using Contracts.Domain.Database;
using Contracts.Dto;

namespace App.Api.Managers
{
    /// <summary>
    /// 日指标写入与查询。当前只做手工录入；接口同步（平台 API / CSV）留到 M1-4 与 M4，
    /// 但落库时已经把 Source / RecordedBy 写入，后续两种来源可以共存且可区分。
    /// </summary>
    public class MetricManager : IManagerService
    {
        private const string DateFormat = "yyyy-MM-dd";
        private const int DefaultTake = 20;
        private const int MaxTake = 100;

        // CSV 导入上限：2000 行足够覆盖一个账号五六年的日更，再大就该走接口同步了。
        private const int MaxImportRows = 2000;
        private const int MaxImportErrors = 20;
        private const int MaxImportChars = 4 * 1024 * 1024;

        private static readonly string[] RequiredColumns = { "platform", "account", "date", "views" };

        /// <summary>
        /// 表头别名。运营从 Excel 导出的往往是中文列名，强制英文列头会逼着人手工改表头。
        /// </summary>
        private static readonly Dictionary<string, string[]> CsvHeaderAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["platform"] = new[] { "platform", "平台", "平台标识" },
            ["account"] = new[] { "account", "handle", "账号", "账号标识" },
            ["date"] = new[] { "date", "日期", "统计日期" },
            ["views"] = new[] { "views", "播放", "播放量", "阅读", "阅读量" },
            ["likes"] = new[] { "likes", "点赞" },
            ["comments"] = new[] { "comments", "评论" },
            ["shares"] = new[] { "shares", "转发", "分享" },
            ["followers_new"] = new[] { "followers_new", "followers", "新增粉丝", "涨粉" }
        };

        private readonly IDatabaseAccessor _databaseAccessor;
        private readonly UserContextService _userContextService;

        public MetricManager(IDatabaseAccessor databaseAccessor, UserContextService userContextService)
        {
            _databaseAccessor = databaseAccessor;
            _userContextService = userContextService;
        }

        /// <summary>
        /// 单日手工录入。同一账号同一天重复提交按覆盖处理，不会产生重复行，
        /// 并把 RecordedBy 更新为本次录入人，保证「谁改的」可回溯。
        /// </summary>
        [RequireAuthenticated]
        public async Task<MetricIngestResultDto> IngestManual(IngestManualMetricsRequestDto request)
        {
            if (request == null || request.AccountId == Guid.Empty)
            {
                return Fail("缺少账号 ID。");
            }

            if (!MetricRules.TryParseDate(request.StatDate, out var statDate, out var dateError))
            {
                return Fail(dateError);
            }

            var countError = MetricRules.ValidateCounts(request.Views, request.Likes, request.Comments, request.Shares);
            if (countError != null)
            {
                return Fail(countError);
            }

            var account = await FindVisibleAccount(request.AccountId);
            if (account == null)
            {
                return Fail("账号不存在，或不属于当前用户。");
            }

            if (account.Status == PlatformAccountStatus.Paused)
            {
                return Fail($"「{account.DisplayName}」已停用，不能继续录入。");
            }

            var existing = await _databaseAccessor
                .GetDocumentByProperty<MetricDaily>(metric => metric.AccountId == account.Id && metric.StatDate == statDate);

            var recordedBy = _userContextService.UserEmail
                             ?? _userContextService.UserId
                             ?? "unknown";

            var updated = existing != null;
            var document = existing ?? new MetricDaily
            {
                AccountId = account.Id,
                StatDate = statDate
            };

            document.Platform = account.Platform;
            document.Views = request.Views;
            document.Likes = request.Likes;
            document.Comments = request.Comments;
            document.Shares = request.Shares;
            document.NewFollowers = request.NewFollowers;
            document.Source = MetricSource.Manual;
            document.RecordedBy = recordedBy;
            document.RecordedAt = DateTimeOffset.UtcNow;

            if (updated)
            {
                await _databaseAccessor.UpsertDocument(document);
            }
            else
            {
                await _databaseAccessor.InsertDocument(document);
            }

            return new MetricIngestResultDto
            {
                Success = true,
                Updated = updated,
                Message = updated
                    ? $"{account.DisplayName} {statDate:yyyy-MM-dd} 的记录已覆盖。"
                    : $"{account.DisplayName} {statDate:yyyy-MM-dd} 的记录已保存。",
                Entry = ToEntry(document, account)
            };
        }

        /// <summary>
        /// 最近录入记录，用于核对「这个数字是谁哪天录的」。
        /// 只返回当前用户可见账号的数据。
        /// </summary>
        [RequireAuthenticated]
        public async Task<MetricEntryListDto> ListRecentMetrics(ListRecentMetricsRequestDto request)
        {
            var accounts = await LoadVisibleAccounts();
            var accountMap = accounts.ToDictionary(account => account.Id);
            var filterId = request?.AccountId ?? Guid.Empty;

            if (filterId != Guid.Empty && !accountMap.ContainsKey(filterId))
            {
                return new MetricEntryListDto();
            }

            var take = Math.Clamp(request?.Take ?? DefaultTake, 1, MaxTake);
            var metrics = (await _databaseAccessor.GetAllDocuments<MetricDaily>()).ToList();

            return new MetricEntryListDto
            {
                Items = metrics
                    .Where(metric => accountMap.ContainsKey(metric.AccountId))
                    .Where(metric => filterId == Guid.Empty || metric.AccountId == filterId)
                    .OrderByDescending(metric => metric.StatDate)
                    .ThenByDescending(metric => metric.RecordedAt)
                    .Take(take)
                    .Select(metric => ToEntry(metric, accountMap[metric.AccountId]))
                    .ToList()
            };
        }

        /// <summary>
        /// CSV 批量导入。逐行校验，失败的行走同一套规则拒绝，并回传物理行号；
        /// 全部通过的行一次性批量写入，1000 行只产生一次落盘。
        /// 表头支持中英文（运营手写的往往是中文列名）。
        /// </summary>
        [RequireAuthenticated]
        public async Task<ImportMetricsCsvResultDto> ImportCsv(ImportMetricsCsvRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Content))
            {
                return ImportFail("CSV 内容为空。");
            }

            if (request.Content.Length > MaxImportChars)
            {
                return ImportFail($"文件过大（约 {request.Content.Length / 1024 / 1024} MB），单次最多 4 MB，请拆分文件。");
            }

            var fileName = (request.FileName ?? string.Empty).Trim();
            if (fileName.Length > 0 && !fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return ImportFail("只支持 .csv 文件；Excel 请先「另存为 CSV UTF-8」。");
            }

            var rows = CsvTable.Parse(request.Content);
            if (rows.Count == 0)
            {
                return ImportFail("没有解析到任何内容，请确认文件是 CSV 文本而不是 Excel 工作簿。");
            }

            var header = ResolveHeader(rows[0].Fields);
            if (header.Error != null)
            {
                return ImportFail(header.Error);
            }

            var dataRows = rows.Skip(1).ToList();
            if (dataRows.Count == 0)
            {
                return ImportFail("只有表头，没有数据行。");
            }

            if (dataRows.Count > MaxImportRows)
            {
                return ImportFail($"单次最多导入 {MaxImportRows} 行，当前 {dataRows.Count} 行，请拆分文件。");
            }

            var accounts = await LoadVisibleAccounts();
            var accountMap = new Dictionary<string, PlatformAccount>();
            foreach (var account in accounts)
            {
                accountMap[AccountKey(account.Platform, account.Handle)] = account;
            }

            var errors = new List<MetricImportErrorDto>();
            var failed = 0;
            var duplicates = 0;
            var pending = new Dictionary<(Guid AccountId, DateOnly Date), MetricImportEntry>();

            foreach (var row in dataRows)
            {
                var rowError = TryReadRow(row, header.Index, accountMap, out var entry);
                if (rowError != null)
                {
                    failed++;
                    if (errors.Count < MaxImportErrors)
                    {
                        errors.Add(new MetricImportErrorDto { RowNumber = row.LineNumber, Message = rowError });
                    }

                    continue;
                }

                // 同一文件里同账号同日出现多次：后出现的覆盖前面的，并告知用户丢了几行。
                if (pending.ContainsKey((entry.AccountId, entry.Date)))
                {
                    duplicates++;
                }

                pending[(entry.AccountId, entry.Date)] = entry;
            }

            var recordedBy = _userContextService.UserEmail
                             ?? _userContextService.UserId
                             ?? "unknown";
            var recordedAt = DateTimeOffset.UtcNow;

            var existing = new Dictionary<(Guid AccountId, DateOnly Date), MetricDaily>();
            foreach (var metric in await _databaseAccessor.GetAllDocuments<MetricDaily>())
            {
                existing[(metric.AccountId, metric.StatDate)] = metric;
            }

            var created = 0;
            var updated = 0;
            var batch = new List<MetricDaily>();

            foreach (var entry in pending.Values)
            {
                MetricDaily document;
                if (existing.TryGetValue((entry.AccountId, entry.Date), out var found))
                {
                    document = found;
                    updated++;
                }
                else
                {
                    document = new MetricDaily { AccountId = entry.AccountId, StatDate = entry.Date };
                    created++;
                }

                document.Platform = entry.Platform;
                document.Views = entry.Views;
                document.Likes = entry.Likes;
                document.Comments = entry.Comments;
                document.Shares = entry.Shares;
                document.NewFollowers = entry.NewFollowers;
                document.Source = MetricSource.Csv;
                document.RecordedBy = recordedBy;
                document.RecordedAt = recordedAt;
                batch.Add(document);
            }

            if (batch.Count > 0)
            {
                await _databaseAccessor.UpsertDocuments(batch);
            }

            var parts = new List<string>();
            if (created > 0) parts.Add($"新增 {created} 行");
            if (updated > 0) parts.Add($"覆盖 {updated} 行");
            if (duplicates > 0) parts.Add($"文件内重复 {duplicates} 行（以后出现的为准）");
            if (failed > 0) parts.Add($"失败 {failed} 行");

            return new ImportMetricsCsvResultDto
            {
                Success = batch.Count > 0,
                Message = batch.Count == 0
                    ? $"全部 {failed} 行都没通过校验，未写入任何数据。"
                    : $"导入完成：{string.Join("，", parts)}。",
                TotalRows = dataRows.Count,
                CreatedRows = created,
                UpdatedRows = updated,
                DuplicateRows = duplicates,
                FailedRows = failed,
                Errors = errors,
                ErrorsTruncated = failed > errors.Count,
                Warning = header.Unknown.Count == 0
                    ? string.Empty
                    : $"已忽略未识别的列：{string.Join("、", header.Unknown)}"
            };
        }

        /// <summary>
        /// 读取一行并校验。返回 null 表示通过。
        /// 任何一条不合格都不写入，避免「一半导入成功」这种最难对账的状态。
        /// </summary>
        private static string? TryReadRow(
            CsvRow row,
            Dictionary<string, int> header,
            Dictionary<string, PlatformAccount> accountMap,
            out MetricImportEntry entry)
        {
            entry = null!;

            var needed = header.Values.Max() + 1;
            if (row.Fields.Length < needed)
            {
                return $"只有 {row.Fields.Length} 列，少于表头的 {needed} 列。";
            }

            var platformRaw = ReadField(row, header, "platform");
            if (!PlatformCatalog.IsSupported(platformRaw))
            {
                return $"不支持的平台「{platformRaw}」，可选：{string.Join(" / ", PlatformCatalog.Supported)}。";
            }

            var platform = PlatformCatalog.Normalize(platformRaw);

            var handle = ReadField(row, header, "account");
            if (string.IsNullOrWhiteSpace(handle))
            {
                return "账号标识为空。";
            }

            if (!MetricRules.TryParseDate(ReadField(row, header, "date"), out var statDate, out var dateError))
            {
                return dateError;
            }

            if (!TryReadLong(ReadField(row, header, "views"), out var views))
            {
                return $"播放「{ReadField(row, header, "views").Trim()}」不是整数。";
            }

            if (string.IsNullOrWhiteSpace(ReadField(row, header, "views")))
            {
                return "播放为空。可以填 0，但不能留空，否则无法判断这行是不是漏填了。";
            }

            if (!TryReadLong(ReadField(row, header, "likes"), out var likes))
            {
                return $"点赞「{ReadField(row, header, "likes").Trim()}」不是整数。";
            }

            if (!TryReadLong(ReadField(row, header, "comments"), out var comments))
            {
                return $"评论「{ReadField(row, header, "comments").Trim()}」不是整数。";
            }

            if (!TryReadLong(ReadField(row, header, "shares"), out var shares))
            {
                return $"转发「{ReadField(row, header, "shares").Trim()}」不是整数。";
            }

            if (!TryReadLong(ReadField(row, header, "followers_new"), out var newFollowers))
            {
                return $"新增粉丝「{ReadField(row, header, "followers_new").Trim()}」不是整数。";
            }

            if (newFollowers is < int.MinValue or > int.MaxValue)
            {
                return "新增粉丝超出可存范围。";
            }

            var countError = MetricRules.ValidateCounts(views, likes, comments, shares);
            if (countError != null)
            {
                return countError;
            }

            if (!accountMap.TryGetValue(AccountKey(platform, handle), out var account))
            {
                return $"平台 {platform} 下没有账号「{handle.Trim()}」，请先在平台连接页创建该账号。";
            }

            if (account.Status == PlatformAccountStatus.Paused)
            {
                return $"「{account.DisplayName}」已停用，不能导入数据。";
            }

            entry = new MetricImportEntry
            {
                AccountId = account.Id,
                Platform = account.Platform,
                Date = statDate,
                Views = views,
                Likes = likes,
                Comments = comments,
                Shares = shares,
                NewFollowers = (int)newFollowers
            };

            return null;
        }

        private static string ReadField(CsvRow row, Dictionary<string, int> header, string key)
        {
            if (!header.TryGetValue(key, out var index) || index >= row.Fields.Length)
            {
                return string.Empty;
            }

            return row.Fields[index] ?? string.Empty;
        }

        /// <summary>
        /// 允许空值（按 0 处理）、允许千分位逗号，也允许 Excel 导出的 12000.0。
        /// 非整数的写法直接判错，不做静默四舍五入——那会让总数对不上而查不出来。
        /// </summary>
        private static bool TryReadLong(string raw, out long value)
        {
            value = 0;
            var text = (raw ?? string.Empty).Trim();
            if (text.Length == 0) return true;

            if (long.TryParse(text, NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue)
                && decimalValue == Math.Truncate(decimalValue)
                && decimalValue >= long.MinValue
                && decimalValue <= long.MaxValue)
            {
                value = (long)decimalValue;
                return true;
            }

            return false;
        }

        private static string AccountKey(string? platform, string? handle) =>
            $"{PlatformCatalog.Normalize(platform ?? string.Empty)}|{NormalizeHandle(handle)}";

        private static string NormalizeHandle(string? handle) =>
            (handle ?? string.Empty).Trim().TrimStart('@').ToLowerInvariant();

        private static (Dictionary<string, int> Index, List<string> Unknown, string? Error) ResolveHeader(string[] fields)
        {
            var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var unknown = new List<string>();

            for (var i = 0; i < fields.Length; i++)
            {
                var raw = (fields[i] ?? string.Empty).Trim().Trim('"');
                if (raw.Length == 0) continue;

                var matched = CsvHeaderAliases
                    .FirstOrDefault(pair => pair.Value.Any(alias => string.Equals(alias, raw, StringComparison.OrdinalIgnoreCase)))
                    .Key;

                if (matched == null)
                {
                    unknown.Add(raw);
                    continue;
                }

                index.TryAdd(matched, i);
            }

            var missing = RequiredColumns.Where(column => !index.ContainsKey(column)).ToList();
            if (missing.Count > 0)
            {
                return (index, unknown, $"表头缺少必需的列：{string.Join("、", missing)}。表头示例：platform,account,date,views,likes,comments,shares,followers_new");
            }

            return (index, unknown, null);
        }

        private static ImportMetricsCsvResultDto ImportFail(string message) =>
            new() { Success = false, Message = message };

        private async Task<PlatformAccount?> FindVisibleAccount(Guid accountId)
        {
            var account = await _databaseAccessor.GetDocumentById<PlatformAccount>(accountId);
            if (account == null) return null;

            return AccountVisibility.IsVisible(account.OwnerId, _userContextService.UserId) ? account : null;
        }

        private async Task<List<PlatformAccount>> LoadVisibleAccounts()
        {
            var userId = _userContextService.UserId;
            var all = (await _databaseAccessor.GetAllDocuments<PlatformAccount>()).ToList();
            return all.Where(account => AccountVisibility.IsVisible(account.OwnerId, userId)).ToList();
        }

        private static MetricEntryDto ToEntry(MetricDaily metric, PlatformAccount account) =>
            new()
            {
                Id = metric.Id,
                AccountId = metric.AccountId,
                Platform = metric.Platform,
                AccountName = account.DisplayName,
                StatDate = metric.StatDate.ToString(DateFormat),
                Views = metric.Views,
                Likes = metric.Likes,
                Comments = metric.Comments,
                Shares = metric.Shares,
                NewFollowers = metric.NewFollowers,
                Source = metric.Source,
                RecordedBy = metric.RecordedBy,
                RecordedAt = metric.RecordedAt.ToString("yyyy-MM-dd HH:mm")
            };

        private static MetricIngestResultDto Fail(string message) =>
            new() { Success = false, Message = message };
    }
}
