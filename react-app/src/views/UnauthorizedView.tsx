import PageShell from '@/components/dashboard/PageShell';
import { Panel, PanelBody, PanelHeader } from '@/components/dashboard/Panel';
import StatusPill from '@/components/dashboard/StatusPill';
import { Button } from '@/components/ui/button';
import { Link } from 'react-router-dom';

const UnauthorizedView = () => (
  <PageShell>
    <Panel>
      <PanelHeader eyebrow="MediaAI" title="暂无访问权限" actions={<StatusPill tone="destructive">无权限</StatusPill>} />
      <PanelBody className="space-y-6">
        <p className="max-w-2xl text-sm leading-7 text-muted-foreground">
          当前账号没有访问该页面的权限。如需开通，请联系管理员调整角色。
        </p>
        <Button asChild>
          <Link to="/">返回工作台</Link>
        </Button>
      </PanelBody>
    </Panel>
  </PageShell>
);

export default UnauthorizedView;
