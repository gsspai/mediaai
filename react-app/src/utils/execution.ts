import { getPlatformLabel } from './platforms';
import { ROLE_LABELS } from './roles';

/**
 * 商务单阶段 / 内容计划状态的界面元数据。
 * 阶段标识与后端 DealStage / ContentPlanStatus 一致；文案由后端 DTO 下发，
 * 这里的 map 只用于筛选下拉、操作按钮和提交前校验，两边改文案时以后端为准。
 */

export type StageOption = { value: string; label: string };

export const DEAL_STAGE_OPTIONS: StageOption[] = [
  { value: 'lead', label: '线索' },
  { value: 'quoting', label: '报价中' },
  { value: 'pricing-review', label: '待报价审核' },
  { value: 'confirmed', label: '已确认' },
  { value: 'delivering', label: '待制作 / 发布' },
  { value: 'settled', label: '已结算' },
  { value: 'lost', label: '已丢失' },
];

export const PLAN_STATUS_OPTIONS: StageOption[] = [
  { value: 'draft', label: '选题中' },
  { value: 'scoring', label: '评分中' },
  { value: 'scheduled', label: '待制作 / 发布' },
  { value: 'published', label: '已发布' },
  { value: 'archived', label: '已归档' },
];

const toLabelMap = (options: StageOption[]): Record<string, string> =>
  options.reduce<Record<string, string>>((acc, option) => {
    acc[option.value] = option.label;
    return acc;
  }, {});

export const DEAL_STAGE_LABELS = toLabelMap(DEAL_STAGE_OPTIONS);
export const PLAN_STATUS_LABELS = toLabelMap(PLAN_STATUS_OPTIONS);

/** 角色标识 → 中文名；与后端 RoleText 保持一致，未知角色按「访客」处理。 */
export const getRoleLabel = (role: string): string => ROLE_LABELS[role] ?? '访客';

/** 平台标识数组 → 「抖音、公众号」；空数组显示「未指定平台」。 */
export const formatPlatforms = (platforms: string[] | null | undefined): string =>
  platforms && platforms.length > 0 ? platforms.map(getPlatformLabel).join('、') : '未指定平台';

/** 「2026-10-30」→「10-30」；空值显示「未确定」。 */
export const formatShortDate = (value: string | null | undefined): string => {
  if (!value) return '未确定';
  const parts = value.split('-');
  return parts.length === 3 ? `${parts[1]}-${parts[2]}` : value;
};

/** 今天之后的日期用「还剩 N 天」，已过期用「已逾期 N 天」。 */
export const formatDueHint = (value: string | null | undefined): string => {
  if (!value) return '未设截止日';
  const due = new Date(`${value}T00:00:00`);
  if (Number.isNaN(due.getTime())) return value;

  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const diff = Math.round((due.getTime() - today.getTime()) / 86400000);

  if (diff === 0) return '今天到期';
  if (diff > 0) return `还剩 ${diff} 天`;
  return `已逾期 ${Math.abs(diff)} 天`;
};

/** 待办紧急度的色板，与后端 TodoText.Tone 对应；后端已给 Tone 时优先用后端的。 */
export const urgencyTone = (urgency: string): string => {
  if (urgency.includes('逾期')) return 'destructive';
  if (urgency.includes('今日') || urgency.includes('需')) return 'warning';
  return 'default';
};
