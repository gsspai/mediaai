# MediaAI — 新传媒公司 AI 运营驾驶舱

MediaAI 面向新传媒公司的多角色协作场景：把商务合作、日常内容计划、平台连接与数据分析收敛到一个工作台，先看平台态势，再判断爆款机会与限流风险。

当前仓库处于**前端原型阶段**：工作台页面、角色视图与设计系统已就绪，页面数据仍为前端 mock，后端仅保留认证骨架。

---

## 目录

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

## 当前状态

| 能力 | 状态 | 说明 |
| --- | --- | --- |
| 工作台首页（`/`） | 已完成（静态） | 平台态势、爆款机会、限流风险、执行列表、管理层概览 |
| 角色化视图 | 已完成（前端） | 按角色决定默认执行列表，`admin` 额外展示管理层概览 |
| 统一设计系统 | 已完成 | `PageShell` / `Panel` / `SectionHeader` / `StatusPill` / `TrendChart` / `SegmentedTabs` |
| 详情页 | 占位 | `/business`、`/plans`、`/connections`、`/analysis` 均指向占位视图 |
| 认证接口 | 骨架可用 | `AuthManager` 提供登录、注册、改密、会话等 8 个方法 |
| 业务接口 | 未开始 | 尚无 Dashboard / 商务 / 计划 / 连接 / 分析的 Manager 与 DTO |
| 登录页 | 未开始 | 目前没有登录 UI，工作台按匿名或已存会话渲染 |
| 暗色模式 | 部分 | Token 已就绪，但页面尚无主题切换入口 |

> 工作台中的平台、账号、合作单数据均为演示数据，集中在 `react-app/src/views/ExampleView.tsx` 顶部的常量中，接入接口时整体替换。

---

## 技术栈

- **前端**：React 18 + TypeScript + Vite + Tailwind CSS v4 + ShadCN UI（`radix-ui`）
- **状态**：Zustand
- **后端**：.NET 8 Web API，统一 RPC 入口（ServiceInvoker）
- **认证**：Supabase Auth（本地开发使用 `LocalAuthAccessor`）
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
├── docs/                          # 认证与流式 RPC 说明
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
| `VITE_APP_DESCRIPTION` | Web App Manifest 描述 | `新传媒公司 AI 运营驾驶舱` |

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
- 生产环境通过 Supabase JWKS 校验 JWT；本地开发使用本地签发与校验。
- 角色存放在 Supabase `app_metadata.roles`，由 `[RequireRole(...)]` 校验。

详见 [docs/service-invoker-auth-flow.md](docs/service-invoker-auth-flow.md) 与 [docs/service-invoker-streaming.md](docs/service-invoker-streaming.md)。

---

## 角色与权限

前端已按 7 类业务角色渲染视图与默认执行列表：

| 角色标识 | 界面名称 | 默认执行列表 |
| --- | --- | --- |
| `admin` | 管理层 | 商务合作（并展示管理层概览） |
| `sales` | 商务 / 客户经理 | 商务合作 |
| `finance` | 财务 | 跨角色待办 |
| `planner` | 策划 | 日常内容计划 |
| `operations` | 运营 / 发布 | 日常内容计划 |
| `editor` | 编辑 / 内容生产 | 日常内容计划 |
| `analyst` | 数据分析 | 日常内容计划 |

> **已知不一致**：后端 `Services/App.Api/Utilities/Roles/Roles.cs` 目前只定义 `admin` / `user` / `authenticated`，上述业务角色尚未落地，`[RequireRole("finance")]` 这类标记暂时无效。这是下一步 P0 项。

---

## 前端路由

| 路径 | 页面 | 状态 |
| --- | --- | --- |
| `/` | 工作台首页 | 静态实现 |
| `/business` | 商务合作列表 | 占位 |
| `/business/:dealId` | 合作单详情 | 占位 |
| `/plans/:planId` | 内容计划详情 | 占位 |
| `/connections/:platform` | 平台连接详情 | 占位 |
| `/analysis/:viewId` | 数据分析视图 | 占位 |
| `/unauthorized` | 无权限 | 已统一文案与样式 |
| `*` | 404 | 已统一文案与样式 |

`ProtectedRoute`（`react-app/src/routes/ProtectedRoute.tsx`）负责受保护路由的会话水合：无 token → 跳转 `/unauthorized`；有 token 无 session → 调用 `AuthManager.GetSession`；超时 → 重试 / 退出登录。

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

---

## 下一步计划

> 完整任务拆解（M0-M4）、数据模型字段、Manager 接口清单与验收标准见 [docs/implementation-plan.md](docs/implementation-plan.md)。

### P0 — 打通「有数据、有身份」的主链路

1. **角色口径对齐**
   - `Roles.cs` 增加 `sales` / `finance` / `planner` / `operations` / `editor` / `analyst`；
   - 前端新增 `src/utils/roles.ts` 统一导出角色常量与中文名，替换 `ExampleView` 内的 `roleLabels` 映射。
2. **登录入口**：新增 `/login` 视图，接入 `AuthManager.Login`，用 `ProtectedRoute` 保护 `/`；补齐登录失败、注册需二次确认的提示。
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

- 工作台所有数字来自接口，刷新后与后端一致；
- 未登录访问 `/` 会被引导到登录页，登录后按角色进入对应默认执行列表；
- 前后端角色清单一致，越权调用返回 403；
- 页面不出现硬编码主题色，暗色模式下无不可读文本。
