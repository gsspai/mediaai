# MediaAI — 个人自媒体矩阵运营工作台

MediaAI 面向个人自媒体矩阵的日常运营：把多个平台账号的播放与互动、爆款机会、限流风险，连同商务合作与内容排期收敛到同一块屏幕——先看态势，再判断机会与风险，最后落到「谁、在哪一步、做什么」。

定位、角色、指标口径与界面结构的完整说明见[产品文档](https://gsspai.github.io/mediaai/)。

当前仓库处于**前端原型阶段**：工作台页面、角色视图与设计系统已就绪，页面数据仍为前端 mock，后端仅保留认证骨架。

---

## 目录

- [文档](#文档)
- [当前状态](#当前状态)
- [技术栈](#技术栈)
- [目录结构](#目录结构)
- [快速开始](#快速开始)
- [环境变量与配置](#环境变量与配置)
- [架构说明](#架构说明)
- [角色与权限](#角色与权限)
- [前端路由](#前端路由)
- [设计系统与 UI 规范](#设计系统与-ui-规范)
- [安全与保密信息处理](#安全与保密信息处理)
- [部署](#部署)
- [下一步计划](#下一步计划)

---

## 文档

| 文档 | 内容 | 位置 |
| --- | --- | --- |
| 产品文档 | 定位、角色、信息架构、界面结构、关键流程、指标口径、权限、分期、验收 | https://gsspai.github.io/mediaai/ |
| 落地实施方案 | 数据模型、Manager 接口清单、前端改造、任务拆解与工时 | [docs/implementation-plan.md](docs/implementation-plan.md) |
| 认证流程 | `ServiceInvoker` 的 JWT 校验与角色拦截 | [docs/service-invoker-auth-flow.md](docs/service-invoker-auth-flow.md) |
| 流式 RPC | 流式调用的传输与生命周期 | [docs/service-invoker-streaming.md](docs/service-invoker-streaming.md) |

产品文档是单文件静态页（`docs/product-overview.html`，入口页 `docs/index.html`），无构建与外部依赖，由 GitHub Pages 从 `main` 分支的 `/docs` 目录发布。

已启用：仓库 **Settings → Pages → Source `Deploy from a branch` → Branch `main`、目录 `/docs`**，访问 https://gsspai.github.io/mediaai/ 即可。推送到 `main` 后约 1 分钟自动发布，无需构建。

---

## 当前状态

| 能力 | 状态 | 说明 |
| --- | --- | --- |
| 工作台首页（`/`） | 已完成（指标卡 + 机会/风险接接口） | 四个 KPI、平台走势、爆款机会、限流风险均由 `DashboardManager.GetSummary` 现场计算；执行列表（商务 / 计划 / 待办）仍是前端数据 |
| 角色化视图 | 已完成（前端） | 按角色决定默认执行列表，`admin` 额外展示管理层概览 |
| 接口权限 | 已接入首批 | 阈值仅 `admin`，账号与指标写接口限 `admin` / `operations`；越权返回 403 + 中文原因，见[角色与权限](#角色与权限) |
| 统一设计系统 | 已完成 | `PageShell` / `Panel` / `SectionHeader` / `StatusPill` / `TrendChart` / `SegmentedTabs` |
| 产品文档 | 已发布 | [gsspai.github.io/mediaai](https://gsspai.github.io/mediaai/)，含定位、角色、指标口径与验收标准 |
| 平台连接页（`/connections`） | 已完成（手工 + CSV） | 账号增删改查、停用、手工录入日指标、CSV 批量导入、最近录入明细；`/connections/:platform` 仍是占位，OAuth 授权未接 |
| 详情页 | 部分 | `/business`、`/plans/:planId`、`/analysis/:viewId` 指向占位视图；`/settings` 已实现阈值配置 |
| 认证接口 | 骨架可用 | `AuthManager` 提供登录、注册、改密、会话等 8 个方法；飞书免登未接入 |
| 看板接口 | 部分可用 | `DashboardManager.GetSummary`（近 N 天 KPI、平台走势、机会 / 风险信号、数据来源）；`PlatformAccountManager`、`MetricManager` 提供账号、手工录入与 CSV 导入；商务 / 计划 / 分析尚无 Manager |
| 数据模型 | 已建立 | `PlatformAccount`（平台账号）、`MetricDaily`（日指标快照，含 `Source` 与 `RecordedBy`） |
| 登录 | 可用 | `/login` 账号密码登录，成功后回到来源路径；飞书免登未接入 |
| 暗色模式 | 部分 | Token 已就绪，但页面尚无主题切换入口 |

> 首页 KPI 卡、平台走势、爆款机会与限流风险均由接口返回，每张卡与每条信号都带来源说明（接口同步 / 手工录入 / CSV 导入 / 开发态种子）。「执行列表」三个 Tab（商务、计划、待办）与「管理层概览」仍是 `react-app/src/views/ExampleView.tsx` 中的演示数据，接口不可用时 KPI 与信号区块会显式降级为「静态示例数据」。

---

## 技术栈

- **前端**：React 18 + TypeScript + Vite + Tailwind CSS v4 + ShadCN UI（`radix-ui`）
- **状态**：Zustand
- **后端**：.NET 8 Web API，统一 RPC 入口（ServiceInvoker）
- **身份**：飞书企业自建应用免登（OAuth 授权码 → `open_id` → 本系统 JWT），本地开发用 `LocalAuthAccessor` 兜底
- **数据库托管**：Supabase（Postgres）
- **文档数据库**：MartenDB（PostgreSQL，非 `public` 的 `app` schema；本地开发写入 JSON 文件）

---

## 目录结构

```text
mediaai/
├── react-app/                     # 前端
│   ├── src/
│   │   ├── api/                   # ApiClient + DTO + AuthManager 封装
│   │   ├── auth/                  # AuthStore（token / session 持久化）
│   │   ├── components/
│   │   │   ├── dashboard/         # 统一展示组件（面板、标题、状态、趋势图、分段切换）
│   │   │   └── ui/                # ShadCN 基础组件
│   │   ├── lib/                   # cn()、数字格式化
│   │   ├── routes/                # 路由表与 ProtectedRoute
│   │   ├── views/                 # 页面视图
│   │   └── default.css            # 设计 token（颜色 / 圆角 / 阴影）
│   └── .env.example               # 环境变量模板（不含真实值）
├── Services/                      # 后端
│   ├── App.Api/                   # 启动、Manager、Engine、Accessor
│   ├── App.ServiceInvoker/        # RPC 路由、反射调用、鉴权
│   ├── Contracts.Dto/             # 请求 / 响应 DTO
│   └── Contracts.Domain/          # 领域实体与契约
├── docs/                          # 产品文档、实施方案与 RPC 说明
└── .gitea/workflows/deploy.yml    # 部署流水线
```

---

## 快速开始

### 前端

```bash
cd react-app
npm ci
cp .env.example .env        # 填入本地后端地址
npm run dev                 # http://localhost:3000
```

常用脚本：

| 命令 | 用途 |
| --- | --- |
| `npm run dev` | 开发服务器 |
| `npm run build` | 类型检查 + 生产构建到 `dist/` |
| `npm run check-ts` | 仅类型检查 |
| `npm run lint` | ESLint（`eslint.config.js` 依赖 `typescript-eslint`，如未安装先执行 `npm i -D typescript-eslint`） |
| `npm run format` | Prettier |

### 后端

```bash
dotnet restore Services/Services.sln
dotnet run --project Services/App.Api      # https://localhost:5123
```

- `ASPNETCORE_ENVIRONMENT=Development` 时使用 `LocalDatabaseAccessor` + `LocalAuthAccessor`，账号数据写入本地 JSON，不需要 Supabase。
- 首次启动会自动生成开发用 JWT 签名密钥 `local-signing.key`（位于 `~/CodeBuddy/Resources/<AppName>/`，仅当前用户可读写）；想固定密钥时通过 `AppConfiguration__LocalJwtSigningKey` 注入，无需把密钥写进任何配置文件。
- 健康检查：`GET /api/health`。

### 测试

```bash
dotnet test Services/Services.sln
cd react-app && npm test
```

---

## 环境变量与配置

仓库内所有配置项只保留占位值。真实密钥由部署环境（环境变量 / 配置管线）注入，**任何情况下都不要提交到 Git**。

前端（`react-app/.env`，不入库）：

| 变量 | 用途 | 示例（占位） |
| --- | --- | --- |
| `VITE_API_URL` | 后端根地址，仅填 origin；`ApiClient` 会拼接 `/Api/invoke` | `https://<your-api-host>` |
| `VITE_APP_NAME` | Web App Manifest 名称 | `MediaAI` |
| `VITE_APP_DESCRIPTION` | Web App Manifest 描述 | `个人自媒体矩阵运营工作台` |

后端（`AppConfiguration` 节，环境变量前缀 `AppConfiguration__`）：

| 键 | 用途 | 示例（占位） |
| --- | --- | --- |
| `SupabaseId` | Supabase 项目 ref，用于推导 Auth / JWKS 地址 | `<supabase-project-ref>` |
| `SupabaseHost` | Postgres 主机 | `<db-host>` |
| `SupabasePort` | Postgres 端口 | `<db-port>` |
| `SupabaseDatabasePassword` | Postgres 密码 | `<masked>` |
| `SupabasePublishableKey` | 匿名 key，仅用于客户端请求头 | `<publishable-key>` |
| `SupabaseSecretKey` | 服务端管理 key（可绕过 RLS） | `<service-role-key>` |
| `AppName` | 本地数据目录名 | `MediaAI` |
| `LocalJwtSigningKey` | 仅开发环境：本地 JWT 签名密钥（至少 32 字符）。留空则自动生成并持久化 | `<generated-on-first-run>` |
| `AdminEmails` | 仅开发环境：初始管理员邮箱，逗号 / 分号分隔。命中名单的账号在登录或注册时自动补 `admin` 角色并落库。生产角色来自飞书用户组，此项留空 | `admin@example.com` |

`Services/App.Api/appsettings.json` 中只有 `default` 等占位串，可直接公开。

飞书自建应用（环境变量前缀 `Feishu__`，**全部为服务端配置，不下发前端**）：

| 键 | 用途 | 示例（占位） |
| --- | --- | --- |
| `Feishu__AppId` / `Feishu__AppSecret` | 自建应用凭据，`AppSecret` 不入库、不回显 | `<app-id>` / `<masked>` |
| `Feishu__RedirectUri` | 免登回调地址，须与开放平台后台登记完全一致 | `https://<your-host>/auth/feishu/callback` |
| `Feishu__VerificationToken` / `Feishu__EncryptKey` | 事件回调验签与解密 | `<masked>` |
| `Feishu__ApprovalCodes` | 报价审核 / 发布确认的 `approval_code` | `{"quote":"<code>","publish":"<code>"}` |
| `Feishu__AlertWebhook` | 运营群机器人地址 | `https://open.feishu.cn/open-apis/bot/v2/hook/<masked>` |

用户组 → 角色映射存数据库（`RoleMappingConfig`），在 `/settings` 页维护、热生效，不走环境变量。

---

## 架构说明

### 单一 RPC 入口

前端不按 REST 资源调用后端，而是通过 ServiceInvoker 调用 Manager 方法：

```text
POST /api/invoke   → ServiceInvokerEndpoints → ServiceMethodInvoker → Manager 方法
POST /api/stream   → NDJSON 流式返回（方法返回 IAsyncEnumerable<T>）
```

新增业务能力的方式：在 `Contracts.Dto` 定义 DTO → 在 `App.Api/Managers` 新增 Manager 方法与 `[RequireAuthenticated]` / `[RequireRole]` 标记 → 在 `react-app/src/api` 增加薄封装。**不要修改 ServiceInvoker**。

### 认证与会话

- 前端在请求体内携带 `AccessToken` / `RefreshToken`，响应 envelope 返回刷新后的 token 与 `SessionDto`。
- `AuthManager` 同时实现 `IAuthTokenManager`，由 ServiceInvoker 统一协调「先刷新、再鉴权、再调用」。
- 身份来源是飞书：前端用授权码换 `open_id`，后端据此 upsert 用户并**签发本系统 JWT**；飞书 `user_access_token` 用完即弃，不落库、不下发浏览器。
- 角色由飞书用户组映射得出（`RoleMappingConfig`，存库可热改），写入 `Session.Roles` 后由 `[RequireRole(...)]` 校验；数据归属另按 `OwnerId` 二次校验，两者都过才放行。
- 本地开发用 `LocalAuthAccessor` 签发与校验，保留管理员兜底账号（仅 admin 可用）；生产环境校验本系统签发的 JWT。

详见 [docs/service-invoker-auth-flow.md](docs/service-invoker-auth-flow.md) 与 [docs/service-invoker-streaming.md](docs/service-invoker-streaming.md)。

### 看板指标与数据模型

`DashboardManager.GetSummary({ Days })` 返回首页四个 KPI、平台走势与数据来源说明，`Days` 会被钳制在 3–90 天。

| 文档 | 作用 | 关键字段 |
| --- | --- | --- |
| `PlatformAccount` | 平台账号 | `Platform`、`Handle`、`Status`（`connected` / `manual` / `paused` / `error`）、`FollowerCount`、`OwnerId` |
| `MetricDaily` | 每个账号每天的指标快照 | `AccountId`、`Platform`、`StatDate`、`Views` / `Likes` / `Comments` / `Shares`、`NewFollowers`、`Source`、`RecordedBy`、`RecordedAt` |

- `MetricDaily.Source` 取 `api`（平台接口同步）/ `manual`（人工录入）/ `csv`（CSV 批量导入）/ `seed`（开发态种子），返回体里的 `DataSource` 会把四者计数摊开，界面直接显示「开发态示例数据 · 5 个账号 / 35 条日记录」「CSV 导入 12 条」这类文案。**新增数据入口时必须写入 `Source` 与 `RecordedBy`，否则数字无法回溯。**
- 环比口径：把一个区间对半切成前段与后段，用**日均播放**比较（`DailyChangeRate`），而不是两段总量——7 天区间前 3 天后 4 天，用总量比会系统性低估后段。
- 无数据时不报错：`HasData = false`，四个 KPI 返回 `"—"` 与 `Status = pending`，界面同时展示「静态示例数据」标记，避免把占位值当成真实数据。

开发态种子数据只在 `ASPNETCORE_ENVIRONMENT=Development` 且库中**没有任何** `MetricDaily` 时写入一次（`Utilities/DashboardSeeder.cs`）。想回到初始状态，删掉本地数据目录下的 `PlatformAccount.json` 与 `MetricDaily.json` 后重启后端。

### 账号与录入接口

| 方法 | 作用 | 说明 |
| --- | --- | --- |
| `PlatformAccountManager.ListAccounts` | 账号列表 | 默认过滤 `paused`；带最近记录日期与近 7 天条数 |
| `PlatformAccountManager.SaveAccount` | 新增 / 编辑 | 同一平台下 `Handle` 唯一；已有日记录的账号不允许改平台 |
| `PlatformAccountManager.DisableAccount` | 停用 | 只改状态不删数据，历史记录仍参与回溯 |
| `MetricManager.IngestManual` | 手工录入单日指标 | 同账号同日按覆盖；写 `Source = manual` 与当前登录邮箱 |
| `MetricManager.ImportCsv` | CSV 批量导入 | 逐行校验，同账号同日按覆盖；写 `Source = csv` 与当前登录邮箱 |
| `MetricManager.ListRecentMetrics` | 最近录入明细 | 按统计日期倒序，最多 100 条 |

录入校验（都返回 `Success = false` + 中文原因，不抛异常）：

- 日期必须是 `yyyy-MM-dd`，不能是未来，最早回溯两年；
- 播放 / 点赞 / 评论 / 转发不能为负，且互动数之和不能超过播放数（`Views = 0` 时跳过，兼容纯掉粉记录）；
- 账号已停用时拒绝录入；
- 数据归属：`OwnerId` 为空表示共享账号（开发态种子账号），非空只对本人可见。这是开发期的过渡规则，飞书接入后收紧。

### CSV 批量导入

入口在平台连接页工具栏的「批量导入 CSV」，同一个 Dialog 里可以先下载模板。解析与校验都在 `MetricManager.ImportCsv`，**和手工录入共用 `MetricRules`**，所以同一份数据走两条路得到的结论一致。

| 项 | 约定 |
| --- | --- |
| 必需列 | `platform`、`account`、`date`、`views`（缺列直接整文件拒绝，并提示缺哪几列） |
| 可选列 | `likes`、`comments`、`shares`、`followers_new`，缺列按 0 处理 |
| 表头别名 | 中文可直接用：平台 / 账号 / 日期 / 播放 / 点赞 / 评论 / 转发 / 新增粉丝（也认「阅读量」「涨粉」等 Excel 常见叫法） |
| 单次上限 | 2000 数据行、4 MB；超限提示拆分文件，不截断 |
| 账号匹配 | 平台 + 账号标识（忽略大小写、自动去 `@`）；`paused` 账号该行报错 |
| 重复数据 | 库里已有的同账号同日记录按**覆盖**计 `updated`；同一文件内重复以靠后的行为准，计 `duplicateRows` |
| 错误策略 | 逐行独立校验，错误行不写入；其余行照常入库，全部失败时才整体不写。错误明细带**物理行号**（含表头偏移与引号内换行），最多返回 20 条，超出时置 `errorsTruncated` |
| 数字容错 | 接受千分位 `12,000`、Excel 导出的 `12000.0`；小数、负数、空播放视为错误并指出具体行 |
| 未识别列 | 不报错，写入 `warning` 提示「已忽略未识别的列：xxx」，避免多一列统计字段就整份导不进去 |

- 编码兼容 UTF-8 BOM 与 CRLF（Excel「另存为 CSV」的默认产物），字段内的引号与逗号按 RFC 4180 处理。
- `views` 留空不会被当成 0：空值意味着「这行可能漏填」，宁可报错也不写脏数据。
- 写库走 `IDatabaseAccessor.UpsertDocuments`（单 session / 单次 `SaveAll`），避免逐条写导致本地 JSON 实现反复全量重写；1000 行实测约 20–120 ms。
- 导入后的首页「数据来源」会显示 `CSV 导入 n 条`（`DashboardDataSourceDto.CsvCount`），与手工录入分开计数。

### 机会与风险判定规则

`DashboardManager.GetSummary` 同时返回 `Opportunities` 与 `Risks`，两者都从区间内的 `MetricDaily` 现场算，结论文案里的 `SignalPolicy` 由后端按**当前生效阈值**生成，前端直接展示，避免「这些结论哪来的」说不清。

阈值存 `SignalThresholdConfig`（单例文档，主键固定），可在 `/settings` 页面修改，保存即生效；库里没有这条文档时按默认值计算。下表的「默认」列就是 `SignalThresholdDefaults` 的取值。

| 类别 | 规则 | 字段 | 默认 | 允许范围 |
| --- | --- | --- | --- | --- |
| 爆款机会 | 单日播放 ≥ 该账号区间日均的 N 倍，且 ≥ M | `BreakoutMultiplier` / `BreakoutMinViews` | 2 倍 / 1,000 | 1.1–10 / 0–100,000,000 |
| 掉速风险 | 后段日均播放较前段下降 ≥ N%（前段无数据则跳过） | `DeclineThresholdPercent` | 30% | 1–90% |
| 互动偏低 | 区间互动率 < N%（播放 ≥ 爆款下限才判） | `LowInteractionRatePercent` | 1% | 0.1–50% |
| 缺数据 | 未停用账号近 N 天没有日记录（区间内新建的账号不判） | `StaleDays` | 3 天 | 1–30 |

- 阈值存正数表示「下降 ≥ N%」，避免表单里出现负号；判定时比较的是 `rate <= -DeclineThresholdPercent`。
- 每个账号在同一时刻只报一条风险，按「掉速 > 互动偏低 > 缺数据」取最严重的一条，避免一个账号占满面板。
- 两个区块各最多 3 条，机会按倍数倒序、风险按严重度与幅度排序；这两个上限是展示规则，不做成配置。
- 规则文案只有一处来源（`SignalPolicyText`）：首页与设置页的「当前生效口径」用的是同一段文字，改阈值后两边自动一致。
- 没有命中时返回空列表，前端显示「区间内没有账号达到阈值」，不生成「疑似机会」这类推测行。

---

## 角色与权限

界面名称、默认视图与[产品文档 · 用户与角色](https://gsspai.github.io/mediaai/)一列一致：

| 角色标识 | 界面名称 | 默认视图 |
| --- | --- | --- |
| `admin` | 管理员 | 总览（额外展示管理面板） |
| `sales` | 商务 | 商务单 |
| `finance` | 财务 | 商务单（报价复核） |
| `planner` | 策划 | 内容计划 |
| `operations` | 运营 | 内容计划（限流处置） |
| `editor` | 剪辑 | 待办 |
| `analyst` | 分析 | 总览 |
| — | 访客（未加入业务用户组） | 总览，只读 |

角色不在本系统维护名单：以飞书用户组为准，入组即得权限、离组即失权，映射关系存 `RoleMappingConfig`。

> 角色常量已在后端 `Services/App.Api/Utilities/Roles/Roles.cs` 与前端 `src/utils/roles.ts` 两处落地，改一处必须同时改另一处。

已按权限矩阵落地的接口（后端 `[RequireRole(...)]` 拦截，前端同步隐藏入口）：

| 接口 | 允许角色 | 越权结果 |
| --- | --- | --- |
| `SettingsManager.GetSignalThresholds` / `SaveSignalThresholds` | `admin` | 403 + 中文原因 |
| `PlatformAccountManager.SaveAccount` / `DisableAccount` | `admin`、`operations` | 403 + 中文原因 |
| `MetricManager.IngestManual` / `ImportCsv` | `admin`、`operations` | 403 + 中文原因 |

读接口（`GetSummary`、`ListAccounts`、`ListRecentMetrics`）仍只要求登录，所以访客也能看总览与账号列表，但看不到任何写入口。`/settings` 路由本身对非管理员落到 `/unauthorized`。开发环境用 `AppConfiguration__AdminEmails` 指定谁能拿到 `admin`。

---

## 前端路由

产品文档定义了 6 个页面（`/login`、`/`、`/business`、`/plans`、`/connections`、`/settings`）。当前实现进度：

| 路径 | 页面 | 状态 |
| --- | --- | --- |
| `/` | 工作台首页（Dashboard） | 已挂 `ProtectedRoute` |
| `/login` | 登录 | 已实现（账号密码）；飞书免登待接入 |
| `/business` | 商务合作列表 | 占位 |
| `/business/:dealId` | 合作单详情 | 占位 |
| `/plans` | 内容计划列表 | 未建立（仅有详情占位） |
| `/plans/:planId` | 内容计划详情 | 占位 |
| `/connections` | 平台连接列表 | 已实现（账号管理 + 手工录入 + CSV 导入） |
| `/connections/:platform` | 平台连接详情 | 占位 |
| `/settings` | 设置（阈值、默认视图、通知渠道） | 已实现阈值配置；默认视图、通知渠道未做 |
| `/analysis/:viewId` | 数据分析视图 | 占位；产品文档未列此页，待并入首页管理面板后下线 |
| `/unauthorized` | 无权限 | 已统一文案与样式 |
| `*` | 404 | 已统一文案与样式 |

`ProtectedRoute`（`react-app/src/routes/ProtectedRoute.tsx`）负责受保护路由的会话水合：无 token → 跳转 `/login` 并携带来源路径，登录后回到原处；有 token 无 session → 调用 `AuthManager.GetSession`；超时 → 重试 / 重新登录。

---

## 设计系统与 UI 规范

所有页面共用 `react-app/src/components/dashboard/` 下的组件，新增页面请直接复用，不要再写一套卡片样式。

| 组件 | 职责 |
| --- | --- |
| `PageShell` | 页面留白、最大宽度（1440px）、纵向节奏 |
| `Panel` / `PanelHeader` / `PanelBody` | 面板容器与头部，统一 `surface-panel` 的圆角 / 描边 / 阴影 |
| `SectionHeader` | `eyebrow + 标题 + 说明 + 右侧操作`，支持图标 |
| `StatusPill` | 5 种语义色状态标签，固定 24px 高、等宽数字 |
| `SegmentedTabs` | 分段切换，带 `role="tablist"` / `aria-selected` |
| `TrendChart` / `TrendLegend` | 多指标走势，按各自峰值归一化，颜色取自 `chart-1` / `info` / `destructive` |

约定：

1. **只用语义色**：`bg-background`、`bg-card`、`text-foreground`、`text-muted-foreground`、`border-border`、`text-success` 等。禁止 `#101114`、`bg-white/90`、`rgba(...)` 这类硬编码主题色，否则暗色模式会失效。
2. **圆角层级**：面板 `rounded-3xl`（24px）→ 内层卡片 `rounded-2xl`（16px）→ 指标块 `rounded-xl`（12px）→ 标签 `rounded-full`。
3. **数字格式**：统一使用 `formatCompactNumber()`（`万` / `千`），表格与指标使用 `tabular-nums`；不要在页面里混用 `k`、`w`、`万`。
4. **标题层级**：区块标题必须用 `SectionHeader`（`h2`）；页面主标题用 `h1`；不要用 `CardDescription` 充当小标题。
5. **不重复展示同一指标**：指标卡、图表、图例各自承担不同信息（数值 / 走势 / 颜色映射）。
6. **状态齐全**：新增异步区域时同时提供 loading（骨架）、empty、error 三态；当前原型尚未接入接口，`TrendChart` 内先提供空数据态示例。
7. **可访问性**：可点击区域使用 `Link` / `Button`，保证 `focus-visible` 可见；图表必须带 `role="img"` 与 `aria-label`。

---

## 安全与保密信息处理

- **密钥一律不入库**：`react-app/.env`、`react-app/.env.local`、`Services/App.Api/appsettings.Development.json`、`appsettings.*.local.json` 已被 `.gitignore` 排除；`appsettings.json` 只保留占位值。
- **Token 视为机密**：`AccessToken` / `RefreshToken` 仅通过请求体与响应 envelope 传输，不写日志、不进 UI、不进错误提示、不出现在截图与文档中。
- **服务端密钥只留在服务端**：`SupabaseSecretKey`（service role）可绕过 RLS，只能用于后端管理操作，不能下发到浏览器。
- **开发签名密钥不内置在代码里**：`LocalAuthAccessor` 不含任何硬编码密钥。启动时优先读取 `AppConfiguration:LocalJwtSigningKey`；未配置则在 `~/CodeBuddy/Resources/<AppName>/local-signing.key` 生成 48 字节随机密钥，并以 `0600`（仅当前用户可读写）落盘。该文件位于仓库之外，不会随代码提交；更换密钥后已签发的本地 token 会失效，重新登录即可。
- **文档与截图需打码**：引用密钥时统一写成 `sk_****`、`eyJ****`、`<masked>`。
- **一旦误提交**：先在 Supabase / 数据库侧**轮换密钥**，再清理 Git 历史；只删提交而不轮换是无效的。

提交前自检：

```bash
git grep -nE "eyJ[A-Za-z0-9_-]{20,}|sk_[A-Za-z0-9]{10,}|postgres(ql)?://[^\"' ]+@"
git status --ignored --short | grep -E "\.env|appsettings"
```

---

## 部署

`.gitea/workflows/deploy.yml` 提供手动触发的流水线：

1. 构建前端（`npm ci && npm run build`）。
2. 发布后端（`dotnet publish -c Release`）。
3. 前端部署到 Azure Static Web Apps（入参 `SWA_DEPLOYMENT_TOKEN`）。
4. 后端部署到 Azure App Service（入参 `APP_SERVICE_PUBLISH_PROFILE`）。

三个入参均由触发者在运行工作流时手动传入，不落库、不写入仓库；CORS 由 Azure 侧配置，生产环境不在应用内重复输出 CORS 头。

### 文档站

产品文档（`docs/`）不参与上述流水线，由 GitHub Pages 独立发布：直接托管 `main` 分支 `/docs` 下的静态文件，无构建步骤。改动文档只需推送到 `main`，Pages 会自动重新发布，通常 1 分钟内生效。

注意：Pages 对 Private 仓库需付费账户；本仓库为 Public，可直接启用。

---

## 下一步计划

> 产品定位、角色与指标口径见[产品文档](https://gsspai.github.io/mediaai/)；完整任务拆解（M0-M4）、数据模型字段、Manager 接口清单与验收标准见 [docs/implementation-plan.md](docs/implementation-plan.md)。

### P0 — 打通「有数据、有身份」的主链路

1. **飞书免登**：登录页并列飞书入口，新增 `/auth/feishu/callback`，由 `FeishuManager.ExchangeCode` 换 `open_id` 并签发本系统 JWT；飞书凭据只走 `Feishu__*` 环境变量，token 不下发浏览器。（账号密码登录与 `ProtectedRoute` 已完成）
2. **角色口径对齐**
   - 新增 `RoleMappingConfig`（飞书用户组 → 角色）与 `SyncDirectory` 定时同步，离职自动置 `Disabled`；
   - ~~业务接口按权限矩阵挂 `[RequireRole(...)]`~~ — 已接入设置页与账号/指标写接口，其余接口随落地补齐。
3. **后端数据骨架**：新增 `DashboardManager` + DTO（平台走势、KPI、机会/风险、待办、合作单摘要），工作台改为通过 `ApiClient` 拉取并删除 mock 常量。（`GetSummary` 四个 KPI、平台走势、机会/风险已完成，待办、合作单摘要待补）
   - 机会/风险阈值已可配：`/settings` 改完保存即生效，规则文案与首页同源（`SignalPolicyText`）。
4. **详情页落地**：先做 4 类详情页的骨架（面包屑、返回、标题区、占位区块），替换现有 `WorkspaceDetailPlaceholderView`。

### P1 — 让页面「可用、可信」

5. **状态与反馈**：统一 loading 骨架、空数据、错误重试、无权限四态组件。
6. **图表交互**：时间范围（5 条 / 周 / 月）切换、hover 数值、平台筛选；把归一化说明放进图例旁。
7. **平台连接**：账号授权状态、同步任务与失败重试、限流告警，对应首页「同步异常」指标。

### P2 — 体验与管理视角

8. **管理层指标口径**：指标可配置、口径说明、导出。
9. **暗色模式**：在应用壳层加主题切换（token 已就绪，页面已无硬编码颜色）。
10. **移动端**：表格转为卡片列表，触控区域与字号适配；补充关键页面端到端测试。

### 验收参考

- 工作台所有数字来自接口，刷新后与后端一致；每条结论都能下钻到明细记录；
- 未登录访问 `/` 会被引导到 `/login`，飞书授权后按角色进入对应默认视图；
- 前后端角色清单一致，越权调用返回 403；改 URL 参数看不到他人数据；
- 未加入任何业务用户组的同事只能看、不能改；
- 商务单走完飞书审批后列表阶段自动更新，非法流转被拒；
- 页面不出现硬编码主题色，暗色模式下无不可读文本。

完整验收清单（M3 逐条勾选）见[产品文档 · 验收标准](https://gsspai.github.io/mediaai/)。
