import { cn } from '@/lib/utils';
import type { ComponentType, ReactNode } from 'react';

type SectionHeaderProps = {
  eyebrow?: string;
  title: string;
  description?: string;
  actions?: ReactNode;
  icon?: ComponentType<{ className?: string }>;
  className?: string;
  titleAs?: 'h1' | 'h2' | 'h3';
  titleClassName?: string;
};

/**
 * 区块标题：统一「小标签 + 标题 + 说明 + 右侧操作」的层级结构。
 * 页面内所有卡片头与独立区块标题共用它，避免出现多种标题写法。
 */
const SectionHeader = ({
  eyebrow,
  title,
  description,
  actions,
  icon: Icon,
  className,
  titleAs: TitleTag = 'h2',
  titleClassName,
}: SectionHeaderProps) => (
  <div className={cn('flex min-w-0 flex-col', className)}>
    {/* 图标内联到 eyebrow 行：小图标与「机会 / 风险」同一行垂直居中，标题不再被图标推右，
        所有区块标题（含无图标的「执行列表」）共用同一条左基准线。 */}
    {Icon || eyebrow ? (
      <div className="flex items-center gap-1.5">
        {Icon ? <Icon className="size-3.5 shrink-0 text-muted-foreground" /> : null}
        {eyebrow ? (
          <p className="text-[11px] font-medium uppercase tracking-[0.2em] text-muted-foreground">{eyebrow}</p>
        ) : null}
      </div>
    ) : null}
    {/* 标题与右侧操作同行并垂直居中：eyebrow / description 不再把操作顶到上方，避免视觉上错开一整行。
        窄屏改为上下堆叠，避免标题被挤压截断。 */}
    <div className="flex flex-col items-start gap-2 pt-1.5 sm:flex-row sm:flex-wrap sm:items-center sm:gap-x-4">
      <TitleTag
        className={cn(
          'min-w-0 text-xl font-semibold tracking-tight text-foreground sm:flex-1 lg:text-2xl',
          titleClassName,
        )}
      >
        {title}
      </TitleTag>
      {actions ? <div className="flex shrink-0 flex-wrap items-center gap-3">{actions}</div> : null}
    </div>
    {description ? <p className="mt-1.5 max-w-2xl text-sm leading-6 text-muted-foreground">{description}</p> : null}
  </div>
);

export default SectionHeader;
