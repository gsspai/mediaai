import { cn } from '@/lib/utils';
import type { ReactNode } from 'react';

type PageShellProps = {
  children: ReactNode;
  className?: string;
};

/**
 * 页面级容器：统一页面留白、最大宽度与纵向节奏。
 * 所有路由视图都应使用它，避免各页面各自定义 padding / max-width。
 */
const PageShell = ({ children, className }: PageShellProps) => (
  <main className="min-h-screen px-5 py-7 text-foreground sm:px-7 lg:px-10 lg:py-10">
    <div className={cn('mx-auto flex w-full max-w-[1440px] flex-col gap-6', className)}>{children}</div>
  </main>
);

export default PageShell;
