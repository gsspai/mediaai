const trimZero = (value: number) => value.toFixed(1).replace(/\.0$/, '');

/**
 * 统一中文数字缩写：>= 1 万显示「万」，>= 1 千显示「千」，其余原样输出。
 * 全站数字展示都应通过该函数，避免 k / w / 万 混用。
 */
export const formatCompactNumber = (value: number): string => {
  if (!Number.isFinite(value)) return '-';
  if (Math.abs(value) >= 10000) return `${trimZero(value / 10000)}万`;
  if (Math.abs(value) >= 1000) return `${trimZero(value / 1000)}千`;
  return `${value}`;
};
