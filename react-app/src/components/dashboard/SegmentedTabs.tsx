import { cn } from '@/lib/utils';

export type SegmentedOption<T extends string> = {
  value: T;
  label: string;
};

type SegmentedTabsProps<T extends string> = {
  value: T;
  options: readonly SegmentedOption<T>[];
  onChange: (value: T) => void;
  label: string;
  className?: string;
};

/**
 * 分段切换控件：统一首页「执行列表」与后续筛选类交互的外观与键盘/读屏行为。
 */
const SegmentedTabs = <T extends string>({ value, options, onChange, label, className }: SegmentedTabsProps<T>) => (
  <div
    role="tablist"
    aria-label={label}
    className={cn('inline-flex flex-wrap items-center gap-1 rounded-full border border-border/70 bg-muted/70 p-1', className)}
  >
    {options.map((option) => {
      const isActive = option.value === value;
      return (
        <button
          key={option.value}
          type="button"
          role="tab"
          aria-selected={isActive}
          onClick={() => onChange(option.value)}
          className={cn(
            'h-8 rounded-full px-4 text-sm font-medium transition-colors focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none',
            isActive
              ? 'bg-primary text-primary-foreground shadow-sm'
              : 'text-muted-foreground hover:text-foreground',
          )}
        >
          {option.label}
        </button>
      );
    })}
  </div>
);

export default SegmentedTabs;
