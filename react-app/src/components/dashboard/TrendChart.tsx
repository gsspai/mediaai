import { formatCompactNumber } from '@/lib/format';
import { cn } from '@/lib/utils';

export type TrendPoint = {
  label: string;
  views: number;
  comments: number;
  shares: number;
};

const series = [
  { key: 'views', label: '浏览量', lineClassName: 'text-chart-1', dotClassName: 'bg-chart-1' },
  { key: 'comments', label: '评论量', lineClassName: 'text-info', dotClassName: 'bg-info' },
  { key: 'shares', label: '转发量', lineClassName: 'text-destructive', dotClassName: 'bg-destructive' },
] as const;

const VIEW_WIDTH = 100;
const VIEW_HEIGHT = 46;
const PADDING_X = 3;
const PADDING_Y = 5;

const buildCoordinates = (values: number[]) => {
  const max = Math.max(...values, 1);
  const step = values.length > 1 ? (VIEW_WIDTH - PADDING_X * 2) / (values.length - 1) : 0;

  return values.map((value, index) => ({
    x: PADDING_X + index * step,
    y: VIEW_HEIGHT - PADDING_Y - (value / max) * (VIEW_HEIGHT - PADDING_Y * 2),
  }));
};

const toPolyline = (points: { x: number; y: number }[]) =>
  points.map((point) => `${point.x.toFixed(2)},${point.y.toFixed(2)}`).join(' ');

/**
 * 平台走势图：三条指标线各自按自身峰值归一化，用来比较走势形状而不是绝对值。
 * 具体数值通过下方指标卡与数据点 title 提供，避免绝对值被不同量级压平。
 */
const TrendChart = ({ points, platform, className }: { points: TrendPoint[]; platform: string; className?: string }) => {
  if (points.length === 0) {
    return (
      <div className={cn('flex h-40 items-center justify-center rounded-2xl border border-dashed border-border/70 text-sm text-muted-foreground', className)}>
        暂无更新数据
      </div>
    );
  }

  const viewsCoordinates = buildCoordinates(points.map((point) => point.views));
  const areaPath = [
    `M ${viewsCoordinates[0].x.toFixed(2)} ${(VIEW_HEIGHT - PADDING_Y).toFixed(2)}`,
    ...viewsCoordinates.map((point) => `L ${point.x.toFixed(2)} ${point.y.toFixed(2)}`),
    `L ${viewsCoordinates[viewsCoordinates.length - 1].x.toFixed(2)} ${(VIEW_HEIGHT - PADDING_Y).toFixed(2)}`,
    'Z',
  ].join(' ');

  return (
    <div className={cn('space-y-3', className)}>
      <svg
        viewBox={`0 0 ${VIEW_WIDTH} ${VIEW_HEIGHT}`}
        preserveAspectRatio="none"
        className="h-36 w-full"
        role="img"
        aria-label={`${platform} 最近 ${points.length} 条更新的浏览、评论、转发走势`}
      >
        <title>{`${platform} 最近 ${points.length} 条更新走势`}</title>
        {[0.25, 0.5, 0.75].map((ratio) => (
          <line
            key={ratio}
            x1={PADDING_X}
            x2={VIEW_WIDTH - PADDING_X}
            y1={VIEW_HEIGHT * ratio}
            y2={VIEW_HEIGHT * ratio}
            stroke="var(--border)"
            strokeWidth={0.5}
            vectorEffect="non-scaling-stroke"
          />
        ))}

        <path d={areaPath} fill="var(--chart-1)" opacity={0.07} />

        {series.map((item) => {
          const coordinates = buildCoordinates(points.map((point) => point[item.key]));
          return (
            <g key={item.key} className={item.lineClassName}>
              <polyline
                fill="none"
                stroke="currentColor"
                strokeWidth={1.6}
                strokeLinecap="round"
                strokeLinejoin="round"
                vectorEffect="non-scaling-stroke"
                points={toPolyline(coordinates)}
              />
              {coordinates.map((coordinate, index) => (
                <circle
                  key={`${item.key}-${points[index].label}`}
                  cx={coordinate.x}
                  cy={coordinate.y}
                  r={1.4}
                  fill="currentColor"
                  stroke="var(--card)"
                  strokeWidth={0.6}
                  vectorEffect="non-scaling-stroke"
                >
                  <title>{`${points[index].label} · ${item.label} ${formatCompactNumber(points[index][item.key])}`}</title>
                </circle>
              ))}
            </g>
          );
        })}
      </svg>

      <div className="flex items-center justify-between text-[11px] tabular-nums text-muted-foreground">
        {points.map((point) => (
          <span key={point.label}>{point.label}</span>
        ))}
      </div>
    </div>
  );
};

export const TrendLegend = ({ className }: { className?: string }) => (
  <div className={cn('flex flex-wrap items-center gap-x-4 gap-y-2 text-[11px] text-muted-foreground', className)}>
    {series.map((item) => (
      <span key={item.key} className="inline-flex items-center gap-2">
        <span className={cn('h-2 w-2 rounded-full', item.dotClassName)} />
        {item.label}
      </span>
    ))}
  </div>
);

export default TrendChart;
