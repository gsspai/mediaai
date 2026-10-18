# HANDOFF

> 交接文档 · 生成于 2026-10-18（会话结束快照）
> 仓库：`gsspai/mediaai`（public） · 分支 `main` · 工作区干净，无未推送提交

---

## 1. 当前进度

本次会话的主线是**把产品文档做成可分享的线上页面，并让 README、前端文案、文档本身三处口径一致**，最后补上移动端适配。

已到达的状态：

- 产品文档已发布为静态站点，地址 **https://gsspai.github.io/mediaai/**，手机与桌面都能正常打开。
- README 已按产品文档重写，不再出现「新传媒公司 AI 运营驾驶舱」这类旧表述。
- 前端角色显示名、页面 meta、`.env.example` 描述已同步。
- 本地开发未受任何影响；后端代码本次未改动。

**未开始**：飞书免登、业务角色落地（`[RequireRole]`）。首页四个 KPI 与平台走势已接真实接口，其余区块仍是演示数据。

---

## 2. 已完成事项

### 2.1 提交记录（`main` 分支）

| 版本 | 时间 | 内容 |
| --- | --- | --- |
| `81ba382` | 2026-10-18 00:22 | 产品文档适配移动端：取消固定侧栏、表格与甘特图横向滚动 |
| `b72fdac` | 2026-10-18 00:05 | 统一口径为「个人自媒体矩阵运营工作台」，README 与产品文档对齐 |
| `98183e4` | 2026-10-17 23:59 | README 增加文档入口与 GitHub Pages 发布说明 |
| `8793153` | 2026-10-17 23:56 | 增加 Pages 入口页 `docs/index.html` 与 `docs/.nojekyll` |
| `89132df` | 2026-10-17 23:55 | 新增产品文档与落地实施方案；看板接入 dashboard 组件与真实数据结构 |

### 2.2 产品文档与发布

- `docs/product-overview.html` — 单文件静态文档，10 章：产品定位、用户与角色、信息架构、界面结构、关键流程、指标口径、权限设计、分期计划、非功能需求、验收标准。纯 CSS 可视化（角色卡、流程图、甘特图、权限矩阵），无外部依赖、无构建。
- `docs/index.html` — `product-overview.html` 的**逐字节副本**，作为 Pages 根路径入口。
- `docs/.nojekyll` — 跳过 Jekyll 处理。
- Pages 配置：`Deploy from a branch` → `main` / `/docs`，推送到 `main` 后约 1 分钟自动发布。

### 2.3 口径统一（`b72fdac`）

| 位置 | 改为 |
| --- | --- |
| README 标题 | `MediaAI — 个人自媒体矩阵运营工作台` |
| README 定位句 | 面向个人自媒体矩阵，附产品文档链接 |
| README 技术栈 | 身份改为飞书自建应用免登；Supabase 退为数据库托管 |
| README 认证与会话 | 飞书换 `open_id` → 本系统 JWT；角色来自用户组映射 + `OwnerId` 二次校验 |
| README 角色表 | 管理员 / 商务 / 财务 / 策划 / 运营 / 剪辑 / 分析 / 访客，默认视图按产品文档 |
| README 路由表 | 12 行，逐条标注「静态 / 占位 / 未建立」 |
| README 环境变量 | 新增 `Feishu__*` 五组配置表 |
| README 文档章节 | 「首次启用」改为「已启用」，写明 `main` + `/docs` |
| README 验收 | 4 条扩到 7 条（免登、只读、审批回写、数据隔离） |

同步改动：

- `react-app/src/views/ExampleView.tsx` — `getRoleLabels` 七个显示名改为管理员 / 商务 / 财务 / 策划 / 运营 / 剪辑 / 分析，空角色由「团队协同」改为「访客」；mock 数据 `owner` 由「管理层 / 周岚」改为「管理员 / 周岚」。lint 0 错误。
- `react-app/index.html` — meta description 改为「个人自媒体矩阵运营工作台」。
- `react-app/.env.example` — `VITE_APP_DESCRIPTION` 同步。
- `docs/product-overview.html` — 3 处 `MediaAI Dashboard` 统一为 `MediaAI`；页眉日期改为 2026-10-18。

### 2.4 移动端适配（`81ba382`）

**根因**：`.layout` 固定 `224px + 1fr` 两栏，全文只有 900 / 820 / 760px 三个断点，**没有一个断点处理侧栏**。375px 屏幕上正文只剩约 150px，中文每行 3–5 字，表现为「乱码」。文件本身是 UTF-8 无 BOM、`<meta charset>` 与 viewport 齐全，**不是编码问题**。

新增 `@media(max-width:768px)` 与 `max-width:380px`：

- 布局改单列；侧栏转为吸顶横条，10 个章节变成可横滑胶囊导航
- `.tw` 改 `overflow-x:auto`，表格 `min-width:540px`
- 甘特图容器横滚，列宽下限 38px，条内 7px 文字隐藏（人日看左侧标签）
- h1 36→26px（380px 以下 23px），`.g2/.g3/.g4` 单列，KPI 两列
- 内联 `code` 与长 URL 加 `overflow-wrap:anywhere`

**验证**：无头 Chrome 在 320 / 390 / 768 / 1280 四档实测，`documentElement.scrollWidth === innerWidth`，越界元素列表全空；1280px 桌面布局未变。探针文件 `docs/_probe.html` 已删除，未入库。

### 2.5 认证闭环（P0 第 1 步）

- `Roles.cs` 与 `src/utils/roles.ts` 补齐 7 个业务角色；`getRoleLabels` 收敛到 `utils/roles.ts` 单一来源，技术角色 `user` / `authenticated` 不显示，只剩它们时按「访客」。
- 新增 `react-app/src/views/LoginView.tsx`：账号密码登录 → `AuthStore.setAuth` → 回到来源路径；飞书入口留待接入。
- `Routes.tsx` 挂 `/login` 与 `ProtectedRoute` 分组；无 token → `/login` 并带 `state.from`。
- 首页顶部增加邮箱与「退出」按钮。
- 新增 `react-app/setup-tests.ts`（原先缺失，`npm test` 直接崩）与 `src/utils/roles.test.ts`，8 条测试通过。
- 端到端实测（CDP 驱动无头 Chrome）：未登录访问 `/business` → `/login` → 填表登录 → 回 `/business`；首页显示「访客」徽章与邮箱；点「退出」→ `/login`。
- 本地测试账号：`demo@mediaai.local` / `demo12345`，`Roles: ["user"]`，界面上按访客只读显示。

### 2.6 首页接真实接口（P0 第 2 步，本轮）

代码：

- 新增 Marten 文档 `Services/Contracts.Domain/Database/PlatformAccount.cs`（`Platform` / `Handle` / `Status` / `FollowerCount` / `OwnerId`，状态枚举 `connected` / `manual` / `paused` / `error`）。
- 新增 `Services/Contracts.Domain/Database/MetricDaily.cs`（`AccountId` / `Platform` / `StatDate` / `Views` / `Likes` / `Comments` / `Shares` / `NewFollowers` / `Source` / `RecordedBy` / `RecordedAt`）。
- 新增 `Services/Contracts.Dto/DashboardSummaryDto.cs` 与 `GetDashboardSummaryRequestDto.cs`；`RangeStart`/`RangeEnd` 用 `string`（`yyyy-MM-dd`），DTO 层不引入 `DateOnly`。
- 新增 `Services/App.Api/Managers/DashboardManager.cs`：`[RequireAuthenticated] GetSummary`，返回四个 KPI（`top-platform` / `declining-platform` / `total-views` / `new-followers`）、平台走势（前 3）、`DataSource` 计数。
- 新增 `Services/App.Api/Utilities/PlatformAggregate.cs`：`App.Api.Managers` 命名空间下所有类型都会被当成 `IManagerService` 扫描，聚合用的内部类必须放到 `Utilities`。
- 新增 `Services/App.Api/Utilities/DashboardSeeder.cs` + `Program.cs` 启动钩子：仅 Development、仅当库中无 `MetricDaily` 时写入 5 个账号 × 7 天 = 35 条，`Source = Seed`、`RecordedBy = seed:development`。
- 前端新增 `src/api/DashboardManager.ts`、`src/utils/platforms.ts`（平台英文标识 → 中文名），`src/api/AppDtos.ts` 追加 7 个 DTO。
- `src/views/ExampleView.tsx`：删除硬编码 `metricCards` / `platformSeries`，改为 `fallbackMetricCards` / `fallbackPlatformSeries`（每条 `trace` 写「静态示例数据 · 接口未连接」）；新增 `summary` / `summaryStatus` / `summaryReloadKey`，错误时红色横幅带重试按钮；KPI 卡加 `trace` 行与 `min-h-[168px]`，`pending` 时 `opacity-60`。

口径修正：

- 初版环比用前后两段**总量**比较，7 天区间前 3 后 4 天，结果系统性偏高（微博 -47%）。改为**日均播放**对比（`DailyChangeRate`），修正为 -31%，文案改「日均环比」「前 3 天与后 4 天的日均播放对比」。

验证：

- `dotnet build Services/Services.sln` 0 错误；`npx tsc --noEmit` 0 错误；`npx vitest run --mode test` 8 条通过。
- CDP 驱动无头 Chrome 实测：未登录 `/business` → `/login` → 登录 → 回 `/business`；首页输出 `hasRealSource: true` / `hasFallback: false` / `hasErrorBanner: false`，读数「最强平台 96.9 万 抖音」「下滑平台 -31% 微博」「总播放 214 万 / 互动率 10.1%」「新增粉丝 3,426」「数据来源：开发态示例数据 · 5 个账号 / 35 条日记录（2026-10-12 至 2026-10-18）」。
- `npm run lint` 仍因仓库缺 `typescript-eslint` 报 `ERR_MODULE_NOT_FOUND`（既有问题，非本轮引入），已用 `tsc` + `vitest` 替代。

---

## 3. 遗留问题

| # | 问题 | 影响 | 状态 |
| --- | --- | --- | --- |
| 1 | `/analysis/:viewId` 路由去留未定 | 产品文档只有 6 个页面，此路由无对应页面，代码仍指向占位视图 | **待用户决定** |
| 2 | 业务角色已定义，但没有任何接口挂 `[RequireRole(...)]` | 权限矩阵尚未生效，越权访问仍可能通过 | 待做（随接口落地） |
| 3 | 飞书免登未接入 | `/auth/feishu/callback` 与 `FeishuManager` 不存在，登录页只支持账号密码 | 待做（P0） |
| 4 | 首页四个 KPI 与平台走势已接接口，其余区块（爆款机会、限流风险、执行列表、合作单）仍是前端常量 | 验收第 1 条对首页指标已成立；下钻明细仍无 | 部分完成 |
| 5 | ~~`/` 未挂 `ProtectedRoute`~~ | 已完成 | ✅ |
| 6 | 无任何自动化测试与 CI | 改动靠人工验证 | 未规划 |
| 7 | 推文文案只存在于对话中 | X / Facebook 两版成稿未落文件，若要发布需重新生成 | 未落地 |

**待确认**：第 1 项需要一句答复——现在删除该路由，还是等 M3 首页接真实接口时并入管理面板一并处理。

---

## 4. 下一步计划

按依赖顺序，建议：

1. ~~**首页接真实接口**：`DashboardManager` + DTO，先接四个指标卡~~ — 已完成（见 2.6）
2. ~~**数据模型**：`PlatformAccount` + `MetricDaily`~~ — 已完成（见 2.6）

接下来，建议按以下顺序：

1. **手工录入 / 接口同步入口**（下一步）：`MetricDaily` 已有 `Source` + `RecordedBy`，但还**没有写入路径**，种子数据是唯一数据来源。最小闭环是 `MetricManager.UpsertDaily`（人工录入某账号某天）与 `PlatformAccountManager` 的增删改查，界面上放在「平台连接」页。
2. **首页剩余区块接接口**：爆款机会、限流风险、执行列表、合作单摘要，补齐 `GetSummary` 或拆成独立方法；每块都要给出可下钻的明细。
3. **飞书免登**：`/auth/feishu/callback` + 登录页飞书入口，与账号登录共用同一签发出口
4. **`RoleMappingConfig`**（飞书用户组 → 角色，存库热改）+ `SyncDirectory` 定时同步，离职置 `Disabled`
5. **`/analysis/:viewId` 去留**（一行决定，可随时处理）

产品文档给出的分期是 M0–M4、共 36 人日（M0 7.5 / M1 9 / M2 11.5 / M3 8），为估算值，大概率会超。

---

## 5. 关键上下文

### 5.1 文件关系（易踩坑）

- **`docs/index.html` 是 `docs/product-overview.html` 的副本**。改完正文必须执行 `cp docs/product-overview.html docs/index.html`，否则首页不变。这是本次最容易漏的一步。
- Pages 从 `main` 的 `/docs` 目录发布，`/docs` 下**任何**新增文件都会被公开，不要放草稿。
- `.gitignore` 已排除 `.env`、`appsettings.*.json`、`local-signing.key`。
- **`App.Api.Managers` 命名空间下只放 Manager**：反射会扫描该命名空间的 `IManagerService`，任何辅助类（如聚合用的 `PlatformAggregate`）必须放到 `Utilities`，静态工具类同样会被当文档类型扫描。
- 调 `/Api/invoke` 时 **`AccessToken` 与 `RefreshToken` 必须同时带**，只给 `AccessToken` 会失败（与 `AuthManager.Login` 返回一致）。

### 5.2 本地环境

- 预览服务：在 `docs/` 下跑 `python3 -m http.server 4174`，访问 `http://localhost:4174/product-overview.html`。**该进程可能仍在后台运行**，端口冲突时先 `lsof -ti:4174 | xargs kill`。
- 无头截图 / 布局度量：`/Applications/Google Chrome.app/Contents/MacOS/Google Chrome --headless=new --dump-dom`，配合同源 iframe 探针可读取 `scrollWidth` 实测溢出（探针文件用完即删）。
- 推送凭据由 IDE 临时注入，**不可用于 GitHub Settings API**；机器上无 gh CLI token、无钥匙串凭据、无 `GITHUB_TOKEN`、无 `~/.netrc`。需要改仓库设置时只能人工操作。
- 改完后端不想打扰正在跑的 `https://localhost:5123`：用 `dotnet run --no-build --urls https://localhost:5124` 起一个独立实例验证，验完 kill 掉。前端 `react-app/.env` 的 `VITE_API_URL` 指向验证实例时，**验完必须改回 5123**。
- 重置开发态种子数据：删掉本地数据目录下 `PlatformAccount.json` 与 `MetricDaily.json`（路径含 `AppConfiguration__AppName`，默认 `~/CodeBuddy/Resources/MediaAI/` 附近），重启后端即重新生成 5 个账号 × 7 天。

### 5.3 口径约定（后续改动请沿用）

- 产品名：**MediaAI**（不再用 `MediaAI Dashboard`）
- 一句话定位：把多个平台账号的播放与互动、爆款机会、限流风险，连同商务合作与内容排期，收敛到同一块屏幕
- 角色显示名：管理员 / 商务 / 财务 / 策划 / 运营 / 剪辑 / 分析 / 访客
- 硬约束：**每个数字都必须能回溯到「谁、在哪个平台、哪一天、录了什么或同步了什么」**，无法回溯的展示数据不上线

### 5.4 验证现状

线上地址 `https://gsspai.github.io/mediaai/` 已确认返回 200，且含移动端 CSS（`max-width:768px`）。桌面与四档移动视口均无横向溢出。
