import { cn } from '@/lib/utils';
import type { ReactNode } from 'react';

export type StatusTone = 'default' | 'success' | 'warning' | 'info' | 'destructive';

const toneClasses: Record<StatusTone, string> = {
  default: 'border-border/70 bg-secondary text-secondary-foreground',
  success: 'border-success/25 bg-success/10 text-success',
  warning: 'border-warning/30 bg-warning/15 text-warning-foreground',
  info: 'border-info/25 bg-info/10 text-info',
  destructive: 'border-destructive/20 bg-destructive/10 text-destructive',
};

type StatusPillProps = {
  children: ReactNode;
  tone?: StatusTone;
  /** inverse 用于深色主色背景（如首页 Hero），避免与 tone 的颜色类互相覆盖。 */
  appearance?: 'default' | 'inverse';
  className?: string;
};

/**
 * 全站唯一的状态标签：固定高度 + 等宽数字，保证表格、卡片、图表里的标签基线一致。
 */
const StatusPill = ({ children, tone = 'default', appearance = 'default', className }: StatusPillProps) => (
  <span
    className={cn(
      'inline-flex h-6 shrink-0 items-center gap-1 rounded-full border px-2.5 text-[11px] font-medium leading-none tabular-nums',
      appearance === 'inverse'
        ? 'border-primary-foreground/20 bg-primary-foreground/10 text-primary-foreground/80'
        : toneClasses[tone],
      className,
    )}
  >
    {children}
  </span>
);

export default StatusPill;
