import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { ArrowLeft } from 'lucide-react';
import { Link, useLocation } from 'react-router-dom';

const WorkspaceDetailPlaceholderView = () => {
  const location = useLocation();

  return (
    <main className="min-h-screen px-6 py-10 text-foreground sm:px-8 lg:px-10">
      <section className="mx-auto flex w-full max-w-5xl flex-col gap-6">
        <div className="flex items-center gap-3 text-xs uppercase tracking-[0.22em] text-muted-foreground">
          <span>MediaAI</span>
          <span className="h-1 w-1 rounded-full bg-foreground/30" />
          <span>详情占位</span>
        </div>

        <Card className="border-border/60 bg-card/90 shadow-none">
          <CardHeader className="border-b border-border/60 pb-5">
            <CardDescription>当前跳转路径</CardDescription>
            <CardTitle className="text-3xl tracking-tight">{location.pathname}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-5 p-6">
            <p className="max-w-3xl text-sm leading-7 text-muted-foreground">
              工作台中的直达入口已经就位。这里先承接到对应详情路径，后续任务会逐步补齐商务合作、内容计划、平台连接与分析页面的真实业务内容。
            </p>

            <Button variant="outline" asChild>
              <Link to="/">
                <ArrowLeft className="size-4" />
                返回工作台
              </Link>
            </Button>
          </CardContent>
        </Card>
      </section>
    </main>
  );
};

export default WorkspaceDetailPlaceholderView;
