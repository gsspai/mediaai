import { SampleEnum } from "./Enums";

export interface ChangePasswordRequestDto {
  NewPassword: string;
}

export interface GetSessionRequestDto {
}

export interface LoginRequestDto {
  Email: string;
  Password: string;
}

export interface LoginResponseDto {
  IsSuccess: boolean;
  ErrorMessage: string | null;
  AccessToken: string | null;
  RefreshToken: string | null;
  Session: SessionDto | null;
}

export interface OperationResultDto {
  Success: boolean;
  Message: string;
}

export interface SendPasswordResetRequestDto {
  Email: string;
  RedirectUrl: string | null;
}

export interface ServiceInvocationRequestDto {
  ManagerName: string;
  MethodName: string;
  Parameters: (any | null)[] | null;
  AccessToken: string | null;
  RefreshToken: string | null;
}

export interface ServiceInvocationResponseEnvelopeDto {
  Result: any | null;
  AccessToken: string | null;
  RefreshToken: string | null;
  Session: SessionDto | null;
}

export interface ServiceStreamingRequestDto {
  ManagerName: string;
  MethodName: string;
  Parameters: (any | null)[] | null;
  AccessToken: string | null;
  RefreshToken: string | null;
}

export interface SessionDto {
  UserId: string | null;
  Email: string | null;
  Roles: string[];
}

export interface SignUpRequestDto {
  Email: string;
  Password: string;
  Name: string | null;
}

export interface SignUpResponseDto {
  IsSuccess: boolean;
  ErrorMessage: string | null;
  RequiresFollowUp: boolean;
  AccessToken: string | null;
  RefreshToken: string | null;
  Session: SessionDto | null;
}

export interface StreamingAuthMetaEventDto {
  Type: string;
  AccessToken: string | null;
  RefreshToken: string | null;
  Session: SessionDto | null;
}

export interface UpdateUserEmailDto {
  NewEmail: string;
}

export interface UpdateUserEmailResponseDto {
  Success: boolean;
  Message: string;
}

export interface UpdateUserNameDto {
  NewName: string;
}

export interface UpdateUserNameResponseDto {
  Success: boolean;
  Message: string;
}

export interface UpdateUserPasswordDto {
  NewPassword: string;
}

export interface UpdateUserPasswordResponseDto {
  Success: boolean;
  Message: string;
}


export interface GetDashboardSummaryRequestDto {
  Days: number;
}

export interface DashboardTotalsDto {
  Views: number;
  Interactions: number;
  NewFollowers: number;
  InteractionRate: number;
}

export interface DashboardKpiDto {
  Key: string;
  Label: string;
  Display: string;
  Unit: string;
  Detail: string;
  IsText: boolean;
  Status: 'ready' | 'pending';
  Trace: string;
}

export interface DashboardTrendPointDto {
  Label: string;
  Views: number;
  Comments: number;
  Shares: number;
}

export interface DashboardPlatformTrendDto {
  Platform: string;
  Points: DashboardTrendPointDto[];
  ChangeRate: string;
  Tone: string;
}

export interface DashboardDataSourceDto {
  ManualCount: number;
  CsvCount: number;
  ApiCount: number;
  SeedCount: number;
  Label: string;
}

export interface DashboardSummaryDto {
  RangeStart: string;
  RangeEnd: string;
  HasData: boolean;
  AccountCount: number;
  Totals: DashboardTotalsDto;
  Kpis: DashboardKpiDto[];
  PlatformTrends: DashboardPlatformTrendDto[];
  DataSource: DashboardDataSourceDto;
  Opportunities: DashboardSignalDto[];
  Risks: DashboardSignalDto[];
  SignalPolicy: string;
  SignalPolicyTrace: string;
}

/** 机会 / 风险区块的一行，Title 是账号展示名，Trace 说明触发依据。 */
export interface DashboardSignalDto {
  Key: string;
  Title: string;
  Platform: string;
  Signal: string;
  Detail: string;
  Tone: string;
  Trace: string;
}

// ---------- 平台账号 · PlatformAccountManager ----------

export interface PlatformAccountDto {
  Id: string;
  Platform: string;
  Handle: string;
  DisplayName: string;
  Status: string;
  FollowerCount: number;
  OwnerId: string;
  /** 最近一条日记录日期（yyyy-MM-dd），没有记录时为 null */
  LastMetricDate: string | null;
  RecentMetricCount: number;
  CreatedAt: string;
  UpdatedAt: string;
}

export interface PlatformAccountListDto {
  Items: PlatformAccountDto[];
}

export interface ListAccountsRequestDto {
  IncludePaused: boolean;
}

export interface SaveAccountRequestDto {
  Id: string | null;
  Platform: string;
  Handle: string;
  DisplayName: string;
  Status: string;
  FollowerCount: number;
}

export interface SaveAccountResultDto {
  Success: boolean;
  Message: string;
  Account: PlatformAccountDto | null;
}

export interface AccountIdRequestDto {
  AccountId: string;
}

// ---------- 日指标 · MetricManager ----------

export interface MetricEntryDto {
  Id: string;
  AccountId: string;
  Platform: string;
  AccountName: string;
  StatDate: string;
  Views: number;
  Likes: number;
  Comments: number;
  Shares: number;
  NewFollowers: number;
  Source: string;
  RecordedBy: string;
  RecordedAt: string;
}

export interface MetricEntryListDto {
  Items: MetricEntryDto[];
}

export interface IngestManualMetricsRequestDto {
  AccountId: string;
  /** yyyy-MM-dd */
  StatDate: string;
  Views: number;
  Likes: number;
  Comments: number;
  Shares: number;
  NewFollowers: number;
}

export interface MetricIngestResultDto {
  Success: boolean;
  Message: string;
  Updated: boolean;
  Entry: MetricEntryDto | null;
}

export interface ListRecentMetricsRequestDto {
  AccountId: string | null;
  Take: number;
}

export interface ImportMetricsCsvRequestDto {
  FileName: string;
  /** 完整 CSV 文本（含表头），前端用 FileReader 读出来直接提交。 */
  Content: string;
}

export interface MetricImportErrorDto {
  /** 文件物理行号（1 开始，含表头），用于定位到具体哪一行。 */
  RowNumber: number;
  Message: string;
}

export interface ImportMetricsCsvResultDto {
  Success: boolean;
  Message: string;
  TotalRows: number;
  CreatedRows: number;
  UpdatedRows: number;
  DuplicateRows: number;
  FailedRows: number;
  Errors: MetricImportErrorDto[];
  ErrorsTruncated: boolean;
  Warning: string;
}

/** 读取阈值配置的请求，目前没有筛选条件，保留空对象以符合一方法一入参的约定。 */
export interface GetSignalThresholdsRequestDto {}

/**
 * 机会 / 风险阈值。IsCustomized 为 false 表示库里没有配置、当前走默认值。
 * Policy 是后端按当前阈值生成的判定规则文案，前端直接展示，不自己拼。
 */
export interface SignalThresholdDto {
  BreakoutMultiplier: number;
  BreakoutMinViews: number;
  /** 正数，表示「下降 ≥ N%」。 */
  DeclineThresholdPercent: number;
  LowInteractionRatePercent: number;
  StaleDays: number;
  IsCustomized: boolean;
  /** yyyy-MM-dd HH:mm，默认阈值时为空字符串。 */
  UpdatedAt: string;
  UpdatedBy: string;
  Policy: string;
}

export interface SaveSignalThresholdRequestDto {
  BreakoutMultiplier: number;
  BreakoutMinViews: number;
  DeclineThresholdPercent: number;
  LowInteractionRatePercent: number;
  StaleDays: number;
}

export interface SaveSignalThresholdResultDto {
  Success: boolean;
  Message: string;
  Thresholds: SignalThresholdDto;
}
