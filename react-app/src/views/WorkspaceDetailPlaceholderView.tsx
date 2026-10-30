import PageShell from '@/components/dashboard/PageShell';
import { Panel, PanelBody, PanelHeader } from '@/components/dashboard/Panel';
import StatusPill from '@/components/dashboard/StatusPill';
import { Button } from '@/components/ui/button';
import { ArrowLeft, ArrowRight } from 'lucide-react';
import { Link, useLocation } from 'react-router-dom';

const routeMeta = [
  { prefix: '/connections', label: '平台连接', next: ['账号授权状态', '同步失败重试', '平台限额与告警'] },
];

const WorkspaceDetailPlaceholderView = () => {
  const location = useLocation();
  const meta = routeMeta.find((item) => location.pathname.startsWith(item.prefix));

  return (
    <PageShell>
      <Panel>
        <PanelHeader
          eyebrow="MediaAI · 详情占位"
          title={meta?.label ?? '待建设页面'}
          actions={<StatusPill tone="info">开发中</StatusPill>}
        />
        <PanelBody className="space-y-6">
          <div className="space-y-2">
            <p className="text-[11px] uppercase tracking-[0.2em] text-muted-foreground">当前跳转路径</p>
            <p className="font-mono text-sm break-all text-foreground">{location.pathname}</p>
          </div>

          <p className="max-w-2xl text-sm leading-7 text-muted-foreground">
            工作台的直达入口已经就位，这里先承接对应详情路径。真实业务内容将按下列顺序补齐。
          </p>

          <div className="grid gap-3 sm:grid-cols-3">
            {(meta?.next ?? ['页面结构', '数据接口', '交互细节']).map((item, index) => (
              <div key={item} className="rounded-2xl border border-border/60 bg-background/60 px-5 py-4">
                <p className="text-[11px] uppercase tracking-[0.16em] text-muted-foreground">
                  {String(index + 1).padStart(2, '0')}
                </p>
                <p className="mt-2 text-sm font-medium">{item}</p>
              </div>
            ))}
          </div>

          <div className="flex flex-wrap gap-3">
            <Button variant="outline" asChild>
              <Link to="/">
                <ArrowLeft className="size-4" />
                返回工作台
              </Link>
            </Button>
            <Button variant="ghost" asChild>
              <Link to="/connections">
                返回平台连接
                <ArrowRight className="size-4" />
              </Link>
            </Button>
          </div>
        </PanelBody>
      </Panel>
    </PageShell>
  );
};

export default WorkspaceDetailPlaceholderView;
