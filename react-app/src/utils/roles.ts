/**
 * 角色清单的唯一前端来源。
 * 与后端 Services/App.Api/Utilities/Roles/Roles.cs 保持一致；改一处必须改另一处。
 * 显示名与「产品文档 · 用户与角色」一致。
 */

export const ADMIN_ROLE = 'admin';
export const SALES_ROLE = 'sales';
export const FINANCE_ROLE = 'finance';
export const PLANNER_ROLE = 'planner';
export const OPERATIONS_ROLE = 'operations';
export const EDITOR_ROLE = 'editor';
export const ANALYST_ROLE = 'analyst';
export const USER_ROLE = 'user';
export const AUTHENTICATED_ROLE = 'authenticated';

/** 业务角色：可读写业务数据；不持有任一业务角色的账号按「访客」只读处理。 */
export const BUSINESS_ROLES = [
  ADMIN_ROLE,
  SALES_ROLE,
  FINANCE_ROLE,
  PLANNER_ROLE,
  OPERATIONS_ROLE,
  EDITOR_ROLE,
  ANALYST_ROLE,
] as const;

export const Roles = [...BUSINESS_ROLES, USER_ROLE, AUTHENTICATED_ROLE];

/** 角色标识 → 界面名称 */
export const ROLE_LABELS: Record<string, string> = {
  [ADMIN_ROLE]: '管理员',
  [SALES_ROLE]: '商务',
  [FINANCE_ROLE]: '财务',
  [PLANNER_ROLE]: '策划',
  [OPERATIONS_ROLE]: '运营',
  [EDITOR_ROLE]: '剪辑',
  [ANALYST_ROLE]: '分析',
};

/** 未加入任何业务用户组时的显示名 */
export const VISITOR_LABEL = '访客';

/**
 * 只展示业务角色。`user` / `authenticated` 是技术角色，不面向业务；
 * 未知的自定义角色原样返回，避免界面上出现空白徽章。
 */
export const getRoleLabels = (roles: string[] | null | undefined): string[] => {
  const visible = (roles ?? []).filter((role) => role !== USER_ROLE && role !== AUTHENTICATED_ROLE);
  if (visible.length === 0) return [VISITOR_LABEL];
  return visible.map((role) => ROLE_LABELS[role] ?? role);
};

export const hasRole = (roles: string[] | null | undefined, role: string): boolean =>
  (roles ?? []).includes(role);

export const hasAnyRole = (roles: string[] | null | undefined, required: readonly string[]): boolean =>
  (roles ?? []).some((role) => required.includes(role));

/** 持有任一业务角色即可写；否则视为访客只读 */
export const isBusinessUser = (roles: string[] | null | undefined): boolean =>
  hasAnyRole(roles, BUSINESS_ROLES);
