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
| 工作台首页（`/`） | 已完成（静态） | 平台态势、爆款机会、限流风险、执行列表、管理层概览 |
| 角色化视图 | 已完成（前端） | 按角色决定默认执行列表，`admin` 额外展示管理层概览 |
| 统一设计系统 | 已完成 | `PageShell` / `Panel` / `SectionHeader` / `StatusPill` / `TrendChart` / `SegmentedTabs` |
| 产品文档 | 已发布 | [gsspai.github.io/mediaai](https://gsspai.github.io/mediaai/)，含定位、角色、指标口径与验收标准 |
| 详情页 | 占位 | `/business`、`/plans/:planId`、`/connections/:platform`、`/analysis/:viewId` 指向占位视图；`/settings` 尚未建立 |
| 认证接口 | 骨架可用 | `AuthManager` 提供登录、注册、改密、会话等 8 个方法；飞书免登未接入 |
| 业务接口 | 未开始 | 尚无 Dashboard / 商务 / 计划 / 连接 / 分析的 Manager 与 DTO |
| 登录 | 可用 | `/login` 账号密码登录，成功后回到来源路径；飞书免登未接入 |
| 暗色模式 | 部分 | Token 已就绪，但页面尚无主题切换入口 |

> 工作台中的平台、账号、合作单数据均为演示数据，集中在 `react-app/src/views/ExampleView.tsx` 顶部的常量中，接入接口时整体替换。

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

> 角色常量已在后端 `Services/App.Api/Utilities/Roles/Roles.cs` 与前端 `src/utils/roles.ts` 两处落地，改一处必须同时改另一处。当前尚无业务接口挂 `[RequireRole(...)]`，权限矩阵待接口落地后逐条接入。

---

## 前端路由

产品文档定义了 6 个页面（`/login`、`/`、`/business`、`/plans`、`/connections`、`/settings`）。当前实现进度：

| 路径 | 页面 | 状态 |
| --- | --- | --- |
| `/` | 工作台首页（Dashboard） | 静态实现；已挂 `ProtectedRoute` |
| `/login` | 登录 | 已实现（账号密码）；飞书免登待接入 |
| `/business` | 商务合作列表 | 占位 |
| `/business/:dealId` | 合作单详情 | 占位 |
| `/plans` | 内容计划列表 | 未建立（仅有详情占位） |
| `/plans/:planId` | 内容计划详情 | 占位 |
| `/connections` | 平台连接列表 | 未建立（仅有详情占位） |
| `/connections/:platform` | 平台连接详情 | 占位 |
| `/settings` | 设置（阈值、默认视图、通知渠道） | 未建立 |
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
   - 业务接口按权限矩阵挂 `[RequireRole(...)]`。
3. **后端数据骨架**：新增 `DashboardManager` + DTO（平台走势、KPI、机会/风险、待办、合作单摘要），工作台改为通过 `ApiClient` 拉取并删除 mock 常量。
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
