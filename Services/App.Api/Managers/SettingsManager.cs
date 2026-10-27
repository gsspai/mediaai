using App.Api.Accessors;
using App.Api.Utilities;
using App.ServiceInvoker.Attributes;
using App.ServiceInvoker.Interfaces;
using Contracts.Domain.Database;
using Contracts.Dto;

namespace App.Api.Managers
{
    /// <summary>
    /// 判定阈值设置。配置只有一份（单例文档），读取时库里有就用库里的，没有就用默认值。
    /// 校验失败统一返回 Success = false + Message，不抛异常，前端直接显示在表单下方。
    /// </summary>
    public class SettingsManager : IManagerService
    {
        // 允许范围。超出范围的输入一律拒绝而不是静默截断，否则界面上显示的值和实际生效的值会不一致。
        private const double MinBreakoutMultiplier = 1.1;
        private const double MaxBreakoutMultiplier = 10;
        private const long MinBreakoutMinViews = 0;
        private const long MaxBreakoutMinViews = 100_000_000;
        private const double MinDeclinePercent = 1;
        private const double MaxDeclinePercent = 90;
        private const double MinInteractionPercent = 0.1;
        private const double MaxInteractionPercent = 50;
        private const int MinStaleDays = 1;
        private const int MaxStaleDays = 30;

        private readonly IDatabaseAccessor _databaseAccessor;
        private readonly UserContextService _userContextService;

        public SettingsManager(IDatabaseAccessor databaseAccessor, UserContextService userContextService)
        {
            _databaseAccessor = databaseAccessor;
            _userContextService = userContextService;
        }

        [RequireAuthenticated]
        public async Task<SignalThresholdDto> GetSignalThresholds(GetSignalThresholdsRequestDto request)
        {
            var config = await SignalThresholdStore.LoadAsync(_databaseAccessor);
            return ToDto(config);
        }

        /// <summary>
        /// 保存阈值并立刻生效。保存后首页的判定规则文案会跟着变，因为文案是按这份配置生成的。
        /// </summary>
        [RequireAuthenticated]
        public async Task<SaveSignalThresholdResultDto> SaveSignalThresholds(SaveSignalThresholdRequestDto request)
        {
            var error = Validate(request);
            if (error != null)
            {
                return new SaveSignalThresholdResultDto
                {
                    Success = false,
                    Message = error,
                    Thresholds = ToDto(await SignalThresholdStore.LoadAsync(_databaseAccessor))
                };
            }

            var config = new SignalThresholdConfig
            {
                Id = SignalThresholdDefaults.SingletonId,
                BreakoutMultiplier = Math.Round(request!.BreakoutMultiplier, 2),
                BreakoutMinViews = request.BreakoutMinViews,
                DeclineThresholdPercent = Math.Round(request.DeclineThresholdPercent, 1),
                LowInteractionRatePercent = Math.Round(request.LowInteractionRatePercent, 1),
                StaleDays = request.StaleDays,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = _userContextService.UserEmail ?? _userContextService.UserId ?? string.Empty
            };

            await _databaseAccessor.UpsertDocument(config);

            return new SaveSignalThresholdResultDto
            {
                Success = true,
                Message = "阈值已保存，首页的机会与风险会按新口径重新计算。",
                Thresholds = ToDto(config)
            };
        }

        private static string? Validate(SaveSignalThresholdRequestDto? request)
        {
            if (request == null)
            {
                return "请求内容为空。";
            }

            if (!double.IsFinite(request.BreakoutMultiplier)
                || request.BreakoutMultiplier < MinBreakoutMultiplier
                || request.BreakoutMultiplier > MaxBreakoutMultiplier)
            {
                return $"爆款倍数必须在 {MinBreakoutMultiplier:0.#} 到 {MaxBreakoutMultiplier:0} 之间。";
            }

            if (request.BreakoutMinViews < MinBreakoutMinViews || request.BreakoutMinViews > MaxBreakoutMinViews)
            {
                return $"爆款播放下限必须在 {MinBreakoutMinViews:N0} 到 {MaxBreakoutMinViews:N0} 之间。";
            }

            if (!double.IsFinite(request.DeclineThresholdPercent)
                || request.DeclineThresholdPercent < MinDeclinePercent
                || request.DeclineThresholdPercent > MaxDeclinePercent)
            {
                return $"掉速幅度必须在 {MinDeclinePercent:0}% 到 {MaxDeclinePercent:0}% 之间。";
            }

            if (!double.IsFinite(request.LowInteractionRatePercent)
                || request.LowInteractionRatePercent < MinInteractionPercent
                || request.LowInteractionRatePercent > MaxInteractionPercent)
            {
                return $"互动率阈值必须在 {MinInteractionPercent:0.#}% 到 {MaxInteractionPercent:0}% 之间。";
            }

            if (request.StaleDays < MinStaleDays || request.StaleDays > MaxStaleDays)
            {
                return $"停更天数必须在 {MinStaleDays} 到 {MaxStaleDays} 天之间。";
            }

            return null;
        }

        private static SignalThresholdDto ToDto(SignalThresholdConfig config) => new()
        {
            BreakoutMultiplier = config.BreakoutMultiplier,
            BreakoutMinViews = config.BreakoutMinViews,
            DeclineThresholdPercent = config.DeclineThresholdPercent,
            LowInteractionRatePercent = config.LowInteractionRatePercent,
            StaleDays = config.StaleDays,
            IsCustomized = SignalThresholdStore.IsCustomized(config),
            UpdatedAt = config.UpdatedAt == default
                ? string.Empty
                : config.UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
            UpdatedBy = config.UpdatedBy,
            Policy = SignalPolicyText.Describe(SignalPolicyText.PreviewDays, config)
        };
    }
}
