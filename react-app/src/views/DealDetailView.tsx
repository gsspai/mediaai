import { useCallback, useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ArrowLeft, ArrowRight, RefreshCw } from 'lucide-react';
import PageShell from '@/components/dashboard/PageShell';
import { Panel, PanelBody, PanelHeader } from '@/components/dashboard/Panel';
import StatusPill, { type StatusTone } from '@/components/dashboard/StatusPill';
import StatusTimeline from '@/components/dashboard/StatusTimeline';
import { Button } from '@/components/ui/button';
import DealManager from '@/api/DealManager';
import ContentPlanManager from '@/api/ContentPlanManager';
import type { ContentPlanDto, DealDto } from '@/api/AppDtos';
import useAuthStore from '@/auth/AuthStore';
import { resolveErrorText } from '@/utils/errors';
import { DEAL_STAGE_LABELS, DEAL_STAGE_OPTIONS, formatDueHint, formatShortDate } from '@/utils/execution';
import { canAdvanceDeal, canEditDeals, isFinanceOnly } from '@/utils/permissions';

type LoadStatus = 'loading' | 'ready' | 'error' | 'missing';

const DealDetailView = () => {
  const { dealId } = useParams<{ dealId: string }>();
  const roles = useAuthStore((state) => state.session?.Roles ?? []);
  const canAdvance = canAdvanceDeal(roles);
  const canEdit = canEditDeals(roles);
  const financeOnly = isFinanceOnly(roles);

  const [status, setStatus] = useState<LoadStatus>('loading');
  const [deal, setDeal] = useState<DealDto | null>(null);
  const [plans, setPlans] = useState<ContentPlanDto[]>([]);
  const [reloadKey, setReloadKey] = useState(0);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ tone: 'success' | 'error'; text: string } | null>(null);

  const load = useCallback(() => {
    if (!dealId) {
      setStatus('missing');
      return;
    }

    setStatus('loading');
    let cancelled = false;

    Promise.all([
      DealManager.GetDeal({ DealId: dealId }),
      // 关联计划在服务端没有按 DealId 筛选的入参，先取一页再在前端过滤，量级在百条以内。
      ContentPlanManager.ListPlans({ Take: 200 }),
    ])
      .then(([dealResult, planResult]) => {
        if (cancelled) return;
        if (!dealResult) {
          setStatus('missing');
          return;
        }

        setDeal(dealResult);
        setPlans((planResult.Items ?? []).filter((plan) => plan.DealId === dealResult.Id));
        setStatus('ready');
      })
      .catch(() => {
        if (cancelled) return;
        setStatus('error');
      });

    return () => {
      cancelled = true;
    };
  }, [dealId]);

  useEffect(() => load(), [load, reloadKey]);

  const advance = async (target: string) => {
    if (!deal) return;
    setBusy(true);
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
      setBusy(false);
    }
  };

  if (status === 'loading') {
    return (
      <PageShell>
        <Panel>
          <PanelBody className="py-10 text-sm text-muted-foreground">正在加载商务单…</PanelBody>
        </Panel>
      </PageShell>
    );
  }

  if (status !== 'ready' || !deal) {
    return (
      <PageShell>
        <Panel>
          <PanelHeader eyebrow="商务合作" title="详情不可用" actions={<StatusPill tone="warning">未找到</StatusPill>} />
          <PanelBody className="space-y-4">
            <p className="text-sm leading-7 text-muted-foreground">
              {status === 'missing'
                ? '这张商务单不存在，或者链接里的 ID 不对。'
                : '商务单没有加载成功，稍后重试或检查后端接口。'}
            </p>
            <div className="flex flex-wrap gap-3">
              <Button variant="outline" asChild>
                <Link to="/business">
                  <ArrowLeft className="size-4" />
                  返回商务单列表
                </Link>
              </Button>
              <Button variant="ghost" onClick={() => setReloadKey((value) => value + 1)}>
                <RefreshCw className="size-4" />
                重试
              </Button>
            </div>
          </PanelBody>
        </Panel>
      </PageShell>
    );
  }

  const showFinanceReview = canAdvance && financeOnly && deal.Stage === 'pricing-review';
  const showAdvance = canAdvance && !financeOnly && deal.NextStage;

  return (
    <PageShell>
      <Panel>
        <PanelHeader
          eyebrow="商务合作 · 详情"
          title={deal.Title}
          description={`${deal.ClientName} · 负责人 ${deal.OwnerName || '未指定'} · 更新于 ${deal.UpdatedAt}`}
          actions={
            <div className="flex flex-wrap items-center gap-2">
              <StatusPill tone={(deal.Tone as StatusTone) ?? 'default'}>{deal.StageLabel}</StatusPill>
              {deal.IsSample ? <StatusPill tone="default">示例数据</StatusPill> : null}
            </div>
          }
        />
        <PanelBody className="space-y-6">
          <StatusTimeline
            steps={DEAL_STAGE_OPTIONS.filter((option) => option.value !== 'lost').map((option) => ({
              value: option.value,
              label: option.label,
            }))}
            current={deal.Stage}
            terminatedLabel={deal.Stage === 'lost' ? '已丢失' : undefined}
            terminatedTone="destructive"
          />

          {notice ? (
            <div
              className={`rounded-2xl px-4 py-3 text-sm leading-6 ${
                notice.tone === 'success'
                  ? 'border border-border/60 bg-muted/40 text-foreground'
                  : 'border border-destructive/30 bg-destructive/5 text-destructive'
              }`}
            >
              {notice.text}
            </div>
          ) : null}

          <div className="flex flex-wrap items-center gap-3">
            {showFinanceReview ? (
              <>
                <Button disabled={busy} onClick={() => advance('confirmed')}>
                  通过报价
                </Button>
                <Button variant="outline" disabled={busy} onClick={() => advance('quoting')}>
                  退回重报
                </Button>
              </>
            ) : null}
            {showAdvance ? (
              <Button disabled={busy} onClick={() => advance(deal.NextStage as string)}>
                推进到{DEAL_STAGE_LABELS[deal.NextStage as string] ?? deal.NextStage}
              </Button>
            ) : null}
            {canAdvance && !financeOnly && deal.Stage !== 'lost' && deal.Stage !== 'settled' ? (
              <Button variant="ghost" disabled={busy} onClick={() => advance('lost')}>
                标记为已丢失
              </Button>
            ) : null}
            {canEdit ? (
              <Button variant="outline" asChild>
                <Link to="/business">去列表编辑</Link>
              </Button>
            ) : null}
            <Button variant="ghost" asChild>
              <Link to="/business">
                <ArrowLeft className="size-4" />
                返回列表
              </Link>
            </Button>
          </div>

          <dl className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <Field label="客户名" value={deal.ClientName} />
            <Field label="对接人" value={deal.Contact || '未填写'} />
            <Field label="报价" value={deal.AmountDisplay} />
            <Field label="下一步" value={deal.NextStep} />
            <Field label="预计签约" value={`${formatShortDate(deal.ExpectedCloseAt)} · ${formatDueHint(deal.ExpectedCloseAt)}`} />
            <Field label="创建人" value={deal.CreatedBy || '未知'} />
          </dl>

          {deal.Notes ? (
            <div className="space-y-2">
              <p className="text-[11px] uppercase tracking-[0.2em] text-muted-foreground">备注</p>
              <p className="max-w-3xl text-sm leading-7 text-muted-foreground">{deal.Notes}</p>
            </div>
          ) : null}
        </PanelBody>
      </Panel>

      <Panel>
        <PanelHeader
          eyebrow="关联内容"
          title="这张商务单下的内容计划"
          description={plans.length > 0 ? `共 ${plans.length} 条。` : '还没有关联的内容计划。'}
        />
        <PanelBody className="space-y-3">
          {plans.length > 0 ? (
            plans.map((plan) => (
              <Link
                key={plan.Id}
                to={`/plans/${plan.Id}`}
                className="flex items-start justify-between gap-4 rounded-2xl border border-border/60 bg-background/60 px-5 py-4 transition-colors hover:bg-muted/60 focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none"
              >
                <div className="space-y-1">
                  <p className="font-medium leading-6">{plan.Title}</p>
                  <p className="text-sm text-muted-foreground">
                    {plan.CycleDisplay} · {plan.ScoreDisplay} · {plan.Direction || '缺少方向说明'}
                  </p>
                </div>
                <div className="flex items-center gap-2">
                  <StatusPill tone={(plan.Tone as StatusTone) ?? 'default'}>{plan.StatusLabel}</StatusPill>
                  <ArrowRight className="size-4 text-muted-foreground" />
                </div>
              </Link>
            ))
          ) : (
            <p className="px-1 py-2 text-sm leading-6 text-muted-foreground">
              内容计划在新建时可以关联这张商务单，关联后会出现在这里。
            </p>
          )}
        </PanelBody>
      </Panel>
    </PageShell>
  );
};

const Field = ({ label, value }: { label: string; value: string }) => (
  <div className="rounded-2xl border border-border/60 bg-background/60 px-5 py-4">
    <dt className="text-[11px] uppercase tracking-[0.16em] text-muted-foreground">{label}</dt>
    <dd className="mt-2 text-sm leading-6 break-words">{value}</dd>
  </div>
);

export default DealDetailView;
