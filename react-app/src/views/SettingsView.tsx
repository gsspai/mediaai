import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { ArrowLeft, Link2, RefreshCw, Save, SlidersHorizontal } from 'lucide-react';
import PageShell from '@/components/dashboard/PageShell';
import { Panel, PanelBody, PanelHeader } from '@/components/dashboard/Panel';
import StatusPill from '@/components/dashboard/StatusPill';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import SettingsManager from '@/api/SettingsManager';
import { SignalThresholdDto } from '@/api/AppDtos';

type LoadStatus = 'loading' | 'ready' | 'error';

type FieldKey =
  | 'BreakoutMultiplier'
  | 'BreakoutMinViews'
  | 'DeclineThresholdPercent'
  | 'LowInteractionRatePercent'
  | 'StaleDays';

type ThresholdField = {
  key: FieldKey;
  label: string;
  unit: string;
  hint: string;
  step: string;
  min: number;
  max: number;
};

/**
 * 字段的允许范围与 SettingsManager 里的校验保持一致：前端只做提示，真正的边界由后端判定，
 * 因此这里改宽了也不会让非法值落库。
 */
const fields: ThresholdField[] = [
  {
    key: 'BreakoutMultiplier',
    label: '爆款倍数',
    unit: '倍账号日均',
    hint: '单日播放 ≥ 账号区间日均的这个倍数，且不低于下面的播放下限，才算机会。',
    step: '0.1',
    min: 1.1,
    max: 10,
  },
  {
    key: 'BreakoutMinViews',
    label: '爆款播放下限',
    unit: '播放',
    hint: '绝对下限，避免小账号的正常波动被判成爆款。',
    step: '100',
    min: 0,
    max: 100000000,
  },
  {
    key: 'DeclineThresholdPercent',
    label: '掉速幅度',
    unit: '%',
    hint: '区间前一半与后一半的日均播放下降达到这个幅度，判为掉速风险。',
    step: '1',
    min: 1,
    max: 90,
  },
  {
    key: 'LowInteractionRatePercent',
    label: '互动率阈值',
    unit: '%',
    hint: '互动率 =（点赞 + 评论 + 转发）/ 播放。低于这个值判为互动偏低。',
    step: '0.1',
    min: 0.1,
    max: 50,
  },
  {
    key: 'StaleDays',
    label: '停更天数',
    unit: '天',
    hint: '连续这么多天没有任何日记录，判为停止更新。',
    step: '1',
    min: 1,
    max: 30,
  },
];

type FormValues = Record<FieldKey, string>;

const toFormValues = (thresholds: SignalThresholdDto): FormValues => ({
  BreakoutMultiplier: String(thresholds.BreakoutMultiplier),
  BreakoutMinViews: String(thresholds.BreakoutMinViews),
  DeclineThresholdPercent: String(thresholds.DeclineThresholdPercent),
  LowInteractionRatePercent: String(thresholds.LowInteractionRatePercent),
  StaleDays: String(thresholds.StaleDays),
});

const SettingsView = () => {
  const [status, setStatus] = useState<LoadStatus>('loading');
  const [thresholds, setThresholds] = useState<SignalThresholdDto | null>(null);
  const [values, setValues] = useState<FormValues | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);

  const load = useCallback(async () => {
    setStatus('loading');
    setError(null);
    try {
      const result = await SettingsManager.GetSignalThresholds({});
      setThresholds(result);
      setValues(toFormValues(result));
      setStatus('ready');
    } catch {
      setStatus('error');
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const update = (key: FieldKey, value: string) => {
    setFeedback(null);
    setValues((current) => (current ? { ...current, [key]: value } : current));
  };

  const submit = async () => {
    if (!values) return;

    const parsed: Partial<Record<FieldKey, number>> = {};
    for (const field of fields) {
      const raw = values[field.key].trim();
      const numeric = Number(raw);
      if (raw === '' || !Number.isFinite(numeric)) {
        setError(`${field.label}需要填一个数字，当前是「${values[field.key]}」。`);
        return;
      }
      parsed[field.key] = numeric;
    }

    setSaving(true);
    setError(null);
    setFeedback(null);

    try {
      const result = await SettingsManager.SaveSignalThresholds({
        BreakoutMultiplier: parsed.BreakoutMultiplier!,
        BreakoutMinViews: parsed.BreakoutMinViews!,
        DeclineThresholdPercent: parsed.DeclineThresholdPercent!,
        LowInteractionRatePercent: parsed.LowInteractionRatePercent!,
        StaleDays: parsed.StaleDays!,
      });

      if (!result.Success) {
        setError(result.Message);
        return;
      }

      setThresholds(result.Thresholds);
      setValues(toFormValues(result.Thresholds));
      setFeedback(result.Message);
    } catch {
      setError('保存失败，后端接口不可用。');
    } finally {
      setSaving(false);
    }
  };

  const dirty = thresholds && values ? fields.some((field) => values[field.key] !== String(thresholds[field.key])) : false;

  return (
    <PageShell>
      <Panel>
        <PanelHeader
          eyebrow="设置"
          title="机会与风险判定阈值"
          description="首页「爆款机会」和「限流 / 掉速风险」都按这里的阈值判定。保存后立即生效，判定规则文案由后端按同一份配置生成，不会出现界面写的和实际算的不一致。"
          icon={SlidersHorizontal}
          actions={
            <div className="flex flex-wrap items-center gap-2">
              <Button variant="outline" size="sm" asChild>
                <Link to="/">
                  <ArrowLeft className="size-4" />
                  返回工作台
                </Link>
              </Button>
              <Button variant="ghost" size="sm" onClick={() => void load()}>
                <RefreshCw className="size-4" />
                刷新
              </Button>
            </div>
          }
        />
        <PanelBody className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
            {status === 'ready' && thresholds ? (
              <>
                <StatusPill tone={thresholds.IsCustomized ? 'success' : 'default'}>
                  {thresholds.IsCustomized ? '已自定义阈值' : '默认阈值'}
                </StatusPill>
                <span>{thresholds.IsCustomized ? `最后修改：${thresholds.UpdatedAt} · ${thresholds.UpdatedBy}` : '库里还没有保存过阈值，当前走默认值。'}</span>
              </>
            ) : status === 'loading' ? (
              <span>正在读取阈值…</span>
            ) : (
              <span className="text-destructive">阈值读取失败，后端接口不可用。</span>
            )}
          </div>
          <Link to="/connections" className="inline-flex items-center gap-1 text-xs font-medium text-foreground underline underline-offset-4">
            <Link2 className="size-3.5" />
            管理账号 / 录入数据
          </Link>
        </PanelBody>
      </Panel>

      {error ? (
        <div className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm leading-6 text-destructive">
          <span>{error}</span>
          <button type="button" className="text-xs underline" onClick={() => setError(null)}>
            关闭
          </button>
        </div>
      ) : null}

      {feedback ? (
        <div className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-border/60 bg-muted/40 px-4 py-3 text-sm leading-6">
          <span>{feedback}</span>
          <button type="button" className="text-xs text-muted-foreground underline" onClick={() => setFeedback(null)}>
            关闭
          </button>
        </div>
      ) : null}

      <Panel>
        <PanelHeader eyebrow="阈值" title="判定参数" description="五项都必填，超出范围的输入后端会直接拒绝并说明原因，不会静默截断。" />
        <PanelBody>
          {status === 'loading' && !values ? (
            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
              {Array.from({ length: 5 }).map((_, index) => (
                <div key={index} className="h-24 animate-pulse rounded-2xl border border-border/60 bg-muted/40" />
              ))}
            </div>
          ) : null}

          {values ? (
            <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
              {fields.map((field) => (
                <div key={field.key} className="space-y-2">
                  <Label htmlFor={`threshold-${field.key}`} className="flex items-baseline justify-between gap-2">
                    <span className="font-medium">{field.label}</span>
                    <span className="text-[11px] font-normal text-muted-foreground">{field.unit}</span>
                  </Label>
                  <Input
                    id={`threshold-${field.key}`}
                    inputMode="decimal"
                    value={values[field.key]}
                    onChange={(event) => update(field.key, event.target.value)}
                    className="tabular-nums"
                  />
                  <p className="text-[11px] leading-5 text-muted-foreground">
                    {field.hint}
                    <span className="ml-1 text-muted-foreground/80">
                      （{field.min} ~ {field.max}）
                    </span>
                  </p>
                </div>
              ))}
            </div>
          ) : null}

          <div className="mt-6 flex flex-wrap items-center justify-between gap-3 border-t border-border/60 pt-4">
            <p className="text-[11px] leading-5 text-muted-foreground">
              {dirty ? '有未保存的修改，保存后首页会按新口径重新计算。' : '当前表单与已保存的阈值一致。'}
            </p>
            <Button size="sm" disabled={!values || saving} onClick={() => void submit()}>
              <Save className="size-4" />
              {saving ? '保存中…' : '保存阈值'}
            </Button>
          </div>
        </PanelBody>
      </Panel>

      <Panel>
        <PanelHeader
          eyebrow="当前生效口径"
          title="判定规则"
          description="这段话与首页底部展示的完全一致，由后端按已保存的阈值生成，不是前端拼的。"
        />
        <PanelBody className="space-y-2">
          {thresholds ? (
            <>
              <p className="text-sm leading-7">{thresholds.Policy}</p>
              <p className="text-[11px] leading-5 text-muted-foreground">
                {thresholds.IsCustomized
                  ? `来源：设置页保存 · ${thresholds.UpdatedAt} · ${thresholds.UpdatedBy}`
                  : '来源：代码内默认阈值（尚未在设置页保存过）'}
              </p>
            </>
          ) : (
            <p className="text-sm text-muted-foreground">暂时读不到阈值配置。</p>
          )}
        </PanelBody>
      </Panel>
    </PageShell>
  );
};

export default SettingsView;
