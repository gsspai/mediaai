import {
  ADMIN_ROLE,
  EDITOR_ROLE,
  FINANCE_ROLE,
  OPERATIONS_ROLE,
  PLANNER_ROLE,
  ROLE_LABELS,
  SALES_ROLE,
  hasAnyRole,
} from './roles';

/**
 * 能力 → 角色 的前端唯一来源，取值来自「产品文档 · 权限设计」矩阵。
 * 后端同名约束用 `[RequireRole(...)]` 标注在 Manager 方法上；前端只负责隐藏入口，
 * 真正的拦截在后端，两边同时改才生效。
 */

/** 平台账号连接 / 指标录入 / CSV 导入：管理员与运营。 */
export const ACCOUNT_MANAGER_ROLES = [ADMIN_ROLE, OPERATIONS_ROLE] as const;

/** 管理层面板（判定阈值）：仅管理员。 */
export const ADMIN_ONLY_ROLES = [ADMIN_ROLE] as const;

/** 能把角色标识翻译成界面名称，用于「需要 xx 角色」这类提示。 */
export const formatRoleList = (roles: readonly string[]): string =>
  roles.map((role) => ROLE_LABELS[role] ?? role).join(' / ');

export const canManageAccounts = (roles: string[] | null | undefined): boolean =>
  hasAnyRole(roles, ACCOUNT_MANAGER_ROLES);

export const canEditThresholds = (roles: string[] | null | undefined): boolean =>
  hasAnyRole(roles, ADMIN_ONLY_ROLES);

/** 只读账号（访客）在页面顶部的说明，避免用户以为是页面出错。 */
export const readOnlyNotice = (): string =>
  `当前账号为访客，只能查看。新增账号、录入指标与批量导入需要「${formatRoleList(ACCOUNT_MANAGER_ROLES)}」角色，请联系管理员开通。`;

/** 商务单新建 / 编辑：管理员与商务。 */
export const DEAL_EDITOR_ROLES = [ADMIN_ROLE, SALES_ROLE] as const;

/** 商务单阶段推进：管理员、商务，财务只做报价复核（由后端再限制为「待审核 → 已确认 / 退回报价中」）。 */
export const DEAL_STAGE_ROLES = [ADMIN_ROLE, SALES_ROLE, FINANCE_ROLE] as const;

/** 内容计划新建 / 编辑 / 状态推进：管理员、策划、运营、剪辑。 */
export const PLAN_EDITOR_ROLES = [ADMIN_ROLE, PLANNER_ROLE, OPERATIONS_ROLE, EDITOR_ROLE] as const;

/** 人工待办：管理员与运营可新建；所有业务角色都能勾选完成。 */
export const TODO_CREATOR_ROLES = [ADMIN_ROLE, OPERATIONS_ROLE] as const;

export const canEditDeals = (roles: string[] | null | undefined): boolean =>
  hasAnyRole(roles, DEAL_EDITOR_ROLES);

export const canAdvanceDeal = (roles: string[] | null | undefined): boolean =>
  hasAnyRole(roles, DEAL_STAGE_ROLES);

export const canEditPlans = (roles: string[] | null | undefined): boolean =>
  hasAnyRole(roles, PLAN_EDITOR_ROLES);

export const canCreateTodos = (roles: string[] | null | undefined): boolean =>
  hasAnyRole(roles, TODO_CREATOR_ROLES);

/**
 * 财务单独持有时只能复核报价，不能新建 / 编辑商务单。
 * 用于界面提示「你可以复核哪一步」，与后端 UpdateDealStage 的判断保持一致。
 */
export const isFinanceOnly = (roles: string[] | null | undefined): boolean => {
  const list = roles ?? [];
  const has = (role: string) => list.includes(role);
  return has(FINANCE_ROLE) && !has(ADMIN_ROLE) && !has(SALES_ROLE);
};
