import PageShell from '@/components/dashboard/PageShell';
import { Panel, PanelBody, PanelHeader } from '@/components/dashboard/Panel';
import StatusPill from '@/components/dashboard/StatusPill';
import { Button } from '@/components/ui/button';
import { Link } from 'react-router-dom';

const NotFoundView = () => (
  <PageShell>
    <Panel>
      <PanelHeader eyebrow="MediaAI" title="页面不存在" actions={<StatusPill tone="warning">404</StatusPill>} />
      <PanelBody className="space-y-6">
        <p className="max-w-2xl text-sm leading-7 text-muted-foreground">
          你访问的地址不存在或已下线，请返回工作台继续操作。
        </p>
        <Button asChild>
          <Link to="/">返回工作台</Link>
        </Button>
      </PanelBody>
    </Panel>
  </PageShell>
);

export default NotFoundView;
