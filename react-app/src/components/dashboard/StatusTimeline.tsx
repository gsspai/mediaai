import { cn } from '@/lib/utils';

type TimelineStep = {
  value: string;
  label: string;
};

type StatusTimelineProps = {
  steps: TimelineStep[];
  /** 当前所处阶段标识；已终止（丢失 / 归档）时传空字符串。 */
  current: string;
  /** 终止态文案，例如「已丢失」，与 steps 分开显示。 */
  terminatedLabel?: string;
  terminatedTone?: 'destructive' | 'default';
  className?: string;
};

/**
 * 商务单阶段 / 内容计划状态的横向进度条。
 * 只做展示：当前阶段高亮，已走过的阶段置灰勾选，未到的不显示判断。
 * 阶段文案来自 options，真正的推进校验在后端。
 */
const StatusTimeline = ({ steps, current, terminatedLabel, terminatedTone = 'default', className }: StatusTimelineProps) => {
  const currentIndex = steps.findIndex((step) => step.value === current);
  const terminated = Boolean(terminatedLabel);

  return (
    <div className={cn('space-y-3', className)}>
      <ol className="flex flex-wrap items-center gap-x-2 gap-y-2">
        {steps.map((step, index) => {
          const isDone = !terminated && currentIndex >= 0 && index < currentIndex;
          const isCurrent = !terminated && index === currentIndex;

          return (
            <li key={step.value} className="flex items-center gap-2">
              <span
                className={cn(
                  'inline-flex items-center rounded-full border px-2.5 py-1 text-[11px] leading-none',
                  isCurrent
                    ? 'border-foreground/30 bg-foreground text-background'
                    : isDone
                      ? 'border-border/60 bg-muted text-muted-foreground'
                      : 'border-border/50 bg-background/60 text-muted-foreground'
                )}
              >
                {step.label}
              </span>
              {index < steps.length - 1 ? <span className="h-px w-4 bg-border" aria-hidden /> : null}
            </li>
          );
        })}
        {terminated ? (
          <li>
            <span
              className={cn(
                'inline-flex items-center rounded-full border px-2.5 py-1 text-[11px] leading-none',
                terminatedTone === 'destructive'
                  ? 'border-destructive/30 bg-destructive/10 text-destructive'
                  : 'border-border/60 bg-muted text-muted-foreground'
              )}
            >
              {terminatedLabel}
            </span>
          </li>
        ) : null}
      </ol>
      <p className="text-[11px] leading-5 text-muted-foreground">
        只能顺次推进一格；阶段推进由后端校验，界面上的按钮只是入口。
      </p>
    </div>
  );
};

export default StatusTimeline;
