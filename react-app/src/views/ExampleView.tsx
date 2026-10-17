import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import useAuthStore from '@/auth/AuthStore';
import { Activity, ArrowRight, CalendarRange, Flame, Siren, Sparkles } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';

const workspaceTabs = [
  { value: 'business', label: '商务合作' },
  { value: 'plans', label: '日常内容计划' },
  { value: 'todo', label: '跨角色待办' },
] as const;

type WorkspaceTab = (typeof workspaceTabs)[number]['value'];
type StatusTone = 'default' | 'success' | 'warning' | 'info' | 'destructive';

const HomeView = () => {
  const session = useAuthStore((state) => state.session);
  const roleLabels = getRoleLabels(session?.Roles ?? []);
  const primarySystemRole = session?.Roles?.[0] ?? null;
  const defaultTab = getDefaultTab(primarySystemRole);
  const [activeTab, setActiveTab] = useState<WorkspaceTab>(defaultTab);
  const isManagement = (session?.Roles ?? []).includes('admin');

  const metricCards = [
    { label: '最强平台', value: '抖音', detail: '+32%', tone: 'success' as StatusTone },
    { label: '风险平台', value: '微博', detail: '3 条限流', tone: 'destructive' as StatusTone },
    { label: '爆款内容', value: '05', detail: '+2 今日新增', tone: 'success' as StatusTone },
    { label: '同步异常', value: '02', detail: '待修复', tone: 'warning' as StatusTone },
  ];

  const platformSeries = [
    { platform: '抖音', tone: 'success' as StatusTone, posts: ['01', '02', '03', '04', '05'], views: [82, 94, 108, 132, 168], comments: [14, 16, 19, 26, 33], shares: [7, 8, 10, 13, 16] },
    { platform: '公众号', tone: 'info' as StatusTone, posts: ['01', '02', '03', '04', '05'], views: [68, 72, 70, 78, 84], comments: [11, 12, 12, 14, 15], shares: [5, 6, 5, 7, 8] },
    { platform: '微博', tone: 'destructive' as StatusTone, posts: ['01', '02', '03', '04', '05'], views: [74, 67, 59, 45, 39], comments: [10, 9, 8, 6, 5], shares: [6, 5, 4, 3, 2] },
  ];

  const breakoutContent = [
    { title: '供应链幕后', platform: '抖音', signal: '2.8x', stat: '18.6w 播放', href: '/plans/daily-hotspot' },
    { title: '品牌增长拆解', platform: '公众号', signal: '14.2% 打开率', stat: '6.3w 阅读', href: '/plans/brand-case' },
  ];

  const throttledContent = [
    { title: '周末热点追踪', platform: '微博', signal: '-42%', reason: '话题匹配弱', href: '/plans/weekly-tech' },
    { title: '新品测评卡点', platform: '抖音', signal: '完播率偏低', reason: '前 3 秒留存不足', href: '/plans/daily-hotspot' },
  ];

  const businessRows = [
    { name: '青岚科技春季合作', owner: '商务 / 林乔', status: '待报价审核', statusTone: 'warning' as StatusTone, nextStep: '财务确认报价区间', href: '/business/deal-qinglan' },
    { name: '云屿品牌发布专项', owner: '管理层 / 周岚', status: '待管理确认', statusTone: 'info' as StatusTone, nextStep: '确认排期与交付边界', href: '/business/deal-yunyu' },
    { name: '洛川汽车区域投放', owner: '运营 / 许舟', status: '待制作 / 发布', statusTone: 'default' as StatusTone, nextStep: '补齐短视频封面与发布时间', href: '/business/deal-luochuan' },
  ];

  const planRows = [
    { name: '今日热点窗口', cycle: '今日 14:00', score: '8.7 / 10', direction: '抖音故事化、公众号深度拆解', href: '/plans/daily-hotspot' },
    { name: '周度科技观察', cycle: '本周', score: '7.9 / 10', direction: '微博快评、头条延展报道', href: '/plans/weekly-tech' },
    { name: '品牌案例拆解', cycle: '本周', score: '7.5 / 10', direction: '公众号长文、小红书图文复盘', href: '/plans/brand-case' },
  ];

  const todoRows = [
    { title: '抖音账号授权即将失效', owner: '运营 / 发布', urgency: '高优先', href: '/connections/douyin' },
    { title: '3 条商务单等待财务复核报价', owner: '财务', urgency: '今日处理', href: '/business?status=pricing-review' },
    { title: '本周内容计划缺少微博方向说明', owner: '策划', urgency: '需补全', href: '/plans/weekly-tech' },
  ];

  const managementOverview = [
    { label: '进行中商务合作', value: '18' },
    { label: '本周内容计划', value: '12' },
    { label: '同步异常账号', value: '02' },
  ];

  return (
    <main className="min-h-screen px-7 py-8 text-foreground lg:px-10 lg:py-10">
      <section className="mx-auto flex w-full max-w-[1500px] flex-col gap-6">
        <section className="overflow-hidden rounded-[2.2rem] border border-border/55 bg-[#101114] text-white shadow-[0_30px_80px_-48px_rgba(0,0,0,0.45)]">
          <div className="grid min-h-[340px] gap-0 lg:grid-cols-[1.2fr_0.8fr]">
            <div className="relative flex flex-col justify-between overflow-hidden px-8 py-8 lg:px-10 lg:py-10">
              <div className="absolute -left-10 top-8 h-56 w-56 rounded-full bg-white/6 blur-3xl" />
              <div className="absolute right-20 top-16 h-40 w-40 rounded-full border border-white/10" />
              <div className="absolute bottom-0 right-0 h-48 w-72 bg-[radial-gradient(circle_at_top_left,rgba(255,255,255,0.16),transparent_55%)]" />

              <div className="relative z-10 space-y-4">
                <div className="flex items-center gap-3 text-[11px] font-medium uppercase tracking-[0.24em] text-white/55">
                  <span>MediaAI</span>
                  <span className="h-1 w-1 rounded-full bg-white/30" />
                  <span>Operations Cockpit</span>
                </div>
                <h1 className="max-w-3xl text-[2.8rem] font-semibold tracking-[-0.07em] text-white sm:text-[3.6rem] lg:text-[4.8rem] lg:leading-[0.96]">
                  Content
                  <br />
                  Intelligence
                  <br />
                  Dashboard
                </h1>
                <p className="max-w-xl text-sm leading-7 text-white/62">
                  先看平台态势，再判断爆款机会与限流风险。
                </p>
              </div>

              <div className="relative z-10 flex flex-wrap items-center gap-2 pt-8">
                {roleLabels.map((role) => (
                  <Badge key={role} variant="outline" className="rounded-full border-white/14 bg-white/6 px-3 py-1 text-[10px] font-medium tracking-[0.16em] uppercase text-white/72">
                    {role}
                  </Badge>
                ))}
                {isManagement ? <Badge className="rounded-full border border-white/16 bg-white text-[#101114]">管理视图</Badge> : null}
              </div>
            </div>

            <div className="relative grid gap-px bg-white/8 p-px">
              <div className="grid gap-px bg-white/8 sm:grid-cols-2 lg:grid-cols-2">
                {metricCards.map((item) => (
                  <div key={item.label} className="flex min-h-[170px] flex-col justify-between bg-[#15161A] px-8 py-7">
                    <div className="flex items-start justify-between gap-4">
                      <p className="max-w-[90px] text-[15px] leading-9 tracking-[-0.04em] text-white/72">{item.label}</p>
                      <StatusPill tone={item.tone}>{item.detail}</StatusPill>
                    </div>
                    <div className="text-[4.5rem] font-semibold leading-none tracking-[-0.1em] text-white">{item.value}</div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </section>

        <Card className="overflow-hidden rounded-[2rem] border-border/55 bg-white/92 shadow-[0_22px_70px_-44px_rgba(15,23,42,0.18)]">
          <CardHeader className="border-b border-border/55 px-7 pb-5 pt-6 lg:px-8">
            <div className="flex items-end justify-between gap-4">
              <div>
                <CardDescription className="text-[12px] uppercase tracking-[0.18em]">平台态势</CardDescription>
                <CardTitle className="mt-2 text-[1.75rem] tracking-[-0.05em]">最近 5 条更新</CardTitle>
              </div>
              <div className="flex items-center gap-4 text-[11px] text-muted-foreground">
                <LegendDot label="浏览量" color="bg-foreground" />
                <LegendDot label="评论量" color="bg-[color:var(--info)]" />
                <LegendDot label="转发量" color="bg-destructive" />
              </div>
            </div>
          </CardHeader>
          <CardContent className="grid gap-4 p-4 lg:grid-cols-3 lg:p-5">
            {platformSeries.map((item) => (
              <div key={item.platform} className="rounded-[1.8rem] border border-border/50 bg-[linear-gradient(180deg,rgba(255,255,255,0.96),rgba(247,248,249,0.92))] p-5">
                <div className="mb-5 flex items-start justify-between gap-4">
                  <div>
                    <p className="text-[1.05rem] font-semibold tracking-[-0.03em]">{item.platform}</p>
                    <p className="mt-1 text-xs text-muted-foreground">05 条更新走势</p>
                  </div>
                  <StatusPill tone={item.tone}>{item.views[item.views.length - 1]}k</StatusPill>
                </div>
                <MultiMetricChart views={item.views} comments={item.comments} shares={item.shares} />
                <div className="mt-5 grid grid-cols-3 gap-2">
                  <MetricTile label="浏览" value={`${item.views[item.views.length - 1]}k`} />
                  <MetricTile label="评论" value={`${item.comments[item.comments.length - 1]}k`} />
                  <MetricTile label="转发" value={`${item.shares[item.shares.length - 1]}k`} />
                </div>
                <div className="mt-4 grid grid-cols-5 text-center text-[11px] text-muted-foreground">
                  {item.posts.map((post) => (
                    <span key={post}>{post}</span>
                  ))}
                </div>
              </div>
            ))}
          </CardContent>
        </Card>

        <div className="grid gap-4 xl:grid-cols-[1.05fr_0.95fr]">
          <FocusPanel title="爆款机会" icon={Flame} tone="success">
            {breakoutContent.map((item) => (
              <SignalRow key={item.title} title={item.title} platform={item.platform} signal={item.signal} stat={item.stat} href={item.href} tone="success" />
            ))}
          </FocusPanel>

          <FocusPanel title="限流 / 掉速风险" icon={Siren} tone="destructive">
            {throttledContent.map((item) => (
              <SignalRow key={item.title} title={item.title} platform={item.platform} signal={item.signal} stat={item.reason} href={item.href} tone="destructive" />
            ))}
          </FocusPanel>
        </div>

        {isManagement ? (
          <Card className="rounded-[2rem] border-border/55 bg-white/90 shadow-[0_20px_60px_-42px_rgba(15,23,42,0.16)]">
            <CardHeader className="px-7 pb-3 pt-6">
              <CardDescription className="text-[12px] uppercase tracking-[0.18em]">管理层概览</CardDescription>
              <CardTitle className="mt-2 text-[1.5rem] tracking-[-0.04em]">跨业务总量</CardTitle>
            </CardHeader>
            <CardContent className="grid gap-3 p-5 sm:grid-cols-3">
              {managementOverview.map((item) => (
                <div key={item.label} className="rounded-[1.6rem] border border-border/50 bg-background/75 px-5 py-5">
                  <p className="text-sm text-muted-foreground">{item.label}</p>
                  <div className="mt-4 text-[2.3rem] font-semibold tracking-[-0.06em]">{item.value}</div>
                </div>
              ))}
            </CardContent>
          </Card>
        ) : null}

        <section className="space-y-4">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
            <div className="space-y-1">
              <p className="text-[12px] uppercase tracking-[0.16em] text-muted-foreground">业务承接</p>
              <h2 className="text-[1.7rem] font-semibold tracking-[-0.05em]">执行列表</h2>
            </div>
            <div className="flex gap-2 rounded-full border border-border/60 bg-white/85 p-1 shadow-sm">
              {workspaceTabs.map((tab) => (
                <button
                  key={tab.value}
                  type="button"
                  onClick={() => setActiveTab(tab.value)}
                  className={`rounded-full px-4 py-2 text-sm font-medium transition-colors ${activeTab === tab.value ? 'bg-foreground text-background' : 'text-muted-foreground hover:text-foreground'}`}
                >
                  {tab.label}
                </button>
              ))}
            </div>
          </div>

          {activeTab === 'business' ? (
            <DataPanel title="商务合作" actionLabel="查看全部" actionHref="/business">
              <Table>
                <TableHeader>
                  <TableRow className="border-border/55 hover:bg-transparent">
                    <TableHead className="px-6">事项</TableHead>
                    <TableHead>负责人</TableHead>
                    <TableHead>状态</TableHead>
                    <TableHead>下一步</TableHead>
                    <TableHead className="px-6 text-right">详情</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {businessRows.map((row) => (
                    <TableRow key={row.name} className="border-border/45">
                      <TableCell className="px-6 py-5 font-medium whitespace-normal">{row.name}</TableCell>
                      <TableCell className="py-5 text-muted-foreground">{row.owner}</TableCell>
                      <TableCell className="py-5"><StatusPill tone={row.statusTone}>{row.status}</StatusPill></TableCell>
                      <TableCell className="py-5 whitespace-normal text-muted-foreground">{row.nextStep}</TableCell>
                      <TableCell className="px-6 py-5 text-right">
                        <Button variant="ghost" size="sm" asChild>
                          <Link to={row.href}>进入详情<ArrowRight className="size-4" /></Link>
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </DataPanel>
          ) : null}

          {activeTab === 'plans' ? (
            <div className="grid gap-4 xl:grid-cols-[1.3fr_0.7fr]">
              <DataPanel title="内容计划">
                <Table>
                  <TableHeader>
                    <TableRow className="border-border/55 hover:bg-transparent">
                      <TableHead className="px-6">计划</TableHead>
                      <TableHead>周期</TableHead>
                      <TableHead>评分</TableHead>
                      <TableHead>方向</TableHead>
                      <TableHead className="px-6 text-right">详情</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {planRows.map((row) => (
                      <TableRow key={row.name} className="border-border/45">
                        <TableCell className="px-6 py-5 font-medium whitespace-normal">{row.name}</TableCell>
                        <TableCell className="py-5 text-muted-foreground">{row.cycle}</TableCell>
                        <TableCell className="py-5"><StatusPill tone="default">{row.score}</StatusPill></TableCell>
                        <TableCell className="py-5 whitespace-normal text-muted-foreground">{row.direction}</TableCell>
                        <TableCell className="px-6 py-5 text-right">
                          <Button variant="ghost" size="sm" asChild>
                            <Link to={row.href}>进入详情<ArrowRight className="size-4" /></Link>
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </DataPanel>

              <DataPanel title="计划提醒">
                <div className="space-y-3 p-5">
                  <CompactAlert icon={Sparkles} title="微博方向待补充" />
                  <CompactAlert icon={CalendarRange} title="热点计划 16:00 前定稿" />
                  <CompactAlert icon={Activity} title="竞品观察已更新" />
                </div>
              </DataPanel>
            </div>
          ) : null}

          {activeTab === 'todo' ? (
            <DataPanel title="跨角色待办">
              <div className="space-y-3 p-5">
                {todoRows.map((row) => (
                  <Link key={row.title} to={row.href} className="block rounded-[1.5rem] border border-border/50 bg-background/72 px-5 py-4 transition-colors hover:bg-muted/55">
                    <div className="flex items-start justify-between gap-4">
                      <div className="space-y-1">
                        <p className="font-medium leading-6">{row.title}</p>
                        <p className="text-sm text-muted-foreground">{row.owner}</p>
                      </div>
                      <StatusPill tone={row.urgency === '高优先' ? 'destructive' : 'warning'}>{row.urgency}</StatusPill>
                    </div>
                  </Link>
                ))}
              </div>
            </DataPanel>
          ) : null}
        </section>
      </section>
    </main>
  );
};

const DataPanel = ({ title, children, actionLabel, actionHref }: { title: string; children: React.ReactNode; actionLabel?: string; actionHref?: string }) => (
  <Card className="overflow-hidden rounded-[2rem] border-border/55 bg-white/90 shadow-[0_20px_60px_-42px_rgba(15,23,42,0.16)]">
    <CardHeader className="border-b border-border/55 px-7 pb-4 pt-6">
      <div className="flex items-end justify-between gap-4">
        <div>
          <CardDescription className="text-[12px] uppercase tracking-[0.18em]">{title}</CardDescription>
          <CardTitle className="mt-2 text-[1.45rem] tracking-[-0.04em]">当前执行</CardTitle>
        </div>
        {actionLabel && actionHref ? (
          <Button variant="outline" asChild>
            <Link to={actionHref}>{actionLabel}</Link>
          </Button>
        ) : null}
      </div>
    </CardHeader>
    <CardContent className="p-0">{children}</CardContent>
  </Card>
);

const FocusPanel = ({ title, icon: Icon, tone, children }: { title: string; icon: typeof Activity; tone: StatusTone; children: React.ReactNode }) => (
  <Card className="overflow-hidden rounded-[2rem] border-border/55 bg-white/90 shadow-[0_20px_60px_-42px_rgba(15,23,42,0.16)]">
    <CardHeader className="border-b border-border/55 px-7 pb-4 pt-6">
      <div className="flex items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <div className="rounded-[1.2rem] border border-border/50 bg-background/80 p-2.5 text-foreground/75">
            <Icon className="size-4" />
          </div>
          <CardTitle className="text-[1.45rem] tracking-[-0.04em]">{title}</CardTitle>
        </div>
        <StatusPill tone={tone}>{tone === 'success' ? '可放大' : '需排查'}</StatusPill>
      </div>
    </CardHeader>
    <CardContent className="space-y-3 p-5">{children}</CardContent>
  </Card>
);

const SignalRow = ({ title, platform, signal, stat, href, tone }: { title: string; platform: string; signal: string; stat: string; href: string; tone: StatusTone }) => (
  <Link to={href} className="block rounded-[1.5rem] border border-border/50 bg-background/72 px-5 py-4 transition-colors hover:bg-muted/55">
    <div className="flex items-start justify-between gap-4">
      <div className="space-y-1.5">
        <div className="flex items-center gap-2">
          <p className="font-medium tracking-[-0.02em]">{title}</p>
          <StatusPill tone={tone}>{platform}</StatusPill>
        </div>
        <div className="flex flex-wrap gap-3 text-sm text-muted-foreground">
          <span>{signal}</span>
          <span>{stat}</span>
        </div>
      </div>
      <ArrowRight className="mt-1 size-4 shrink-0 text-muted-foreground" />
    </div>
  </Link>
);

const CompactAlert = ({ icon: Icon, title }: { icon: typeof Activity; title: string }) => (
  <div className="flex items-center gap-3 rounded-[1.5rem] border border-border/50 bg-background/72 px-5 py-4">
    <Icon className="size-4 text-foreground/70" />
    <span className="text-sm font-medium">{title}</span>
  </div>
);

const LegendDot = ({ label, color }: { label: string; color: string }) => (
  <div className="flex items-center gap-2">
    <span className={`h-2 w-2 rounded-full ${color}`} />
    <span>{label}</span>
  </div>
);

const MetricTile = ({ label, value }: { label: string; value: string }) => (
  <div className="rounded-[1.15rem] border border-border/45 bg-white/78 px-3 py-3 text-center">
    <div className="text-[11px] uppercase tracking-[0.12em] text-muted-foreground">{label}</div>
    <div className="mt-1.5 text-[1.35rem] font-semibold tracking-[-0.05em]">{value}</div>
  </div>
);

const MultiMetricChart = ({ views, comments, shares }: { views: number[]; comments: number[]; shares: number[] }) => {
  const allValues = [views, comments, shares].flat();
  const max = Math.max(...allValues);
  const min = Math.min(...allValues);
  const buildPoints = (values: number[]) =>
    values
      .map((value, index) => {
        const x = 4 + (index / (values.length - 1)) * 92;
        const y = max === min ? 30 : 54 - ((value - min) / (max - min)) * 42;
        return `${x},${y}`;
      })
      .join(' ');

  return (
    <svg viewBox="0 0 100 60" className="h-40 w-full" preserveAspectRatio="none">
      {[14, 26, 38, 50].map((line) => (
        <line key={line} x1="4" x2="96" y1={line} y2={line} stroke="rgba(24,24,27,0.06)" strokeWidth="0.6" />
      ))}
      <polyline fill="none" stroke="rgba(16,17,20,0.92)" strokeWidth="1.35" strokeLinecap="round" strokeLinejoin="round" points={buildPoints(views)} />
      <polyline fill="none" stroke="rgba(93,126,176,0.95)" strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round" points={buildPoints(comments)} />
      <polyline fill="none" stroke="rgba(198,88,88,0.9)" strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round" points={buildPoints(shares)} />
    </svg>
  );
};

const statusToneClasses: Record<StatusTone, string> = {
  default: 'border-border/70 bg-secondary/80 text-secondary-foreground',
  success: 'border-success/15 bg-success/10 text-[color:var(--success)]',
  warning: 'border-warning/20 bg-warning/12 text-[color:var(--warning-foreground)]',
  info: 'border-info/18 bg-info/10 text-[color:var(--info)]',
  destructive: 'border-destructive/15 bg-destructive/8 text-destructive',
};

const StatusPill = ({ children, tone }: { children: React.ReactNode; tone: StatusTone }) => (
  <span className={`inline-flex min-h-9 items-center rounded-full border px-3 py-1 text-[11px] font-medium tracking-[0.01em] ${statusToneClasses[tone]}`}>
    {children}
  </span>
);

const getRoleLabels = (roles: string[]) => {
  if (roles.length === 0) return ['团队协同'];
  return roles.map((role) => {
    if (role === 'admin') return '管理层';
    if (role === 'finance') return '财务';
    if (role === 'planner') return '策划';
    if (role === 'operations') return '运营 / 发布';
    if (role === 'editor') return '编辑 / 内容生产';
    if (role === 'analyst') return '数据分析';
    if (role === 'sales') return '商务 / 客户经理';
    return role;
  });
};

const getDefaultTab = (role: string | null): WorkspaceTab => {
  if (role === 'planner' || role === 'operations' || role === 'editor' || role === 'analyst') return 'plans';
  if (role === 'finance') return 'todo';
  return 'business';
};

export default HomeView;
