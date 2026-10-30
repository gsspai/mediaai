import { useCallback, useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ArrowLeft, ArrowRight, ExternalLink, RefreshCw } from 'lucide-react';
import PageShell from '@/components/dashboard/PageShell';
import { Panel, PanelBody, PanelHeader } from '@/components/dashboard/Panel';
import StatusPill, { type StatusTone } from '@/components/dashboard/StatusPill';
import StatusTimeline from '@/components/dashboard/StatusTimeline';
import { Button } from '@/components/ui/button';
import ContentPlanManager from '@/api/ContentPlanManager';
import type { ContentPlanDto } from '@/api/AppDtos';
import useAuthStore from '@/auth/AuthStore';
import { resolveErrorText } from '@/utils/errors';
import { PLAN_STATUS_LABELS, PLAN_STATUS_OPTIONS, formatPlatforms, formatShortDate } from '@/utils/execution';
import { canEditPlans } from '@/utils/permissions';

type LoadStatus = 'loading' | 'ready' | 'error' | 'missing';

const PlanDetailView = () => {
  const { planId } = useParams<{ planId: string }>();
  const roles = useAuthStore((state) => state.session?.Roles ?? []);
  const canEdit = canEditPlans(roles);

  const [status, setStatus] = useState<LoadStatus>('loading');
  const [plan, setPlan] = useState<ContentPlanDto | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ tone: 'success' | 'error'; text: string } | null>(null);

  const load = useCallback(() => {
    if (!planId) {
      setStatus('missing');
      return;
    }

    setStatus('loading');
    let cancelled = false;

    ContentPlanManager.GetPlan({ PlanId: planId })
      .then((result) => {
        if (cancelled) return;
        if (!result) {
          setStatus('missing');
          return;
        }

        setPlan(result);
        setStatus('ready');
      })
      .catch(() => {
        if (cancelled) return;
        setStatus('error');
      });

    return () => {
      cancelled = true;
    };
  }, [planId]);

  useEffect(() => load(), [load, reloadKey]);

  const updateStatus = async (target: string) => {
    if (!plan) return;
    setBusy(true);
    setNotice(null);
    try {
      const result = await ContentPlanManager.UpdatePlanStatus({ PlanId: plan.Id, Status: target });
      if (!result.Success) {
        setNotice({ tone: 'error', text: result.Message || '状态推进失败。' });
        return;
      }

      setNotice({ tone: 'success', text: result.Message });
      setReloadKey((value) => value + 1);
    } catch (error) {
      setNotice({ tone: 'error', text: resolveErrorText(error, '状态推进失败，后端接口不可用。') });
    } finally {
      setBusy(false);
    }
  };

  if (status === 'loading') {
    return (
      <PageShell>
        <Panel>
          <PanelBody className="py-10 text-sm text-muted-foreground">正在加载内容计划…</PanelBody>
        </Panel>
      </PageShell>
    );
  }

  if (status !== 'ready' || !plan) {
    return (
      <PageShell>
        <Panel>
          <PanelHeader eyebrow="内容计划" title="详情不可用" actions={<StatusPill tone="warning">未找到</StatusPill>} />
          <PanelBody className="space-y-4">
            <p className="text-sm leading-7 text-muted-foreground">
              {status === 'missing' ? '这条内容计划不存在，或者链接里的 ID 不对。' : '内容计划没有加载成功，稍后重试。'}
            </p>
            <div className="flex flex-wrap gap-3">
              <Button variant="outline" asChild>
                <Link to="/plans">
                  <ArrowLeft className="size-4" />
                  返回内容计划
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

  return (
    <PageShell>
      <Panel>
        <PanelHeader
          eyebrow="内容计划 · 详情"
          title={plan.Title}
          description={`${formatPlatforms(plan.Platforms)} · 责任人 ${plan.AssigneeName || '未指定'} · 更新于 ${plan.UpdatedAt}`}
          actions={
            <div className="flex flex-wrap items-center gap-2">
              <StatusPill tone={(plan.Tone as StatusTone) ?? 'default'}>{plan.StatusLabel}</StatusPill>
              {plan.IsSample ? <StatusPill tone="default">示例数据</StatusPill> : null}
            </div>
          }
        />
        <PanelBody className="space-y-6">
          <StatusTimeline
            steps={PLAN_STATUS_OPTIONS.filter((option) => option.value !== 'archived').map((option) => ({
              value: option.value,
              label: option.label,
            }))}
            current={plan.Status}
            terminatedLabel={plan.Status === 'archived' ? '已归档' : undefined}
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
            {canEdit && plan.NextStatus ? (
              <Button disabled={busy} onClick={() => updateStatus(plan.NextStatus as string)}>
                推进到{PLAN_STATUS_LABELS[plan.NextStatus as string] ?? plan.NextStatus}
              </Button>
            ) : null}
            {canEdit && plan.Status !== 'archived' ? (
              <Button variant="ghost" disabled={busy} onClick={() => updateStatus('archived')}>
                归档
              </Button>
            ) : null}
            {plan.DealId ? (
              <Button variant="outline" asChild>
                <Link to={`/business/${plan.DealId}`}>
                  关联商务单
                  <ArrowRight className="size-4" />
                </Link>
              </Button>
            ) : null}
            {plan.Link ? (
              <Button variant="outline" asChild>
                <a href={plan.Link} target="_blank" rel="noreferrer">
                  查看成品
                  <ExternalLink className="size-4" />
                </a>
              </Button>
            ) : null}
            <Button variant="ghost" asChild>
              <Link to="/plans">
                <ArrowLeft className="size-4" />
                返回列表
              </Link>
            </Button>
          </div>

          {!canEdit ? (
            <p className="text-sm leading-6 text-muted-foreground">
              当前账号只能查看内容计划。推进状态需要「管理员 / 策划 / 运营 / 剪辑」角色。
            </p>
          ) : null}

          <dl className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <Field label="覆盖平台" value={formatPlatforms(plan.Platforms)} />
            <Field label="排期" value={`${plan.CycleDisplay} · ${formatShortDate(plan.PlannedAt)}`} />
            <Field label="机会分" value={plan.ScoreDisplay} />
            <Field label="责任人" value={plan.AssigneeName || '未指定'} />
            <Field label="关联商务单" value={plan.DealTitle || '未关联'} />
            <Field label="创建人" value={plan.CreatedBy || '未知'} />
          </dl>

          <div className="space-y-2">
            <p className="text-[11px] uppercase tracking-[0.2em] text-muted-foreground">内容方向</p>
            <p className="max-w-3xl text-sm leading-7 text-muted-foreground">
              {plan.Direction || '还没有填写方向说明。方向为空时会自动生成一条策划待办。'}
            </p>
          </div>
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

export default PlanDetailView;
