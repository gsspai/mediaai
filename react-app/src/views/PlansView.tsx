import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { ArrowLeft, ArrowRight, Pencil, Plus, RefreshCw } from 'lucide-react';
import PageShell from '@/components/dashboard/PageShell';
import { Panel, PanelHeader } from '@/components/dashboard/Panel';
import SegmentedTabs from '@/components/dashboard/SegmentedTabs';
import SectionHeader from '@/components/dashboard/SectionHeader';
import StatusPill, { type StatusTone } from '@/components/dashboard/StatusPill';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import ContentPlanManager from '@/api/ContentPlanManager';
import DealManager from '@/api/DealManager';
import type { ContentPlanDto, DealDto, SavePlanRequestDto } from '@/api/AppDtos';
import useAuthStore from '@/auth/AuthStore';
import { resolveErrorText } from '@/utils/errors';
import { PLAN_STATUS_LABELS, PLAN_STATUS_OPTIONS, formatPlatforms } from '@/utils/execution';
import { canEditPlans } from '@/utils/permissions';
import { PLATFORM_LABELS } from '@/utils/platforms';

type LoadStatus = 'loading' | 'ready' | 'error';

type PlanFormState = {
  Id: string | null;
  Title: string;
  Platforms: string[];
  PlannedAt: string;
  Direction: string;
  Score: string;
  AssigneeName: string;
  DealId: string;
  Link: string;
};

const emptyForm = (): PlanFormState => ({
  Id: null,
  Title: '',
  Platforms: [],
  PlannedAt: '',
  Direction: '',
  Score: '',
  AssigneeName: '',
  DealId: 'none',
  Link: '',
});

const statusFilters = [
  { value: 'all', label: '全部' },
  { value: 'scheduled', label: PLAN_STATUS_OPTIONS[2].label },
  { value: 'scoring', label: PLAN_STATUS_OPTIONS[1].label },
  { value: 'draft', label: PLAN_STATUS_OPTIONS[0].label },
  { value: 'published', label: PLAN_STATUS_OPTIONS[3].label },
] as const;

const PlansView = () => {
  const roles = useAuthStore((state) => state.session?.Roles ?? []);
  const canEdit = canEditPlans(roles);

  const [status, setStatus] = useState<LoadStatus>('loading');
  const [plans, setPlans] = useState<ContentPlanDto[]>([]);
  const [total, setTotal] = useState(0);
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [keywordInput, setKeywordInput] = useState('');
  const [keyword, setKeyword] = useState('');
  const [reloadKey, setReloadKey] = useState(0);
  const [deals, setDeals] = useState<DealDto[]>([]);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [form, setForm] = useState<PlanFormState>(emptyForm);
  const [formError, setFormError] = useState('');
  const [saving, setSaving] = useState(false);
  const [notice, setNotice] = useState<{ tone: 'success' | 'error'; text: string } | null>(null);

  const load = useCallback(() => {
    setStatus('loading');
    let cancelled = false;

    ContentPlanManager.ListPlans({
      Status: statusFilter === 'all' ? undefined : statusFilter,
      Keyword: keyword.trim() || undefined,
      Take: 100,
    })
      .then((data) => {
        if (cancelled) return;
        setPlans(data.Items ?? []);
        setTotal(data.Total ?? 0);
        setStatus('ready');
      })
      .catch(() => {
        if (cancelled) return;
        setStatus('error');
      });

    return () => {
      cancelled = true;
    };
  }, [statusFilter, keyword]);

  // 输入停顿 300ms 再查，避免每敲一个字母打一次接口。
  useEffect(() => {
    const timer = window.setTimeout(() => setKeyword(keywordInput.trim()), 300);
    return () => window.clearTimeout(timer);
  }, [keywordInput]);

  useEffect(() => load(), [load, reloadKey]);

  // 关联商务单的下拉：只取未结算 / 未丢失的，避免把计划挂到历史单上。
  useEffect(() => {
    let cancelled = false;
    DealManager.ListDeals({ Take: 100 })
      .then((data) => {
        if (cancelled) return;
        setDeals((data.Items ?? []).filter((deal) => deal.Stage !== 'settled' && deal.Stage !== 'lost'));
      })
      .catch(() => {
        if (cancelled) return;
        setDeals([]);
      });

    return () => {
      cancelled = true;
    };
  }, [reloadKey]);

  const openCreate = () => {
    setForm(emptyForm());
    setFormError('');
    setDialogOpen(true);
  };

  const openEdit = (plan: ContentPlanDto) => {
    setForm({
      Id: plan.Id,
      Title: plan.Title,
      Platforms: [...plan.Platforms],
      PlannedAt: plan.PlannedAt ?? '',
      Direction: plan.Direction,
      Score: plan.Score === null ? '' : String(plan.Score),
      AssigneeName: plan.AssigneeName,
      DealId: plan.DealId ?? 'none',
      Link: plan.Link,
    });
    setFormError('');
    setDialogOpen(true);
  };

  const togglePlatform = (platform: string) => {
    setForm((prev) => ({
      ...prev,
      Platforms: prev.Platforms.includes(platform)
        ? prev.Platforms.filter((item) => item !== platform)
        : [...prev.Platforms, platform],
    }));
  };

  const submit = async () => {
    if (form.Title.trim().length === 0) {
      setFormError('计划名不能为空。');
      return;
    }

    let score: number | null = null;
    if (form.Score.trim().length > 0) {
      const parsed = Number(form.Score.trim());
      if (Number.isNaN(parsed) || parsed < 0 || parsed > 10) {
        setFormError('机会分应在 0 到 10 之间，留空表示未评分。');
        return;
      }

      score = parsed;
    }

    const request: SavePlanRequestDto = {
      Id: form.Id,
      Title: form.Title.trim(),
      Platforms: form.Platforms,
      PlannedAt: form.PlannedAt || null,
      Direction: form.Direction.trim(),
      Score: score,
      AssigneeName: form.AssigneeName.trim(),
      DealId: form.DealId === 'none' ? null : form.DealId,
      Link: form.Link.trim(),
    };

    setSaving(true);
    setFormError('');
    try {
      const result = await ContentPlanManager.SavePlan(request);
      if (!result.Success) {
        setFormError(result.Message || '保存失败。');
        return;
      }

      setDialogOpen(false);
      setNotice({ tone: 'success', text: result.Message });
      setReloadKey((value) => value + 1);
    } catch (error) {
      setFormError(resolveErrorText(error, '保存失败，后端接口不可用。'));
    } finally {
      setSaving(false);
    }
  };

  const emptyHint = useMemo(() => {
    if (status === 'loading') return '正在加载内容计划…';
    if (status === 'error') return '内容计划没有加载成功，稍后重试或检查后端接口。';
    return '没有符合条件的内容计划。';
  }, [status]);

  return (
    <PageShell>
      <SectionHeader
        eyebrow="内容计划"
        title="计划与排期"
        description="选题 → 评分 → 待制作 → 已发布；方向为空时会自动生成一条策划待办。"
        actions={
          <div className="flex flex-wrap items-center gap-2">
            <Button variant="outline" size="sm" asChild>
              <Link to="/">
                <ArrowLeft className="size-4" />
                返回工作台
              </Link>
            </Button>
            <Button variant="outline" size="sm" onClick={() => setReloadKey((value) => value + 1)}>
              <RefreshCw className="size-4" />
              刷新
            </Button>
            {canEdit ? (
              <Button size="sm" onClick={openCreate}>
                <Plus className="size-4" />
                新增内容计划
              </Button>
            ) : null}
          </div>
        }
      />

      {!canEdit ? (
        <div className="rounded-2xl border border-border/60 bg-muted/40 px-4 py-3 text-sm leading-6 text-muted-foreground">
          当前账号只能查看内容计划。新建与编辑需要「管理员 / 策划 / 运营 / 剪辑」角色。
        </div>
      ) : null}

      {notice ? (
        <div className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-border/60 bg-muted/40 px-4 py-3 text-sm leading-6">
          <span>{notice.text}</span>
          <button type="button" className="text-xs underline underline-offset-4" onClick={() => setNotice(null)}>
            关闭
          </button>
        </div>
      ) : null}

      <Panel>
        <PanelHeader
          eyebrow="列表"
          title={statusFilter === 'all' ? '全部内容计划' : PLAN_STATUS_LABELS[statusFilter] ?? '内容计划'}
          description={`库里共 ${total} 条计划，当前筛选显示 ${plans.length} 条。`}
          actions={
            <div className="flex flex-wrap items-center gap-2">
              <Input
                value={keywordInput}
                onChange={(event) => setKeywordInput(event.target.value)}
                placeholder="搜计划名 / 方向 / 责任人"
                className="h-9 w-56"
              />
              <SegmentedTabs
                label="按状态筛选"
                value={statusFilter}
                options={statusFilters.map((item) => ({ value: item.value, label: item.label }))}
                onChange={(next) => setStatusFilter(next)}
              />
            </div>
          }
        />
        <Table>
          <TableHeader>
            <TableRow className="border-border/60 hover:bg-transparent">
              <TableHead className="px-6">计划</TableHead>
              <TableHead>状态</TableHead>
              <TableHead>排期</TableHead>
              <TableHead>评分</TableHead>
              <TableHead>平台</TableHead>
              <TableHead>责任人</TableHead>
              <TableHead className="px-6 text-right">操作</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {plans.length > 0 ? (
              plans.map((plan) => (
                <TableRow key={plan.Id} className="border-border/50">
                  <TableCell className="px-6 py-4 font-medium whitespace-normal">
                    <div className="flex flex-wrap items-center gap-2">
                      <Link to={`/plans/${plan.Id}`} className="underline-offset-4 hover:underline">
                        {plan.Title}
                      </Link>
                      {plan.IsSample ? <StatusPill tone="default">示例</StatusPill> : null}
                    </div>
                    <p className="mt-1 text-xs text-muted-foreground">{plan.Direction || '缺少方向说明'}</p>
                  </TableCell>
                  <TableCell className="py-4">
                    <StatusPill tone={(plan.Tone as StatusTone) ?? 'default'}>{plan.StatusLabel}</StatusPill>
                  </TableCell>
                  <TableCell className="py-4 text-muted-foreground">{plan.CycleDisplay}</TableCell>
                  <TableCell className="py-4 tabular-nums text-muted-foreground">{plan.ScoreDisplay}</TableCell>
                  <TableCell className="py-4 whitespace-normal text-muted-foreground">
                    {formatPlatforms(plan.Platforms)}
                  </TableCell>
                  <TableCell className="py-4 text-muted-foreground">{plan.AssigneeName || '未指定'}</TableCell>
                  <TableCell className="px-6 py-4">
                    <div className="flex flex-wrap items-center justify-end gap-2">
                      {canEdit ? (
                        <Button variant="ghost" size="sm" onClick={() => openEdit(plan)}>
                          <Pencil className="size-4" />
                          编辑
                        </Button>
                      ) : null}
                      <Button variant="ghost" size="sm" asChild>
                        <Link to={`/plans/${plan.Id}`}>
                          详情
                          <ArrowRight className="size-4" />
                        </Link>
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))
            ) : (
              <TableRow className="border-border/50 hover:bg-transparent">
                <TableCell colSpan={7} className="px-6 py-6 text-sm leading-6 text-muted-foreground">
                  {emptyHint}
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </Panel>

      <Dialog open={dialogOpen} onOpenChange={(next) => (saving ? undefined : setDialogOpen(next))}>
        <DialogContent className="sm:max-w-xl">
          <DialogHeader>
            <DialogTitle>{form.Id ? '编辑内容计划' : '新增内容计划'}</DialogTitle>
            <DialogDescription>计划名必填；排期与机会分留空表示尚未确定。</DialogDescription>
          </DialogHeader>

          <div className="grid gap-4">
            <div className="space-y-2">
              <Label htmlFor="plan-title">计划名</Label>
              <Input
                id="plan-title"
                value={form.Title}
                onChange={(event) => setForm((prev) => ({ ...prev, Title: event.target.value }))}
                placeholder="例如 今日热点窗口"
              />
            </div>

            <div className="space-y-2">
              <Label>覆盖平台</Label>
              <div className="flex flex-wrap gap-2">
                {Object.entries(PLATFORM_LABELS).map(([value, label]) => {
                  const active = form.Platforms.includes(value);
                  return (
                    <button
                      key={value}
                      type="button"
                      onClick={() => togglePlatform(value)}
                      className={`rounded-full border px-3 py-1.5 text-xs transition-colors ${
                        active
                          ? 'border-foreground/30 bg-foreground text-background'
                          : 'border-border/60 bg-background text-muted-foreground hover:bg-muted'
                      }`}
                    >
                      {label}
                    </button>
                  );
                })}
              </div>
            </div>

            <div className="grid gap-2 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="plan-date">排期</Label>
                <Input
                  id="plan-date"
                  type="date"
                  value={form.PlannedAt}
                  onChange={(event) => setForm((prev) => ({ ...prev, PlannedAt: event.target.value }))}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="plan-score">机会分（0-10）</Label>
                <Input
                  id="plan-score"
                  inputMode="decimal"
                  value={form.Score}
                  onChange={(event) => setForm((prev) => ({ ...prev, Score: event.target.value }))}
                  placeholder="留空表示未评分"
                />
              </div>
            </div>

            <div className="grid gap-2 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="plan-assignee">责任人</Label>
                <Input
                  id="plan-assignee"
                  value={form.AssigneeName}
                  onChange={(event) => setForm((prev) => ({ ...prev, AssigneeName: event.target.value }))}
                  placeholder="留空则用当前账号"
                />
              </div>
              <div className="space-y-2">
                <Label>关联商务单</Label>
                <Select
                  value={form.DealId}
                  onValueChange={(next) => setForm((prev) => ({ ...prev, DealId: next }))}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="不关联" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">不关联</SelectItem>
                    {deals.map((deal) => (
                      <SelectItem key={deal.Id} value={deal.Id}>
                        {deal.Title}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="plan-direction">内容方向</Label>
              <Textarea
                id="plan-direction"
                value={form.Direction}
                onChange={(event) => setForm((prev) => ({ ...prev, Direction: event.target.value }))}
                placeholder="例如 抖音故事化、公众号深度拆解"
                rows={3}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="plan-link">成品链接</Label>
              <Input
                id="plan-link"
                value={form.Link}
                onChange={(event) => setForm((prev) => ({ ...prev, Link: event.target.value }))}
                placeholder="发布后回填"
              />
            </div>

            {formError ? <p className="text-sm leading-6 text-destructive">{formError}</p> : null}
          </div>

          <DialogFooter>
            <Button variant="outline" disabled={saving} onClick={() => setDialogOpen(false)}>
              取消
            </Button>
            <Button disabled={saving} onClick={submit}>
              {saving ? '保存中…' : '保存'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </PageShell>
  );
};

export default PlansView;
