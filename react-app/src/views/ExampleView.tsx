import PageShell from '@/components/dashboard/PageShell';
import { Panel, PanelBody, PanelHeader } from '@/components/dashboard/Panel';
import SectionHeader from '@/components/dashboard/SectionHeader';
import SegmentedTabs from '@/components/dashboard/SegmentedTabs';
import StatusPill, { type StatusTone } from '@/components/dashboard/StatusPill';
import TrendChart, { TrendLegend, type TrendPoint } from '@/components/dashboard/TrendChart';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import useAuthStore from '@/auth/AuthStore';
import { formatCompactNumber } from '@/lib/format';
import { Activity, ArrowRight, CalendarRange, Flame, Sparkles, Siren, type LucideIcon } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';

const workspaceTabs = [
  { value: 'business', label: '商务合作' },
  { value: 'plans', label: '日常内容计划' },
  { value: 'todo', label: '跨角色待办' },
] as const;

type WorkspaceTab = (typeof workspaceTabs)[number]['value'];

type MetricCard = {
  label: string;
  value: string;
  unit?: string;
  detail: string;
  isTextValue?: boolean;
};

const metricCards: MetricCard[] = [
  { label: '最强平台', value: '抖音', detail: '播放 +32%', isTextValue: true },
  { label: '风险平台', value: '微博', detail: '3 条限流', isTextValue: true },
  { label: '爆款内容', value: '5', unit: '条', detail: '+2 今日新增' },
  { label: '同步异常', value: '2', unit: '项', detail: '待修复' },
];

const platformSeries: { platform: string; tone: StatusTone; points: TrendPoint[] }[] = [
  {
    platform: '抖音',
    tone: 'success',
    points: [
      { label: '01', views: 82000, comments: 14000, shares: 7000 },
      { label: '02', views: 94000, comments: 16000, shares: 8000 },
      { label: '03', views: 108000, comments: 19000, shares: 10000 },
      { label: '04', views: 132000, comments: 26000, shares: 13000 },
      { label: '05', views: 168000, comments: 33000, shares: 16000 },
    ],
  },
  {
    platform: '公众号',
    tone: 'info',
    points: [
      { label: '01', views: 68000, comments: 11000, shares: 5000 },
      { label: '02', views: 72000, comments: 12000, shares: 6000 },
      { label: '03', views: 70000, comments: 12000, shares: 5000 },
      { label: '04', views: 78000, comments: 14000, shares: 7000 },
      { label: '05', views: 84000, comments: 15000, shares: 8000 },
    ],
  },
  {
    platform: '微博',
    tone: 'destructive',
    points: [
      { label: '01', views: 74000, comments: 10000, shares: 6000 },
      { label: '02', views: 67000, comments: 9000, shares: 5000 },
      { label: '03', views: 59000, comments: 8000, shares: 4000 },
      { label: '04', views: 45000, comments: 6000, shares: 3000 },
      { label: '05', views: 39000, comments: 5000, shares: 2000 },
    ],
  },
];

const breakoutContent = [
  { title: '供应链幕后', platform: '抖音', signal: '2.8x', stat: '18.6万 播放', href: '/plans/daily-hotspot' },
  { title: '品牌增长拆解', platform: '公众号', signal: '14.2% 打开率', stat: '6.3万 阅读', href: '/plans/brand-case' },
];

const throttledContent = [
  { title: '周末热点追踪', platform: '微博', signal: '-42%', reason: '话题匹配弱', href: '/plans/weekly-tech' },
  { title: '新品测评卡点', platform: '抖音', signal: '完播率偏低', reason: '前 3 秒留存不足', href: '/plans/daily-hotspot' },
];

const businessRows = [
  { name: '青岚科技春季合作', owner: '商务 / 林乔', status: '待报价审核', statusTone: 'warning' as StatusTone, nextStep: '财务确认报价区间', href: '/business/deal-qinglan' },
  { name: '云屿品牌发布专项', owner: '管理员 / 周岚', status: '待管理确认', statusTone: 'info' as StatusTone, nextStep: '确认排期与交付边界', href: '/business/deal-yunyu' },
  { name: '洛川汽车区域投放', owner: '运营 / 许舟', status: '待制作 / 发布', statusTone: 'default' as StatusTone, nextStep: '补齐短视频封面与发布时间', href: '/business/deal-luochuan' },
];

const planRows = [
  { name: '今日热点窗口', cycle: '今日 14:00', score: '8.7 / 10', direction: '抖音故事化、公众号深度拆解', href: '/plans/daily-hotspot' },
  { name: '周度科技观察', cycle: '本周', score: '7.9 / 10', direction: '微博快评、头条延展报道', href: '/plans/weekly-tech' },
  { name: '品牌案例拆解', cycle: '本周', score: '7.5 / 10', direction: '公众号长文、小红书图文复盘', href: '/plans/brand-case' },
];

const todoRows = [
  { title: '抖音账号授权即将失效', owner: '运营 / 发布', urgency: '高优先', tone: 'destructive' as StatusTone, href: '/connections/douyin' },
  { title: '3 条商务单等待财务复核报价', owner: '财务', urgency: '今日处理', tone: 'warning' as StatusTone, href: '/business?status=pricing-review' },
  { title: '本周内容计划缺少微博方向说明', owner: '策划', urgency: '需补全', tone: 'warning' as StatusTone, href: '/plans/weekly-tech' },
];

const managementOverview = [
  { label: '进行中商务合作', value: 18, unit: '个' },
  { label: '本周内容计划', value: 12, unit: '份' },
  { label: '同步异常账号', value: 2, unit: '个' },
];

const changeRate = (points: TrendPoint[]) => {
  if (points.length < 2) return '暂无对比';
  const first = points[0].views;
  const last = points[points.length - 1].views;
  if (first === 0) return '暂无对比';
  const rate = ((last - first) / first) * 100;
  return `${rate > 0 ? '+' : ''}${rate.toFixed(0)}%`;
};

const HomeView = () => {
  const session = useAuthStore((state) => state.session);
  const roles = session?.Roles ?? [];
  const roleKey = roles.join(',');
  const roleLabels = getRoleLabels(roles);
  const isManagement = roles.includes('admin');

  const [activeTab, setActiveTab] = useState<WorkspaceTab>('business');
  const [isTabPinned, setIsTabPinned] = useState(false);

  useEffect(() => {
    if (isTabPinned) return;
    setActiveTab(getDefaultTab(roleKey ? roleKey.split(',') : []));
  }, [roleKey, isTabPinned]);

  return (
    <PageShell>
      <section className="overflow-hidden rounded-3xl bg-primary text-primary-foreground">
        <div className="grid lg:grid-cols-[1.15fr_0.85fr]">
          <div className="relative flex flex-col justify-between gap-8 overflow-hidden px-6 py-8 sm:px-8 lg:px-10 lg:py-10">
            <div className="pointer-events-none absolute -left-12 top-6 size-56 rounded-full bg-primary-foreground/5 blur-3xl" />
            <div className="pointer-events-none absolute right-16 top-10 size-40 rounded-full border border-primary-foreground/10" />

            <div className="relative space-y-4">
              <div className="flex items-center gap-3 text-[11px] font-medium uppercase tracking-[0.24em] text-primary-foreground/55">
                <span>MediaAI</span>
                <span className="size-1 rounded-full bg-primary-foreground/30" />
                <span>Operations Cockpit</span>
              </div>
              <h1 className="max-w-2xl text-4xl font-semibold leading-[1.08] tracking-tight sm:text-5xl lg:text-6xl">
                Dashboard
              </h1>
              <p className="max-w-xl text-sm leading-7 text-primary-foreground/65">
                先看平台态势，再判断爆款机会与限流风险。
              </p>
            </div>

            <div className="relative flex flex-wrap items-center gap-2">
              {roleLabels.map((role) => (
                <Badge
                  key={role}
                  variant="outline"
                  className="h-6 rounded-full border-primary-foreground/20 bg-primary-foreground/10 px-3 text-[10px] font-medium uppercase tracking-[0.16em] text-primary-foreground/75"
                >
                  {role}
                </Badge>
              ))}
              {isManagement ? (
                <Badge className="h-6 rounded-full bg-primary-foreground px-3 text-[10px] tracking-[0.16em] text-primary">
                  管理视图
                </Badge>
              ) : null}
            </div>
          </div>

          <div className="grid gap-px border-t border-primary-foreground/10 bg-primary-foreground/10 p-px sm:grid-cols-2 lg:border-l lg:border-t-0">
            {metricCards.map((item) => (
              <div key={item.label} className="flex min-h-[148px] flex-col justify-between gap-5 bg-primary px-6 py-6">
                <div className="flex items-center justify-between gap-3">
                  <p className="text-sm text-primary-foreground/65">{item.label}</p>
                  <StatusPill appearance="inverse">{item.detail}</StatusPill>
                </div>
                <div className="flex items-baseline gap-1.5">
                  <span
                    className={`font-semibold leading-none tracking-tight tabular-nums ${
                      item.isTextValue ? 'text-4xl' : 'text-5xl'
                    }`}
                  >
                    {item.value}
                  </span>
                  {item.unit ? <span className="text-sm text-primary-foreground/60">{item.unit}</span> : null}
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      <Panel>
        <PanelHeader
          eyebrow="平台态势"
          title="最近 5 条更新"
          description="三条指标各自按自身峰值归一化，用于比较走势形状；具体数值见指标卡。"
          actions={<TrendLegend />}
        />
        <PanelBody className="grid gap-4 lg:grid-cols-3">
          {platformSeries.map((item) => {
            const latest = item.points[item.points.length - 1];
            const rate = changeRate(item.points);

            return (
              <article key={item.platform} className="rounded-2xl border border-border/60 bg-background/60 p-5">
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <p className="text-base font-semibold tracking-tight">{item.platform}</p>
                    <p className="mt-1 text-xs text-muted-foreground">最近 {item.points.length} 条更新</p>
                  </div>
                  <StatusPill tone={item.tone}>{rate}</StatusPill>
                </div>

                <TrendChart className="mt-4" points={item.points} platform={item.platform} />

                <dl className="mt-4 grid grid-cols-3 gap-2">
                  <MetricTile label="浏览" value={formatCompactNumber(latest.views)} />
                  <MetricTile label="评论" value={formatCompactNumber(latest.comments)} />
                  <MetricTile label="转发" value={formatCompactNumber(latest.shares)} />
                </dl>
              </article>
            );
          })}
        </PanelBody>
      </Panel>

      <div className="grid gap-4 xl:grid-cols-2">
        <Panel>
          <PanelHeader
            eyebrow="机会"
            title="爆款机会"
            icon={Flame}
            actions={<StatusPill tone="success">可放大</StatusPill>}
          />
          <PanelBody className="space-y-3">
            {breakoutContent.map((item) => (
              <SignalRow key={item.title} {...item} />
            ))}
          </PanelBody>
        </Panel>

        <Panel>
          <PanelHeader
            eyebrow="风险"
            title="限流 / 掉速风险"
            icon={Siren}
            actions={<StatusPill tone="destructive">需排查</StatusPill>}
          />
          <PanelBody className="space-y-3">
            {throttledContent.map((item) => (
              <SignalRow key={item.title} {...item} />
            ))}
          </PanelBody>
        </Panel>
      </div>

      {isManagement ? (
        <Panel>
          <PanelHeader eyebrow="管理层概览" title="跨业务总量" />
          <PanelBody className="grid gap-3 sm:grid-cols-3">
            {managementOverview.map((item) => (
              <div key={item.label} className="rounded-2xl border border-border/60 bg-background/60 px-5 py-4">
                <p className="text-sm text-muted-foreground">{item.label}</p>
                <div className="mt-3 flex items-baseline gap-1.5">
                  <span className="text-4xl font-semibold leading-none tracking-tight tabular-nums">{item.value}</span>
                  <span className="text-xs text-muted-foreground">{item.unit}</span>
                </div>
              </div>
            ))}
          </PanelBody>
        </Panel>
      ) : null}

      <section className="space-y-4">
        <SectionHeader
          /* 25px = 卡片 1px 边框 + 24px 内边距，让标题与 Tab 同卡片内文字（表头/单元格）严格对齐 */
          className="px-[25px]"
          eyebrow="业务承接"
          title="执行列表"
          description="按角色汇总需要推进的事项与下一步动作。"
          actions={
            <SegmentedTabs
              label="执行列表分类"
              value={activeTab}
              options={workspaceTabs}
              onChange={(next) => {
                setIsTabPinned(true);
                setActiveTab(next);
              }}
            />
          }
        />

        {activeTab === 'business' ? (
          <Panel>
            <PanelHeader
              eyebrow="商务合作"
              title="当前执行"
              actions={
                <Button variant="outline" size="sm" asChild>
                  <Link to="/business">查看全部</Link>
                </Button>
              }
            />
            <Table>
              <TableHeader>
                <TableRow className="border-border/60 hover:bg-transparent">
                  <TableHead className="px-6">事项</TableHead>
                  <TableHead>负责人</TableHead>
                  <TableHead>状态</TableHead>
                  <TableHead>下一步</TableHead>
                  <TableHead className="px-6 text-right">详情</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {businessRows.map((row) => (
                  <TableRow key={row.name} className="border-border/50">
                    <TableCell className="px-6 py-4 font-medium whitespace-normal">{row.name}</TableCell>
                    <TableCell className="py-4 text-muted-foreground">{row.owner}</TableCell>
                    <TableCell className="py-4">
                      <StatusPill tone={row.statusTone}>{row.status}</StatusPill>
                    </TableCell>
                    <TableCell className="py-4 whitespace-normal text-muted-foreground">{row.nextStep}</TableCell>
                    <TableCell className="px-6 py-4 text-right">
                      {/* -mr-2.5 抵消 Button 的 px-2.5，让“进入详情”与表头“详情”右边缘严格对齐 */}
                      <Button variant="ghost" size="sm" asChild className="-mr-[11px]">
                        <Link to={row.href}>
                          进入详情
                          <ArrowRight className="size-4" />
                        </Link>
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Panel>
        ) : null}

        {activeTab === 'plans' ? (
          <div className="grid gap-4 xl:grid-cols-[1.35fr_0.65fr]">
            <Panel>
              <PanelHeader eyebrow="内容计划" title="当前执行" />
              <Table>
                <TableHeader>
                  <TableRow className="border-border/60 hover:bg-transparent">
                    <TableHead className="px-6">计划</TableHead>
                    <TableHead>周期</TableHead>
                    <TableHead>评分</TableHead>
                    <TableHead>方向</TableHead>
                    <TableHead className="px-6 text-right">详情</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {planRows.map((row) => (
                    <TableRow key={row.name} className="border-border/50">
                      <TableCell className="px-6 py-4 font-medium whitespace-normal">{row.name}</TableCell>
                      <TableCell className="py-4 text-muted-foreground">{row.cycle}</TableCell>
                      <TableCell className="py-4 tabular-nums">{row.score}</TableCell>
                      <TableCell className="py-4 whitespace-normal text-muted-foreground">{row.direction}</TableCell>
                      <TableCell className="px-6 py-4 text-right">
                        {/* 同上：负外边距抵消按钮左右内边距，保证列内右对齐 */}
                        <Button variant="ghost" size="sm" asChild className="-mr-[11px]">
                          <Link to={row.href}>
                            进入详情
                            <ArrowRight className="size-4" />
                          </Link>
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </Panel>

            <Panel>
              <PanelHeader eyebrow="计划提醒" title="待处理提示" icon={CalendarRange} />
              <PanelBody className="space-y-3">
                <CompactAlert icon={Sparkles} title="微博方向待补充" />
                <CompactAlert icon={CalendarRange} title="热点计划 16:00 前定稿" />
                <CompactAlert icon={Activity} title="竞品观察已更新" />
              </PanelBody>
            </Panel>
          </div>
        ) : null}

        {activeTab === 'todo' ? (
          <Panel>
            <PanelHeader eyebrow="跨角色待办" title="当前执行" />
            <PanelBody className="space-y-3">
              {todoRows.map((row) => (
                <Link
                  key={row.title}
                  to={row.href}
                  className="block rounded-2xl border border-border/60 bg-background/60 px-5 py-4 transition-colors hover:bg-muted/60 focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none"
                >
                  <div className="flex items-start justify-between gap-4">
                    <div className="space-y-1">
                      <p className="font-medium leading-6">{row.title}</p>
                      <p className="text-sm text-muted-foreground">{row.owner}</p>
                    </div>
                    <StatusPill tone={row.tone}>{row.urgency}</StatusPill>
                  </div>
                </Link>
              ))}
            </PanelBody>
          </Panel>
        ) : null}
      </section>
    </PageShell>
  );
};

const SignalRow = ({
  title,
  platform,
  signal,
  stat,
  reason,
  href,
}: {
  title: string;
  platform: string;
  signal: string;
  stat?: string;
  reason?: string;
  href: string;
}) => (
  <Link
    to={href}
    className="block rounded-2xl border border-border/60 bg-background/60 px-5 py-4 transition-colors hover:bg-muted/60 focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none"
  >
    <div className="flex items-start justify-between gap-4">
      <div className="space-y-1.5">
        <div className="flex flex-wrap items-center gap-2">
          <p className="font-medium tracking-tight">{title}</p>
          <StatusPill>{platform}</StatusPill>
        </div>
        <div className="flex flex-wrap gap-x-4 gap-y-1 text-sm text-muted-foreground">
          <span>{signal}</span>
          <span>{stat ?? reason}</span>
        </div>
      </div>
      <ArrowRight className="mt-1 size-4 shrink-0 text-muted-foreground" />
    </div>
  </Link>
);

const CompactAlert = ({ icon: Icon, title }: { icon: LucideIcon; title: string }) => (
  <div className="flex items-center gap-3 rounded-2xl border border-border/60 bg-background/60 px-5 py-4">
    <Icon className="size-4 text-muted-foreground" />
    <span className="text-sm font-medium">{title}</span>
  </div>
);

const MetricTile = ({ label, value }: { label: string; value: string }) => (
  <div className="rounded-xl border border-border/60 bg-card/70 px-3 py-2.5 text-center">
    <dt className="text-[11px] uppercase tracking-[0.12em] text-muted-foreground">{label}</dt>
    <dd className="mt-1 text-lg font-semibold leading-none tracking-tight tabular-nums">{value}</dd>
  </div>
);

const getRoleLabels = (roles: string[]) => {
  if (roles.length === 0) return ['访客'];
  return roles.map((role) => {
    if (role === 'admin') return '管理员';
    if (role === 'finance') return '财务';
    if (role === 'planner') return '策划';
    if (role === 'operations') return '运营';
    if (role === 'editor') return '剪辑';
    if (role === 'analyst') return '分析';
    if (role === 'sales') return '商务';
    return role;
  });
};

/** 按角色优先级决定默认执行列表；与后端角色清单保持一致，未知角色回落到商务合作。 */
const getDefaultTab = (roles: string[]): WorkspaceTab => {
  const priority: WorkspaceTab[] = ['business', 'plans', 'todo'];
  const roleToTab: Record<string, WorkspaceTab> = {
    finance: 'todo',
    planner: 'plans',
    operations: 'plans',
    editor: 'plans',
    analyst: 'plans',
    sales: 'business',
    admin: 'business',
  };

  for (const tab of priority) {
    if (roles.some((role) => roleToTab[role] === tab)) return tab;
  }

  return 'business';
};

export default HomeView;
