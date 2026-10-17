# MediaAI 落地实施方案

> 目标：把当前「前端原型 + 认证骨架」推进到**多用户、可配置、数据真实**的工作台。
> 数据源采用**混合策略**：手工录入 / CSV 导入打底，拿到平台资质后按平台逐个开启官方 API 自动同步。
> 本文档与 `README.md` 的「下一步计划」一致，粒度细化到任务、文件、接口与验收标准，可直接用于排期。

---

## 0. 范围与边界

**做**：飞书免登与组织同步、按用户隔离的数据、用户配置、业务增删改查、审批与告警、首页真实指标、同步任务骨架、平台 API 适配器。

**暂不做**（M4 之后）：移动端原生、复杂 BI 自定义看板、计费、开放 API 对外、全文检索。

---

## 1. 现状与差距

| 能力 | 现状 | 代码位置 |
| --- | --- | --- |
| 登录 | 无登录页，`/` 公开 | `react-app/src/routes/Routes.tsx` |
| 密码校验 | 本地开发不校验密码，邮箱存在即签发 token | `Services/App.Api/Accessors/LocalAuthAccessor.cs` |
| 路由保护 | `ProtectedRoute` 已实现但路由表未使用 | `react-app/src/routes/ProtectedRoute.tsx` |
| 角色 | 后端 3 个 / 前端 7 个，不一致 | `App.Api/Utilities/Roles/Roles.cs`、`views/ExampleView.tsx` |
| 业务接口 | 只有 `AuthManager` 8 个方法 | `App.Api/Managers/AuthManager.cs` |
| 数据隔离 | 无 `OwnerId`，本地数据全机共享 | `LocalDatabaseAccessor`（按类型名存 JSON） |
| 页面数据 | 全部是常量 | `views/ExampleView.tsx:16-107` |
| 用户配置 | 无 | — |
| 真实数据源 | 无 | — |

---

## 2. 总体架构

```text
飞书（企业自建应用）
  ├─ 网页免登 OAuth        → FeishuManager.ExchangeCode → 签发本系统 JWT
  ├─ 通讯录 / 用户组同步    → UserProfile + 角色映射（每小时）
  ├─ 审批实例（报价/发布）  → 事件回调 → 推进 Deal.Stage
  └─ 群机器人消息卡片       ← AlertManager 推送告警

React 页面
  └─ src/api/XxxManager.ts       薄封装（每个后端 Manager 一个文件）
       └─ ApiClient.invokeMethod("Api", "XxxManager", "Method", dto)
            └─ POST /api/invoke   请求体含 AccessToken / RefreshToken
                 └─ ServiceInvoker（不改动）
                      └─ XxxManager（[RequireAuthenticated] / [RequireRole]）
                           ├─ IDatabaseAccessor（Marten / 本地 JSON）
                           ├─ IPlatformProvider（Manual / Csv / Douyin / Xhs ...）
                           └─ IFeishuClient（免登 / 通讯录 / 审批 / 消息）
```

### 关键决策

| 决策 | 选择 | 理由 |
| --- | --- | --- |
| 身份源 | 飞书网页免登（企业自建应用），保留本地 fallback 账号 | 内部同事都在飞书，免维护账号与密码体系；自建应用仅本企业可用，正好匹配 |
| 角色来源 | 飞书用户组 → 7 角色映射（`RoleMappingConfig`，存库可热改） | 管理员在飞书调组即可改权限，不用自建用户管理页 |
| 会话 | 飞书 code 只用于换身份，业务请求仍走本系统 JWT | `ServiceInvoker` 与前端 `AuthStore` 零改动；飞书 token 不下发浏览器 |
| 用户隔离 | 所有业务实体加 `OwnerId`（= `AuthId`，即飞书 `open_id`），查询强制带过滤 | 现有 Marten 是单库单会话，改造量最小；团队维度预留 `WorkspaceId` |
| 文档 ID | 继续用 `Guid`，`OwnerId` 只做字段与索引 | 避免改 `BaseEntity` 与 `IDatabaseAccessor` 签名 |
| 数据源 | `IPlatformProvider` 统一写 `MetricSnapshot` | 手工、CSV、API 三条路产出同一张表，首页无需区分来源 |
| 配置存储 | `UserSettings` 单文档 / 用户 | 配置项少、整体读写，无需拆表 |
| 权限 | 角色 + 数据归属双校验 | 只判 `[RequireRole]` 挡不住越权读别人数据 |
| 本地开发 | 保留 JSON 存储，M2 起切 Postgres | 本地无需装库；多用户并发前必须切库 |

---

## 3. 数据模型

统一放 `Services/Contracts.Domain/Database/`，全部继承 `BaseEntity`（`Guid Id`）。
公共字段（除 `UserSettings` 外）：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `OwnerId` | `string` | 数据归属，取 `AuthId` |
| `WorkspaceId` | `string?` | 团队/工作区，M3 后启用 |
| `CreatedAt` / `UpdatedAt` | `DateTimeOffset` | UTC 存储，前端按时区展示 |

### 3.1 UserProfile（扩展已有 `User`）

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `AuthId` | `string` | 主键语义，取飞书 `open_id`（本地 fallback 用 `local:{email}`） |
| `FeishuUnionId` | `string?` | 跨企业唯一，预留 |
| `Email` | `string` | 唯一，来自通讯录 |
| `Name` | `string` | 显示名 |
| `AvatarUrl` | `string?` | 飞书头像 |
| `DepartmentIds` | `string[]` | 通讯录部门，用于展示 |
| `FeishuGroupIds` | `string[]` | 用户组，**角色由此映射** |
| `Roles` | `string[]` | 7 类业务角色（由 `FeishuGroupIds` 计算后落库） |
| `Status` | `enum` | `Active` / `Invited` / `Disabled`（通讯录离职自动置 `Disabled`） |

### 3.2 UserSettings

| 字段 | 类型 | 默认 | 说明 |
| --- | --- | --- | --- |
| `OwnerId` | `string` | — | 唯一索引 |
| `DefaultTab` | `string` | 按角色推导 | `business` / `plans` / `todo` |
| `VisiblePlatforms` | `string[]` | 全部 | 首页只展示勾选平台 |
| `SlumpThresholdPercent` | `int` | 30 | 掉速告警阈值（窗口内跌幅） |
| `ThrottleDropPercent` | `int` | 40 | 限流判定阈值 |
| `WindowDays` | `int` | 7 | 判定窗口 |
| `TimeZone` | `string` | `Asia/Shanghai` | 日粒度聚合口径 |
| `NotifyChannels` | `string[]` | `[]` | `email` / `webhook` / `none` |
| `Theme` | `string` | `system` | 预留暗色模式 |

### 3.3 PlatformAccount

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `Platform` | `string` | `douyin` / `xiaohongshu` / `wechat_channels` / `weibo` / `bilibili` |
| `AccountName` | `string` | 展示名 |
| `ExternalId` | `string?` | 平台侧账号 ID |
| `AuthMode` | `enum` | `Manual` / `OAuth` |
| `Status` | `enum` | `Connected` / `Expired` / `Error` / `Disabled` |
| `AccessTokenCipher` | `string?` | `IDataProtector` 加密后的密文 |
| `RefreshTokenCipher` | `string?` | 同上 |
| `TokenExpiresAt` | `DateTimeOffset?` | 过期时间，用于「授权即将失效」提醒 |
| `LastSyncedAt` | `DateTimeOffset?` | 最近一次成功同步 |

索引：`(OwnerId, Platform, ExternalId)` 唯一。

### 3.4 MetricSnapshot（核心事实表）

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `OwnerId` | `string` | — |
| `PlatformAccountId` | `Guid` | 关联账号 |
| `StatDate` | `DateOnly` | 按 `TimeZone` 归一的日粒度 |
| `Views` / `Likes` / `Comments` / `Shares` / `Saves` | `long` | 指标 |
| `FollowersNew` | `long` | 新增粉丝 |
| `Source` | `enum` | `Manual` / `Csv` / `PlatformApi` |

唯一约束：`(OwnerId, PlatformAccountId, StatDate)`；重复写入按 upsert 覆盖。

> 内容级指标（单条视频的播放/完播）单独建 `ContentMetric`，字段同上 + `ContentRef`（平台内容 ID）+ `Title`，用于「爆款机会 / 限流风险」判定。

### 3.5 ContentPlan

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `Title` | `string` | 计划名 |
| `Platforms` | `string[]` | 覆盖平台 |
| `PlannedAt` | `DateTimeOffset?` | 排期时间 |
| `Status` | `enum` | `Draft` / `Scoring` / `Scheduled` / `Published` / `Archived` |
| `Direction` | `string?` | 内容方向 |
| `Score` | `decimal?` | 机会分（0-10） |
| `AssigneeAuthId` | `string?` | 责任人 |
| `DealId` | `Guid?` | 关联商务单 |
| `Link` | `string?` | 成品链接 |

### 3.6 Deal（商务合作）

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `ClientName` | `string` | 客户名 |
| `Contact` | `string?` | 联系人 |
| `Amount` | `decimal` | 金额 |
| `Currency` | `string` | 默认 `CNY` |
| `Stage` | `enum` | `Lead` / `Quoting` / `PricingReview` / `Confirmed` / `Delivering` / `Settled` / `Lost` |
| `OwnerAuthId` | `string` | 商务负责人 |
| `ExpectedCloseAt` | `DateTimeOffset?` | 预计签约 |
| `Notes` | `string?` | — |

### 3.7 Alert

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `Type` | `enum` | `Throttle` / `Slump` / `SyncFailure` / `TokenExpiring` |
| `PlatformAccountId` | `Guid?` | — |
| `ContentRef` | `string?` | — |
| `Severity` | `enum` | `High` / `Medium` / `Low` |
| `Message` | `string` | 展示文案 |
| `MetricValue` / `Threshold` | `double?` | 触发时数值 |
| `DetectedAt` | `DateTimeOffset` | — |
| `Status` | `enum` | `Open` / `Acked` / `Closed` |

### 3.8 SyncJob

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `PlatformAccountId` | `Guid` | — |
| `Status` | `enum` | `Queued` / `Running` / `Succeeded` / `Failed` |
| `StartedAt` / `FinishedAt` | `DateTimeOffset?` | — |
| `RetryCount` | `int` | 上限 3 |
| `Error` | `string?` | 脱敏后的错误摘要 |

### 3.9 AuditLog

`OwnerId` / `ActorAuthId` / `EntityName` / `EntityId` / `Action`（`Create|Update|Delete`）/ `At` / `ChangedFields`（JSON，不含敏感值）。

### 3.10 ApprovalLink（飞书审批 ↔ 业务单）

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `OwnerId` | `string` | 发起人 |
| `EntityName` | `string` | `Deal` / `ContentPlan` |
| `EntityId` | `Guid` | 业务单 ID |
| `ApprovalCode` | `string` | 飞书审批定义 code（报价审核 / 发布确认） |
| `InstanceCode` | `string` | 飞书审批实例，唯一索引（回调幂等键） |
| `Status` | `enum` | `Pending` / `Approved` / `Rejected` / `Canceled` |
| `UpdatedAt` | `DateTimeOffset` | 回调到达时间 |

### 3.11 RoleMappingConfig（单文档）

`GroupRoleMap: Dictionary<string, string>`（飞书用户组 ID → 角色），`AdminGroupIds: string[]`，`SyncedAt`。
放在数据库而非 `appsettings`，便于管理员在 `/settings` 页面调整而无需重启；密钥类配置仍只在环境变量。

---

## 4. 接口清单

约定（沿用现有实现，不改 `ServiceInvoker`）：

- Manager 类实现 `IManagerService`，在 `Program.cs` 注册为 Scoped；
- 方法参数为单个 DTO（放在 `Services/Contracts.Dto`，命名 `XxxRequestDto`），返回 DTO；
- 鉴权用 `[RequireAuthenticated]`，管理类方法用 `[RequireRole(Roles.Admin)]`；
- **每个方法必须从 `UserContextService.UserId` 取 `OwnerId` 作为查询条件**，禁止信任入参里的 owner 字段。

### 4.1 SettingsManager

| 方法 | 请求 | 返回 | 权限 | 用途 |
| --- | --- | --- | --- | --- |
| `GetSettings` | `GetSettingsRequestDto`（空） | `UserSettingsDto` | 登录 | 首次登录自动建默认值 |
| `SaveSettings` | `UserSettingsDto` | `UserSettingsDto` | 登录 | 部分更新 |
| `ResetSettings` | `ResetSettingsRequestDto` | `UserSettingsDto` | 登录 | 恢复默认 |

### 4.2 PlatformAccountManager

| 方法 | 请求 | 返回 | 用途 |
| --- | --- | --- | --- |
| `ListAccounts` | `ListAccountsRequestDto` | `PlatformAccountDto[]` | 连接页列表 |
| `SaveAccount` | `SaveAccountRequestDto` | `PlatformAccountDto` | 新增/编辑（手工模式） |
| `DisableAccount` | `AccountIdRequestDto` | `OperationResultDto` | 停用 |
| `GetConnectUrl` | `GetConnectUrlRequestDto` | `ConnectUrlDto` | OAuth 授权链接（M4） |
| `CompleteConnect` | `CompleteConnectRequestDto` | `PlatformAccountDto` | 回调换 token（M4） |

返回的 DTO **不得包含 token**，只返回 `Status` 与 `TokenExpiresAt`。

### 4.3 MetricManager

| 方法 | 请求 | 返回 | 用途 |
| --- | --- | --- | --- |
| `IngestManual` | `IngestManualMetricsRequestDto`（账号 + 日期 + 指标） | `OperationResultDto` | 单日手工录入 |
| `ImportCsv` | `ImportCsvRequestDto`（文件名 + base64 或行数组） | `ImportResultDto`（成功/失败行号 + 原因） | 批量导入 |
| `QuerySeries` | `QuerySeriesRequestDto`（账号、区间、粒度） | `MetricSeriesDto` | 图表数据 |
| `QueryContentMetrics` | `QueryContentMetricsRequestDto` | `ContentMetricDto[]` | 机会/风险判定 |

CSV 模板列：`platform,account,date,views,likes,comments,shares,saves,followers_new`。

### 4.4 ContentPlanManager

`ListPlans` / `GetPlan` / `SavePlan` / `UpdatePlanStatus` / `DeletePlan`，DTO：`ContentPlanDto`、`PlanListRequestDto`（分页 + 状态 + 平台 + 关键词）。

### 4.5 DealManager

`ListDeals` / `GetDeal` / `SaveDeal` / `UpdateDealStage` / `DeleteDeal`，DTO：`DealDto`、`DealListRequestDto`。

状态流转校验：只允许 `Lead → Quoting → PricingReview → Confirmed → Delivering → Settled`，`Lost` 可从任意阶段进入。

### 4.6 AlertManager

| 方法 | 说明 |
| --- | --- |
| `ListAlerts` | 分页 + 状态 + 类型 |
| `AcknowledgeAlert` | 置 `Acked` |
| `CloseAlert` | 置 `Closed` |
| `EvaluateRules` | 内部/定时调用：按 `UserSettings` 阈值扫描 `MetricSnapshot` 生成告警 |

### 4.7 DashboardManager（首页唯一入口）

```text
GetOverview(GetOverviewRequestDto { From, To, Platform? })
  -> DashboardOverviewDto
       Kpis: MetricCardDto[4]           // 最强平台 / 风险平台 / 爆款内容 / 同步异常
       Platforms: PlatformSeriesDto[3]  // 平台名 + tone + 5 点 TrendPoint
       Breakouts: ContentHighlightDto[] // 标题 / 平台 / 信号 / 数值 / href
       Risks: ContentHighlightDto[]     // 标题 / 平台 / 信号 / 原因 / href
       Tabs: WorkspaceTabDto[]          // 当前用户的可见 Tab
       Rows: 按 tab 返回 DealRowDto[] | PlanRowDto[] | TodoDto[]
       Management: ManagementOverviewDto[] // 仅 admin
```

字段与 `ExampleView.tsx:24-107` 的常量结构一一对应，前端替换数据来源即可，不改布局。

### 4.8 AdminManager（管理层）

`ListUsers` / `InviteUser` / `AssignRoles` / `DisableUser`，全部 `[RequireRole(Roles.Admin)]`。

### 4.9 SyncManager（M4）

`TriggerSync` / `GetSyncStatus` / `RetrySync`；实际执行在 `BackgroundService`，按 `SyncJob` 串行处理。

### 4.10 FeishuManager

| 方法 | 请求 | 返回 | 权限 | 说明 |
| --- | --- | --- | --- | --- |
| `GetLoginUrl` | `GetFeishuLoginUrlRequestDto`（`RedirectUri`） | `LoginUrlDto` | 匿名 | 生成带 `state` 的授权地址，`state` 存 5 分钟 |
| `ExchangeCode` | `ExchangeCodeRequestDto`（`Code`、`State`） | `LoginResponseDto` | 匿名 | code → `open_id` → upsert `UserProfile` → **签发本系统 JWT** |
| `SyncDirectory` | `SyncDirectoryRequestDto` | `SyncDirectoryResultDto` | admin / 定时 | 拉部门、用户、用户组，更新角色，离职置 `Disabled` |
| `GetRoleMapping` | 空 | `RoleMappingDto` | admin | 用户组 ↔ 角色 |
| `SaveRoleMapping` | `RoleMappingDto` | `RoleMappingDto` | admin | 保存后下次登录/同步生效 |
| `CreateApproval` | `CreateApprovalRequestDto`（`EntityName`、`EntityId`、`ApprovalCode`） | `ApprovalLinkDto` | 登录 | 为商务单/计划发起审批 |
| `SendAlertCard` | `SendAlertCardRequestDto` | `OperationResultDto` | 内部调用 | 推送告警卡片到运营群 |

**唯一不走 `ServiceInvoker` 的入口**：飞书事件订阅回调 `POST /api/feishu/events`（飞书不会携带本系统 AccessToken）。该端点需自行完成 `VerificationToken` 校验、`EncryptKey` 解密、`instance_code` 幂等，并快速返回 200（业务处理异步化），否则飞书会重推。

免登时序：

```text
前端 /login → GetLoginUrl → 跳 accounts.feishu.cn 授权
  → 回跳 /auth/feishu/callback?code=&state=
  → ExchangeCode（匿名 invoke）
  → 返回本系统 AccessToken/RefreshToken + Session
  → AuthStore.setAuth → 跳 /（受保护路由）
```

飞书 `user_access_token` 只在后端内存中使用，换完身份即丢弃，不落库、不下发。

---

## 5. 前端改造清单

| 文件/新增 | 内容 |
| --- | --- |
| `src/api/SettingsManager.ts` 等 9 个 | 与后端 Manager 同名薄封装，写法照 `api/AuthManager.ts`；含 `FeishuManager.ts` |
| `src/api/AppDtos.ts` | 补全部 DTO interface（与后端字段名一致，PascalCase） |
| `src/hooks/useDashboard.ts` 等 | 统一 `{ data, loading, error, refetch }`，AbortController 取消 |
| `src/views/LoginView.tsx` | 「飞书登录」按钮；本地 fallback 账号入口（仅管理员）；不自建注册 |
| `src/views/FeishuCallbackView.tsx` | 接收 `code` / `state`，调 `ExchangeCode`，失败回登录并提示 |
| `src/views/SettingsView.tsx` | 平台账号、阈值、默认 Tab、通知 |
| `src/views/BusinessView.tsx` / `DealDetailView.tsx` | 列表 + 筛选 + 表单 + 详情 |
| `src/views/PlansView.tsx` / `PlanDetailView.tsx` | 同上 |
| `src/views/ConnectionsView.tsx` | 账号卡片、授权状态、同步按钮、CSV 导入 |
| `src/components/feedback/` | `LoadingSkeleton` / `EmptyState` / `ErrorState` 三态组件 |
| `src/utils/roles.ts` | 角色常量 + 中文名 + 默认 Tab（替换 `ExampleView` 内映射） |
| `src/routes/Routes.tsx` | 业务路由包 `ProtectedRoute`，补 `/login`、`/settings` |
| `src/views/ExampleView.tsx` | 删除全部常量，改为 `useDashboard()`；保留布局与设计系统 |

约定：所有金额用 `formatCompactNumber` + 币种；日期按 `settings.TimeZone` 渲染；不新增硬编码主题色。

---

## 6. 任务拆解与排期

> 人日为单人对齐后的估算，含自测；不含平台资质等待时间。

### M0 身份与配置底座（7.5 人日）

| ID | 任务 | 产出 | 验收 | 人日 |
| --- | --- | --- | --- | --- |
| M0-1 | 飞书应用与免登 | 开放平台自建应用（网页应用）、`LoginView` 改为授权跳转、`FeishuCallbackView` | 从 `/login` 到进看板 ≤ 3 秒，无需输密码 | 1.5 |
| M0-2 | 路由保护 | `Routes.tsx` 改造 | 未登录访问 `/` 跳 `/login`，登录后回跳 | 0.5 |
| M0-3 | 本系统会话 | `FeishuManager.ExchangeCode` + 复用 JWT 签发；本地 fallback 账号 | 飞书 token 不出后端；业务请求仍走原 `AccessToken` | 1 |
| M0-4 | 角色映射 | `RoleMappingConfig` + 飞书用户组 → 7 角色；`utils/roles.ts` | 飞书调整用户组后重新登录权限即变；越权 403 | 1.5 |
| M0-5 | 用户配置 | `UserSettings` + `SettingsManager` + `/settings` | 改阈值后首页判定立即生效 | 2 |
| M0-6 | 通讯录同步 | `BackgroundService` 每小时同步部门/用户组，离职置 `Disabled` | 新同事当天可登录；离职账号次日拒绝 | 1 |

### M1 数据底座（9 人日）

| ID | 任务 | 产出 | 验收 | 人日 |
| --- | --- | --- | --- | --- |
| M1-1 | 归属约定 | 实体公共字段、索引、查询封装 | 任何列表接口不传 owner 也只看自己数据 | 1 |
| M1-2 | 核心实体 | `PlatformAccount` / `MetricSnapshot` / `ContentMetric` | 增删改查通过 | 2 |
| M1-3 | 指标写入 | `MetricManager.IngestManual` + upsert | 重复日期不产生重复行 | 2 |
| M1-4 | CSV 导入 | `ImportCsv` + 前端上传 | 1000 行导入 < 3s，错误行返回行号 | 2 |
| M1-5 | 三态组件 | `LoadingSkeleton` / `EmptyState` / `ErrorState` | 三个页面接入，断网有重试 | 1.5 |
| M1-6 | 切 Postgres | Supabase 实例 + 连接配置 | 两个浏览器并发写不丢数据 | 0.5 |

### M2 业务闭环（11.5 人日）

| ID | 任务 | 产出 | 验收 | 人日 |
| --- | --- | --- | --- | --- |
| M2-1 | 商务 | `DealManager` + 列表/详情/表单；状态由飞书审批回调推进 | 报价走飞书审批，回调回写阶段正确；列表可筛选 | 3 |
| M2-2 | 内容计划 | `ContentPlanManager` + 列表/详情 | 排期、责任人、关联商务单可用 | 3 |
| M2-3 | 平台连接 | `PlatformAccountManager` + 连接页 | 手工账号可增删改停；过期账号有提醒 | 2.5 |
| M2-4 | 告警 | `AlertManager` + 规则扫描 + 飞书群卡片推送与按钮回调 | 阈值命中 1 分钟内收到卡片，可直接确认 | 1.5 |
| M2-5 | 审计 | `AuditLog` 写入 | 每笔写操作有记录（含审批来源），可查 | 1.5 |

### M3 首页真实化（8 人日）

| ID | 任务 | 产出 | 验收 | 人日 |
| --- | --- | --- | --- | --- |
| M3-1 | 聚合接口 | `DashboardManager.GetOverview` | 与手工录入数据一致 | 3 |
| M3-2 | 前端替换 | 删 mock、接 hooks、三态 | 刷新后数字不变；空账号显示空态 | 3 |
| M3-3 | 管理层面板 | 按 `admin` 角色返回 | 非 admin 不返回该字段 | 1 |
| M3-4 | 端到端 | 登录→录入→看板→详情 | 全链路无 mock 残留 | 1 |

**M0-M3 合计约 36 人日**（7.5 + 9 + 11.5 + 8）。

> 与不接飞书的 35 人日方案基本持平（+1）：工时没有净节省，换来的是**免登、组织同步、审批流、告警推送**四项能力，且不再需要自研用户管理与密码体系。若只算「登录 + 用户管理」这一块确实省了，但新增的 OAuth 联调、角色映射、通讯录同步、审批回调把差额吃回去了。

1 人约 7 周，2 人并行约 4 周。

### M4 平台 API 增强（按平台推进）

| ID | 任务 | 人日 |
| --- | --- | --- |
| M4-1 | `IPlatformProvider` 抽象 + `SyncJob` + `BackgroundService` | 3 |
| M4-2 | 抖音开放平台 OAuth + 指标拉取 | 5 |
| M4-3 | 小红书（或公众号）授权 + 拉取 | 5 |
| M4-4 | token 刷新、限流退避、失败重试与告警 | 2 |

> 每个平台单独排期，资质审批周期不计入。

---

## 7. 角色权限矩阵

| 能力 | admin | sales | finance | planner | operations | editor | analyst |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 看板查看 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| 管理层面板 | ✓ | — | — | — | — | — | — |
| 商务单编辑 | ✓ | ✓ | 报价复核 | — | — | — | — |
| 内容计划编辑 | ✓ | — | — | ✓ | ✓ | ✓ | — |
| 平台连接管理 | ✓ | — | — | — | ✓ | — | — |
| 用户与角色 | ✓ | — | — | — | — | — | — |
| 数据导出 | ✓ | ✓ | ✓ | ✓ | — | — | ✓ |

后端逐方法打 `[RequireRole]`，并在 Manager 内校验数据归属。

角色来源：飞书用户组（`RoleMappingConfig.GroupRoleMap`），新同事入组即得权限、离组即失权，无需人工开账号。`admin` 仅限运维组；数据归属仍按 `OwnerId` 二次校验。前端不维护角色名单，只消费 `Session.Roles`。

---

## 8. 用户可配置项（`/settings`）

1. 默认落地 Tab、可见 Tab
2. 首页展示哪些平台
3. 掉速 / 限流阈值与判定窗口
4. 通知渠道（邮件、Webhook）
5. 时区与周起始日
6. 绑定的平台账号（手工新增 / OAuth 授权 / 停用）
7. 主题（暗色预留）

飞书相关为**系统级配置**，不进用户配置：

| 配置键 | 位置 | 说明 |
| --- | --- | --- |
| `Feishu:AppId` / `Feishu:AppSecret` | 环境变量 | `AppSecret` 不入库、不回显 |
| `Feishu:RedirectUri` | 环境变量 | 需与开放平台后台登记一致，否则授权失败 |
| `Feishu:VerificationToken` / `EncryptKey` | 环境变量 | 事件回调签名与解密 |
| `Feishu:ApprovalCodes` | 环境变量 | 报价审核 / 发布确认的 `approval_code` |
| `Feishu:AlertWebhook` | 环境变量 | 运营群机器人地址 |
| 用户组 → 角色映射 | 数据库 `RoleMappingConfig` | 页面可改，热生效 |

其余系统配置仍走 `AppConfiguration__*` 环境变量。

---

## 9. 安全与运维清单

- [ ] 所有列表/详情接口按 `OwnerId` 过滤，写操作校验归属
- [ ] 平台 token 用 `IDataProtector` 加密入库，任何 DTO 都不返回
- [ ] 飞书回调校验 `VerificationToken` / 解密 `EncryptKey`，并以 `instance_code` 做幂等
- [ ] 免登 `state` 一次性、5 分钟过期，回调时校验来源
- [ ] 飞书 `open_id`、手机号、邮箱不写日志；`ExchangeCode` 限速（同 IP 20 次/分钟）
- [ ] 本地 fallback 账号仅管理员可用，失败限速（同 IP 5 次/分钟）
- [ ] JWT 有效期与刷新策略确认；前端不打印 token
- [ ] 生产启用 Supabase RLS 作为第二道防线
- [ ] 审计日志保留 90 天，敏感字段脱敏
- [ ] 定时任务幂等：同账号同日重复同步只 upsert
- [ ] 备份与恢复演练（Postgres 每日快照）
- [ ] 错误提示不暴露内部异常与密钥

---

## 10. 验收清单

- [ ] 新用户注册 → 登录 → 看到空看板（不是假数据），有引导
- [ ] 用户 A 无法通过改 URL/参数读取用户 B 的数据（403）
- [ ] 手工录入 3 天数据后，首页指标与图表同步变化
- [ ] CSV 导入 1000 行成功，错误行有行号与原因
- [ ] 阈值改为 20% 后，原 25% 跌幅内容出现在风险列表
- [ ] 商务单状态非法流转被拒并有提示
- [ ] 断网 / 500 / 空数据三种状态都有明确界面
- [ ] 刷新页面数字不变；切换 Tab 数据各自正确
- [ ] 非 admin 看不到管理层面板，接口也不返回该字段
- [ ] 从飞书工作台 / 浏览器打开均可免登进入，退出后再进无需重输密码
- [ ] 未加入任何业务用户组的同事登录后只能看，不能改
- [ ] 商务单走完飞书审批后，列表阶段自动更新，无需人工刷新两次
- [ ] `npm run build` 与 `dotnet test` 全绿

---

## 11. 风险与待确认

| 风险 | 影响 | 应对 |
| --- | --- | --- |
| 平台开放平台资质审批周期长 | M4 无法按期 | 以手工/CSV 为主路径，API 作为增量 |
| 平台 API 限流、字段口径不一 | 数据缺口 | Provider 内做字段映射与缺失标记，不阻断首页 |
| 第三方数据服务合规风险 | 法律风险 | 优先官方 API，第三方需书面授权 |
| 本地 JSON 并发写丢数据 | 开发期偶发 | M1-6 切 Postgres，开发也建议直连 |
| 时区导致日粒度错位 | 指标对不上 | 统一 `StatDate` 按用户时区归一 |
| 角色口径继续漂移 | 越权 | 角色常量单一来源（后端 Roles.cs + 前端 roles.ts 由同一清单生成） |
| 飞书不可用（故障/网络） | 无法登录 | JWT 有效期覆盖一个工作日；保留本地 admin fallback；失败页给出明确提示而非白屏 |
| 业务方改动审批定义 | 回调字段解析失败 | `approval_code` 与字段名配置化；解析失败只记日志、不回写状态，业务单保持原阶段 |
| 飞书应用权限被回收 | 通讯录/审批同步中断 | 同步任务连续失败 3 次推告警卡片；页面顶部提示「组织数据已过期 X 小时」 |
