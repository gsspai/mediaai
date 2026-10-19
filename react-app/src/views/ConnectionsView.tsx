import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { ArrowLeft, Link2, Pencil, Plus, RefreshCw, Save } from 'lucide-react';
import PageShell from '@/components/dashboard/PageShell';
import { Panel, PanelBody, PanelHeader } from '@/components/dashboard/Panel';
import StatusPill, { type StatusTone } from '@/components/dashboard/StatusPill';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
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
import PlatformAccountManager from '@/api/PlatformAccountManager';
import MetricManager from '@/api/MetricManager';
import { MetricEntryDto, PlatformAccountDto } from '@/api/AppDtos';
import { formatCompactNumber } from '@/lib/format';
import { PLATFORM_LABELS, getPlatformLabel } from '@/utils/platforms';

type LoadStatus = 'loading' | 'ready' | 'error';

const accountStatusMeta: Record<string, { label: string; tone: StatusTone }> = {
  connected: { label: '接口已授权', tone: 'success' },
  manual: { label: '手工录入', tone: 'info' },
  paused: { label: '已停用', tone: 'default' },
  error: { label: '授权失效', tone: 'destructive' },
};

const sourceMeta: Record<string, { label: string; tone: StatusTone }> = {
  api: { label: '接口同步', tone: 'success' },
  manual: { label: '手工录入', tone: 'info' },
  seed: { label: '开发态示例', tone: 'default' },
};

const todayString = () => {
  const now = new Date();
  const pad = (value: number) => String(value).padStart(2, '0');
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
};

type AccountFormState = {
  Id: string | null;
  Platform: string;
  Handle: string;
  DisplayName: string;
  Status: string;
  FollowerCount: string;
};

const emptyAccountForm = (): AccountFormState => ({
  Id: null,
  Platform: 'douyin',
  Handle: '',
  DisplayName: '',
  Status: 'manual',
  FollowerCount: '0',
});

type MetricFormState = {
  AccountId: string;
  StatDate: string;
  Views: string;
  Likes: string;
  Comments: string;
  Shares: string;
  NewFollowers: string;
};

const emptyMetricForm = (accountId: string): MetricFormState => ({
  AccountId: accountId,
  StatDate: todayString(),
  Views: '',
  Likes: '',
  Comments: '',
  Shares: '',
  NewFollowers: '',
});

const toNumber = (value: string): number => {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
};

const ConnectionsView = () => {
  const [accounts, setAccounts] = useState<PlatformAccountDto[]>([]);
  const [accountsStatus, setAccountsStatus] = useState<LoadStatus>('loading');
  const [entries, setEntries] = useState<MetricEntryDto[]>([]);
  const [entriesStatus, setEntriesStatus] = useState<LoadStatus>('loading');
  const [showPaused, setShowPaused] = useState(false);
  const [reloadKey, setReloadKey] = useState(0);
  const [feedback, setFeedback] = useState<{ tone: StatusTone; text: string } | null>(null);

  const [accountDialogOpen, setAccountDialogOpen] = useState(false);
  const [accountForm, setAccountForm] = useState<AccountFormState>(emptyAccountForm());
  const [accountFormError, setAccountFormError] = useState('');
  const [savingAccount, setSavingAccount] = useState(false);

  const [metricDialogOpen, setMetricDialogOpen] = useState(false);
  const [metricForm, setMetricForm] = useState<MetricFormState>(emptyMetricForm(''));
  const [metricFormError, setMetricFormError] = useState('');
  const [savingMetric, setSavingMetric] = useState(false);

  const reload = useCallback(() => setReloadKey((value) => value + 1), []);

  useEffect(() => {
    let cancelled = false;
    setAccountsStatus('loading');

    PlatformAccountManager.ListAccounts({ IncludePaused: showPaused })
      .then((result) => {
        if (cancelled) return;
        setAccounts(result.Items ?? []);
        setAccountsStatus('ready');
      })
      .catch(() => {
        if (cancelled) return;
        setAccounts([]);
        setAccountsStatus('error');
      });

    return () => {
      cancelled = true;
    };
  }, [showPaused, reloadKey]);

  useEffect(() => {
    let cancelled = false;
    setEntriesStatus('loading');

    MetricManager.ListRecentMetrics({ AccountId: null, Take: 20 })
      .then((result) => {
        if (cancelled) return;
        setEntries(result.Items ?? []);
        setEntriesStatus('ready');
      })
      .catch(() => {
        if (cancelled) return;
        setEntries([]);
        setEntriesStatus('error');
      });

    return () => {
      cancelled = true;
    };
  }, [reloadKey]);

  const accountOptions = useMemo(() => accounts.filter((account) => account.Status !== 'paused'), [accounts]);

  const openCreateAccount = () => {
    setAccountForm(emptyAccountForm());
    setAccountFormError('');
    setAccountDialogOpen(true);
  };

  const openEditAccount = (account: PlatformAccountDto) => {
    setAccountForm({
      Id: account.Id,
      Platform: account.Platform,
      Handle: account.Handle,
      DisplayName: account.DisplayName,
      Status: account.Status,
      FollowerCount: String(account.FollowerCount),
    });
    setAccountFormError('');
    setAccountDialogOpen(true);
  };

  const openMetricDialog = (accountId?: string) => {
    setMetricForm(emptyMetricForm(accountId ?? accountOptions[0]?.Id ?? ''));
    setMetricFormError('');
    setMetricDialogOpen(true);
  };

  const submitAccount = async () => {
    setSavingAccount(true);
    setAccountFormError('');

    try {
      const result = await PlatformAccountManager.SaveAccount({
        Id: accountForm.Id,
        Platform: accountForm.Platform,
        Handle: accountForm.Handle,
        DisplayName: accountForm.DisplayName,
        Status: accountForm.Status,
        FollowerCount: toNumber(accountForm.FollowerCount),
      });

      if (!result.Success) {
        setAccountFormError(result.Message);
        return;
      }

      setAccountDialogOpen(false);
      setFeedback({ tone: 'success', text: result.Message });
      reload();
    } catch {
      setAccountFormError('保存失败，后端接口不可用。');
    } finally {
      setSavingAccount(false);
    }
  };

  const disableAccount = async (account: PlatformAccountDto) => {
    if (!window.confirm(`停用「${account.DisplayName}」？历史日记录会保留，首页仍可回溯。`)) {
      return;
    }

    try {
      const result = await PlatformAccountManager.DisableAccount({ AccountId: account.Id });
      setFeedback({ tone: result.Success ? 'success' : 'destructive', text: result.Message });
      if (result.Success) reload();
    } catch {
      setFeedback({ tone: 'destructive', text: '停用失败，后端接口不可用。' });
    }
  };

  const submitMetric = async () => {
    if (!metricForm.AccountId) {
      setMetricFormError('请先选择账号。');
      return;
    }

    setSavingMetric(true);
    setMetricFormError('');

    try {
      const result = await MetricManager.IngestManual({
        AccountId: metricForm.AccountId,
        StatDate: metricForm.StatDate,
        Views: toNumber(metricForm.Views),
        Likes: toNumber(metricForm.Likes),
        Comments: toNumber(metricForm.Comments),
        Shares: toNumber(metricForm.Shares),
        NewFollowers: toNumber(metricForm.NewFollowers),
      });

      if (!result.Success) {
        setMetricFormError(result.Message);
        return;
      }

      setMetricDialogOpen(false);
      setFeedback({ tone: result.Updated ? 'warning' : 'success', text: result.Message });
      reload();
    } catch {
      setMetricFormError('保存失败，后端接口不可用。');
    } finally {
      setSavingMetric(false);
    }
  };

  const activeCount = accounts.filter((account) => account.Status !== 'paused').length;

  return (
    <PageShell>
      <Panel>
        <PanelHeader
          eyebrow="平台连接"
          title="账号与数据录入"
          description="首页的每个数字都必须能回答「谁、哪个平台、哪天、怎么录的」。这里维护账号并补录每日指标。"
          icon={Link2}
          actions={
            <div className="flex flex-wrap items-center gap-2">
              <Button variant="outline" size="sm" asChild>
                <Link to="/">
                  <ArrowLeft className="size-4" />
                  返回工作台
                </Link>
              </Button>
              <Button size="sm" onClick={openCreateAccount}>
                <Plus className="size-4" />
                新增账号
              </Button>
            </div>
          }
        />
        <PanelBody className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
            <StatusPill tone="info">{activeCount} 个在用账号</StatusPill>
            <span>停用账号不会出现在录入选项里，历史记录仍然保留。</span>
          </div>
          <div className="flex items-center gap-2">
            <label className="flex items-center gap-2 text-sm text-muted-foreground">
              <input
                type="checkbox"
                className="size-4 rounded border-border"
                checked={showPaused}
                onChange={(event) => setShowPaused(event.target.checked)}
              />
              显示已停用
            </label>
            <Button variant="outline" size="sm" onClick={() => openMetricDialog()}>
              <Save className="size-4" />
              录入日指标
            </Button>
            <Button variant="ghost" size="sm" onClick={reload}>
              <RefreshCw className="size-4" />
              刷新
            </Button>
          </div>
        </PanelBody>
      </Panel>

      {feedback ? (
        <div
          className={`flex flex-wrap items-center justify-between gap-3 rounded-2xl border px-4 py-3 text-sm leading-6 ${
            feedback.tone === 'destructive'
              ? 'border-destructive/30 bg-destructive/5 text-destructive'
              : 'border-border/60 bg-muted/40 text-foreground'
          }`}
        >
          <span>{feedback.text}</span>
          <button type="button" className="text-xs text-muted-foreground underline" onClick={() => setFeedback(null)}>
            关闭
          </button>
        </div>
      ) : null}

      {accountsStatus === 'error' ? (
        <div className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm leading-6 text-destructive">
          <span>账号列表加载失败。停用或错填的账号请不要继续录入，先确认后端可用。</span>
          <Button variant="outline" size="sm" onClick={reload}>
            重试
          </Button>
        </div>
      ) : null}

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        {accountsStatus === 'loading'
          ? Array.from({ length: 3 }).map((_, index) => (
              <div key={index} className="h-44 animate-pulse rounded-3xl border border-border/60 bg-muted/40" />
            ))
          : null}

        {accountsStatus === 'ready' && accounts.length === 0 ? (
          <div className="rounded-3xl border border-dashed border-border/70 bg-background/60 px-6 py-10 text-center md:col-span-2 xl:col-span-3">
            <p className="text-sm font-medium">还没有任何平台账号</p>
            <p className="mt-2 text-sm text-muted-foreground">先新增一个账号，再录入它今天的播放与互动数据。</p>
            <Button className="mt-4" size="sm" onClick={openCreateAccount}>
              <Plus className="size-4" />
              新增账号
            </Button>
          </div>
        ) : null}

        {accounts.map((account) => {
          const status = accountStatusMeta[account.Status] ?? { label: account.Status, tone: 'default' as StatusTone };
          const paused = account.Status === 'paused';

          return (
            <article
              key={account.Id}
              className={`flex flex-col gap-4 rounded-3xl border border-border/60 bg-background/60 p-5 ${
                paused ? 'opacity-70' : ''
              }`}
            >
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0 space-y-1">
                  <p className="text-[11px] uppercase tracking-[0.18em] text-muted-foreground">
                    {getPlatformLabel(account.Platform)}
                  </p>
                  <p className="truncate text-lg font-semibold tracking-tight">{account.DisplayName}</p>
                  <p className="truncate font-mono text-xs text-muted-foreground">{account.Handle}</p>
                </div>
                <div className="flex shrink-0 flex-col items-end gap-1">
                  <StatusPill tone={status.tone}>{status.label}</StatusPill>
                  {account.OwnerId ? null : <span className="text-[10px] text-muted-foreground">共享账号</span>}
                </div>
              </div>

              <dl className="grid grid-cols-2 gap-2 text-sm">
                <div className="rounded-xl bg-muted/40 px-3 py-2">
                  <dt className="text-[10px] uppercase tracking-[0.14em] text-muted-foreground">粉丝数</dt>
                  <dd className="mt-1 font-medium tabular-nums">{formatCompactNumber(account.FollowerCount)}</dd>
                </div>
                <div className="rounded-xl bg-muted/40 px-3 py-2">
                  <dt className="text-[10px] uppercase tracking-[0.14em] text-muted-foreground">最近录入</dt>
                  <dd className="mt-1 font-medium tabular-nums">{account.LastMetricDate ?? '尚未录入'}</dd>
                </div>
              </dl>

              <p className="text-[11px] leading-5 text-muted-foreground">
                近 7 天 {account.RecentMetricCount} 条记录 · 创建于 {account.CreatedAt}
              </p>

              <div className="mt-auto flex flex-wrap gap-2">
                <Button variant="outline" size="sm" disabled={paused} onClick={() => openMetricDialog(account.Id)}>
                  <Save className="size-4" />
                  录入
                </Button>
                <Button variant="ghost" size="sm" onClick={() => openEditAccount(account)}>
                  <Pencil className="size-4" />
                  编辑
                </Button>
                {paused ? null : (
                  <Button variant="ghost" size="sm" className="text-destructive" onClick={() => disableAccount(account)}>
                    停用
                  </Button>
                )}
              </div>
            </article>
          );
        })}
      </div>

      <Panel>
        <PanelHeader
          eyebrow="最近录入"
          title="日记录明细"
          description="按统计日期倒序，最多 20 条。来源与录入人直接展示，避免出现无法回溯的数字。"
        />
        <PanelBody className="overflow-x-auto p-0">
          {entriesStatus === 'error' ? (
            <div className="px-6 py-5 text-sm text-destructive">日记录加载失败，请点击上方「刷新」重试。</div>
          ) : null}

          {entriesStatus === 'ready' && entries.length === 0 ? (
            <div className="px-6 py-8 text-sm text-muted-foreground">还没有任何日记录。</div>
          ) : null}

          {entries.length > 0 ? (
            <table className="w-full min-w-[720px] border-collapse text-sm">
              <thead>
                <tr className="border-b border-border/60 text-left text-[11px] uppercase tracking-[0.14em] text-muted-foreground">
                  <th className="px-6 py-3 font-medium">日期</th>
                  <th className="px-3 py-3 font-medium">账号</th>
                  <th className="px-3 py-3 text-right font-medium">播放</th>
                  <th className="px-3 py-3 text-right font-medium">点赞</th>
                  <th className="px-3 py-3 text-right font-medium">评论</th>
                  <th className="px-3 py-3 text-right font-medium">转发</th>
                  <th className="px-3 py-3 text-right font-medium">新增粉丝</th>
                  <th className="px-3 py-3 font-medium">来源</th>
                  <th className="px-6 py-3 font-medium">录入人</th>
                </tr>
              </thead>
              <tbody>
                {entries.map((entry) => {
                  const source = sourceMeta[entry.Source] ?? { label: entry.Source, tone: 'default' as StatusTone };
                  return (
                    <tr key={entry.Id} className="border-b border-border/40 last:border-0">
                      <td className="px-6 py-3 font-mono text-xs tabular-nums">{entry.StatDate}</td>
                      <td className="px-3 py-3">
                        <span className="font-medium">{entry.AccountName}</span>
                        <span className="ml-2 text-xs text-muted-foreground">{getPlatformLabel(entry.Platform)}</span>
                      </td>
                      <td className="px-3 py-3 text-right tabular-nums">{entry.Views.toLocaleString()}</td>
                      <td className="px-3 py-3 text-right tabular-nums">{entry.Likes.toLocaleString()}</td>
                      <td className="px-3 py-3 text-right tabular-nums">{entry.Comments.toLocaleString()}</td>
                      <td className="px-3 py-3 text-right tabular-nums">{entry.Shares.toLocaleString()}</td>
                      <td className="px-3 py-3 text-right tabular-nums">{entry.NewFollowers.toLocaleString()}</td>
                      <td className="px-3 py-3">
                        <StatusPill tone={source.tone}>{source.label}</StatusPill>
                      </td>
                      <td className="px-6 py-3 text-xs text-muted-foreground">
                        {entry.RecordedBy}
                        <span className="ml-1 text-[10px]">{entry.RecordedAt}</span>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          ) : null}
        </PanelBody>
      </Panel>

      <Dialog open={accountDialogOpen} onOpenChange={setAccountDialogOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{accountForm.Id ? '编辑平台账号' : '新增平台账号'}</DialogTitle>
            <DialogDescription>账号标识用于区分同一平台下的多个发布身份，例如 @shuishui。</DialogDescription>
          </DialogHeader>

          <div className="grid gap-4">
            <div className="grid gap-2">
              <Label htmlFor="account-platform">平台</Label>
              <Select
                value={accountForm.Platform}
                onValueChange={(value) => setAccountForm((form) => ({ ...form, Platform: value }))}
              >
                <SelectTrigger id="account-platform">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {Object.entries(PLATFORM_LABELS).map(([value, label]) => (
                    <SelectItem key={value} value={value}>
                      {label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="grid gap-2">
              <Label htmlFor="account-name">展示名</Label>
              <Input
                id="account-name"
                value={accountForm.DisplayName}
                placeholder="抖音 · 主号"
                onChange={(event) => setAccountForm((form) => ({ ...form, DisplayName: event.target.value }))}
              />
            </div>

            <div className="grid gap-2">
              <Label htmlFor="account-handle">账号标识</Label>
              <Input
                id="account-handle"
                value={accountForm.Handle}
                placeholder="@shuishui"
                onChange={(event) => setAccountForm((form) => ({ ...form, Handle: event.target.value }))}
              />
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div className="grid gap-2">
                <Label htmlFor="account-status">接入方式</Label>
                <Select
                  value={accountForm.Status}
                  onValueChange={(value) => setAccountForm((form) => ({ ...form, Status: value }))}
                >
                  <SelectTrigger id="account-status">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="manual">手工录入</SelectItem>
                    <SelectItem value="connected">接口已授权</SelectItem>
                    <SelectItem value="paused">暂停统计</SelectItem>
                    <SelectItem value="error">授权失效</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="grid gap-2">
                <Label htmlFor="account-followers">粉丝数</Label>
                <Input
                  id="account-followers"
                  type="number"
                  min={0}
                  value={accountForm.FollowerCount}
                  onChange={(event) => setAccountForm((form) => ({ ...form, FollowerCount: event.target.value }))}
                />
              </div>
            </div>

            {accountFormError ? <p className="text-sm text-destructive">{accountFormError}</p> : null}
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setAccountDialogOpen(false)}>
              取消
            </Button>
            <Button onClick={submitAccount} disabled={savingAccount}>
              {savingAccount ? '保存中…' : '保存'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={metricDialogOpen} onOpenChange={setMetricDialogOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>录入日指标</DialogTitle>
            <DialogDescription>同一账号同一天重复提交会覆盖旧值，录入人按当前登录邮箱记录。</DialogDescription>
          </DialogHeader>

          <div className="grid gap-4">
            <div className="grid gap-2">
              <Label htmlFor="metric-account">账号</Label>
              <Select
                value={metricForm.AccountId}
                onValueChange={(value) => setMetricForm((form) => ({ ...form, AccountId: value }))}
              >
                <SelectTrigger id="metric-account">
                  <SelectValue placeholder="选择账号" />
                </SelectTrigger>
                <SelectContent>
                  {accountOptions.map((account) => (
                    <SelectItem key={account.Id} value={account.Id}>
                      {account.DisplayName} · {getPlatformLabel(account.Platform)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="grid gap-2">
              <Label htmlFor="metric-date">统计日期</Label>
              <Input
                id="metric-date"
                type="date"
                max={todayString()}
                value={metricForm.StatDate}
                onChange={(event) => setMetricForm((form) => ({ ...form, StatDate: event.target.value }))}
              />
            </div>

            <div className="grid grid-cols-2 gap-3">
              {(
                [
                  ['Views', '播放 / 阅读'],
                  ['Likes', '点赞'],
                  ['Comments', '评论'],
                  ['Shares', '转发'],
                  ['NewFollowers', '新增粉丝'],
                ] as const
              ).map(([field, label]) => (
                <div key={field} className="grid gap-2">
                  <Label htmlFor={`metric-${field}`}>{label}</Label>
                  <Input
                    id={`metric-${field}`}
                    type="number"
                    min={field === 'NewFollowers' ? undefined : 0}
                    value={metricForm[field]}
                    onChange={(event) => setMetricForm((form) => ({ ...form, [field]: event.target.value }))}
                  />
                </div>
              ))}
            </div>

            <p className="text-xs leading-5 text-muted-foreground">
              互动数之和不能超过播放数；新增粉丝可以填负数，用于记录掉粉。
            </p>

            {metricFormError ? <p className="text-sm text-destructive">{metricFormError}</p> : null}
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setMetricDialogOpen(false)}>
              取消
            </Button>
            <Button onClick={submitMetric} disabled={savingMetric}>
              {savingMetric ? '保存中…' : '保存记录'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </PageShell>
  );
};

export default ConnectionsView;
