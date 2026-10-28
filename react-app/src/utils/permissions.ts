import { ADMIN_ROLE, OPERATIONS_ROLE, ROLE_LABELS, hasAnyRole } from './roles';

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
