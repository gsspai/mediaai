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
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import DealManager from '@/api/DealManager';
import type { DealDto, SaveDealRequestDto } from '@/api/AppDtos';
import useAuthStore from '@/auth/AuthStore';
import { resolveErrorText } from '@/utils/errors';
import { DEAL_STAGE_LABELS, DEAL_STAGE_OPTIONS, formatShortDate } from '@/utils/execution';
import { canAdvanceDeal, canEditDeals, isFinanceOnly } from '@/utils/permissions';

type LoadStatus = 'loading' | 'ready' | 'error';

type DealFormState = {
  Id: string | null;
  ClientName: string;
  Title: string;
  Contact: string;
  Amount: string;
  ExpectedCloseAt: string;
  OwnerName: string;
  Notes: string;
};

const emptyForm = (): DealFormState => ({
  Id: null,
  ClientName: '',
  Title: '',
  Contact: '',
  Amount: '',
  ExpectedCloseAt: '',
  OwnerName: '',
  Notes: '',
});

/** 列表筛选只列推进中的阶段；已结算 / 已丢失通过「全部」查看，避免默认视图被历史单占满。 */
const stageFilters = [
  { value: 'all', label: '全部' },
  { value: 'pricing-review', label: DEAL_STAGE_OPTIONS[2].label },
  { value: 'quoting', label: DEAL_STAGE_OPTIONS[1].label },
  { value: 'delivering', label: DEAL_STAGE_OPTIONS[4].label },
  { value: 'settled', label: DEAL_STAGE_OPTIONS[5].label },
] as const;

const BusinessView = () => {
  const roles = useAuthStore((state) => state.session?.Roles ?? []);
  const canEdit = canEditDeals(roles);
  const canAdvance = canAdvanceDeal(roles);
  const financeOnly = isFinanceOnly(roles);

  const [status, setStatus] = useState<LoadStatus>('loading');
  const [deals, setDeals] = useState<DealDto[]>([]);
  const [total, setTotal] = useState(0);
  const [stageFilter, setStageFilter] = useState<string>('all');
  const [keywordInput, setKeywordInput] = useState('');
  const [keyword, setKeyword] = useState('');
  const [reloadKey, setReloadKey] = useState(0);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [form, setForm] = useState<DealFormState>(emptyForm);
  const [formError, setFormError] = useState('');
  const [saving, setSaving] = useState(false);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [notice, setNotice] = useState<{ tone: 'success' | 'error'; text: string } | null>(null);

  const load = useCallback(() => {
    setStatus('loading');
    let cancelled = false;

    DealManager.ListDeals({
      Stage: stageFilter === 'all' ? undefined : stageFilter,
      Keyword: keyword.trim() || undefined,
      Take: 100,
    })
      .then((data) => {
        if (cancelled) return;
        setDeals(data.Items ?? []);
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
  }, [stageFilter, keyword]);

  // 输入停顿 300ms 再查，避免每敲一个字母打一次接口。
  useEffect(() => {
    const timer = window.setTimeout(() => setKeyword(keywordInput.trim()), 300);
    return () => window.clearTimeout(timer);
  }, [keywordInput]);

  useEffect(() => load(), [load, reloadKey]);

  const openCreate = () => {
    setForm(emptyForm());
    setFormError('');
    setDialogOpen(true);
  };

  const openEdit = (deal: DealDto) => {
    setForm({
      Id: deal.Id,
      ClientName: deal.ClientName,
      Title: deal.Title,
      Contact: deal.Contact,
      Amount: deal.Amount > 0 ? String(deal.Amount) : '',
      ExpectedCloseAt: deal.ExpectedCloseAt ?? '',
      OwnerName: deal.OwnerName,
      Notes: deal.Notes,
    });
    setFormError('');
    setDialogOpen(true);
  };

  const submit = async () => {
    if (form.ClientName.trim().length === 0) {
      setFormError('客户名不能为空。');
      return;
    }

    const amount = Number(form.Amount.trim() || '0');
    if (Number.isNaN(amount) || amount < 0) {
      setFormError('报价金额应是不小于 0 的数字。');
      return;
    }

    const request: SaveDealRequestDto = {
      Id: form.Id,
      ClientName: form.ClientName.trim(),
      Title: form.Title.trim(),
      Contact: form.Contact.trim(),
      Amount: amount,
      Currency: 'CNY',
      OwnerName: form.OwnerName.trim(),
      ExpectedCloseAt: form.ExpectedCloseAt || null,
      Notes: form.Notes.trim(),
    };

    setSaving(true);
    setFormError('');
    try {
      const result = await DealManager.SaveDeal(request);
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

  const advance = async (deal: DealDto, target: string) => {
    setBusyId(deal.Id);
    setNotice(null);
    try {
      const result = await DealManager.UpdateDealStage({ DealId: deal.Id, Stage: target });
      if (!result.Success) {
        setNotice({ tone: 'error', text: result.Message || '阶段推进失败。' });
        return;
      }

      setNotice({ tone: 'success', text: result.Message });
      setReloadKey((value) => value + 1);
    } catch (error) {
      setNotice({ tone: 'error', text: resolveErrorText(error, '阶段推进失败，后端接口不可用。') });
    } finally {
      setBusyId(null);
    }
  };

  const emptyHint = useMemo(() => {
    if (status === 'loading') return '正在加载商务单…';
    if (status === 'error') return '商务单没有加载成功，稍后重试或检查后端接口。';
    return '没有符合条件的商务单。';
  }, [status]);

  return (
    <PageShell>
      <SectionHeader
        eyebrow="商务合作"
        title="商务单"
        description="报价 → 复核 → 确认 → 交付 → 结算，每次只推进一格；财务只能做报价复核。"
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
                新增商务单
              </Button>
            ) : null}
          </div>
        }
      />

      {!canEdit ? (
        <div className="rounded-2xl border border-border/60 bg-muted/40 px-4 py-3 text-sm leading-6 text-muted-foreground">
          {financeOnly
            ? '财务账号可以复核「待报价审核」的商务单（通过或退回），新建与编辑请找商务。'
            : '当前账号只能查看商务单。新建与编辑需要「管理员 / 商务」角色。'}
        </div>
      ) : null}

      {notice ? (
        <div
          className={`flex flex-wrap items-center justify-between gap-3 rounded-2xl px-4 py-3 text-sm leading-6 ${
            notice.tone === 'success'
              ? 'border border-border/60 bg-muted/40 text-foreground'
              : 'border border-destructive/30 bg-destructive/5 text-destructive'
          }`}
        >
          <span>{notice.text}</span>
          <button type="button" className="text-xs underline underline-offset-4" onClick={() => setNotice(null)}>
            关闭
          </button>
        </div>
      ) : null}

      <Panel>
        <PanelHeader
          eyebrow="列表"
          title={stageFilter === 'all' ? '全部商务单' : DEAL_STAGE_LABELS[stageFilter] ?? '商务单'}
          description={`库里共 ${total} 条商务单，当前筛选显示 ${deals.length} 条。`}
          actions={
            <div className="flex flex-wrap items-center gap-2">
              <Input
                value={keywordInput}
                onChange={(event) => setKeywordInput(event.target.value)}
                placeholder="搜客户 / 事项 / 对接人"
                className="h-9 w-56"
              />
              <SegmentedTabs
                label="按阶段筛选"
                value={stageFilter}
                options={stageFilters.map((item) => ({ value: item.value, label: item.label }))}
                onChange={(next) => setStageFilter(next)}
              />
            </div>
          }
        />
        <Table>
          <TableHeader>
            <TableRow className="border-border/60 hover:bg-transparent">
              <TableHead className="px-6">事项</TableHead>
              <TableHead>负责人</TableHead>
              <TableHead>报价</TableHead>
              <TableHead>阶段</TableHead>
              <TableHead>下一步</TableHead>
              <TableHead>预计签约</TableHead>
              <TableHead className="px-6 text-right">操作</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {deals.length > 0 ? (
              deals.map((deal) => (
                <TableRow key={deal.Id} className="border-border/50">
                  <TableCell className="px-6 py-4 font-medium whitespace-normal">
                    <div className="flex flex-wrap items-center gap-2">
                      <Link to={`/business/${deal.Id}`} className="underline-offset-4 hover:underline">
                        {deal.Title}
                      </Link>
                      {deal.IsSample ? <StatusPill tone="default">示例</StatusPill> : null}
                    </div>
                    <p className="mt-1 text-xs text-muted-foreground">{deal.ClientName}</p>
                  </TableCell>
                  <TableCell className="py-4 text-muted-foreground">{deal.OwnerName || '未指定'}</TableCell>
                  <TableCell className="py-4 tabular-nums text-muted-foreground">{deal.AmountDisplay}</TableCell>
                  <TableCell className="py-4">
                    <StatusPill tone={(deal.Tone as StatusTone) ?? 'default'}>{deal.StageLabel}</StatusPill>
                  </TableCell>
                  <TableCell className="py-4 whitespace-normal text-muted-foreground">{deal.NextStep}</TableCell>
                  <TableCell className="py-4 text-muted-foreground">{formatShortDate(deal.ExpectedCloseAt)}</TableCell>
                  <TableCell className="px-6 py-4">
                    <div className="flex flex-wrap items-center justify-end gap-2">
                      {canAdvance && deal.Stage === 'pricing-review' && financeOnly ? (
                        <>
                          <Button
                            size="sm"
                            disabled={busyId === deal.Id}
                            onClick={() => advance(deal, 'confirmed')}
                          >
                            通过报价
                          </Button>
                          <Button
                            variant="outline"
                            size="sm"
                            disabled={busyId === deal.Id}
                            onClick={() => advance(deal, 'quoting')}
                          >
                            退回重报
                          </Button>
                        </>
                      ) : null}
                      {canAdvance && !financeOnly && deal.NextStage ? (
                        <Button size="sm" disabled={busyId === deal.Id} onClick={() => advance(deal, deal.NextStage as string)}>
                          推进到{DEAL_STAGE_LABELS[deal.NextStage] ?? deal.NextStage}
                        </Button>
                      ) : null}
                      {canEdit ? (
                        <Button variant="ghost" size="sm" onClick={() => openEdit(deal)}>
                          <Pencil className="size-4" />
                          编辑
                        </Button>
                      ) : null}
                      <Button variant="ghost" size="sm" asChild>
                        <Link to={`/business/${deal.Id}`}>
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
            <DialogTitle>{form.Id ? '编辑商务单' : '新增商务单'}</DialogTitle>
            <DialogDescription>客户名必填；报价留空按「未报价」显示。新建后阶段为「线索」。</DialogDescription>
          </DialogHeader>

          <div className="grid gap-4">
            <div className="grid gap-2 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="deal-client">客户名</Label>
                <Input
                  id="deal-client"
                  value={form.ClientName}
                  onChange={(event) => setForm((prev) => ({ ...prev, ClientName: event.target.value }))}
                  placeholder="例如 青岚科技"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="deal-title">合作事项</Label>
                <Input
                  id="deal-title"
                  value={form.Title}
                  onChange={(event) => setForm((prev) => ({ ...prev, Title: event.target.value }))}
                  placeholder="留空则与客户名相同"
                />
              </div>
            </div>

            <div className="grid gap-2 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="deal-contact">对接人</Label>
                <Input
                  id="deal-contact"
                  value={form.Contact}
                  onChange={(event) => setForm((prev) => ({ ...prev, Contact: event.target.value }))}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="deal-owner">商务负责人</Label>
                <Input
                  id="deal-owner"
                  value={form.OwnerName}
                  onChange={(event) => setForm((prev) => ({ ...prev, OwnerName: event.target.value }))}
                  placeholder="留空则用当前账号"
                />
              </div>
            </div>

            <div className="grid gap-2 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="deal-amount">报价（元）</Label>
                <Input
                  id="deal-amount"
                  inputMode="decimal"
                  value={form.Amount}
                  onChange={(event) => setForm((prev) => ({ ...prev, Amount: event.target.value }))}
                  placeholder="例如 48000"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="deal-close">预计签约日</Label>
                <Input
                  id="deal-close"
                  type="date"
                  value={form.ExpectedCloseAt}
                  onChange={(event) => setForm((prev) => ({ ...prev, ExpectedCloseAt: event.target.value }))}
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="deal-notes">备注</Label>
              <Textarea
                id="deal-notes"
                value={form.Notes}
                onChange={(event) => setForm((prev) => ({ ...prev, Notes: event.target.value }))}
                placeholder="交付范围、素材来源等"
                rows={3}
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

export default BusinessView;
