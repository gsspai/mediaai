import { describe, expect, it } from 'vitest';
import {
  ADMIN_ONLY_ROLES,
  ACCOUNT_MANAGER_ROLES,
  canAdvanceDeal,
  canCreateTodos,
  canEditDeals,
  canEditPlans,
  canEditThresholds,
  canManageAccounts,
  formatRoleList,
  isFinanceOnly,
  readOnlyNotice,
} from './permissions';
import { ADMIN_ROLE, ANALYST_ROLE, EDITOR_ROLE, FINANCE_ROLE, OPERATIONS_ROLE, PLANNER_ROLE, SALES_ROLE } from './roles';

describe('平台账号连接权限', () => {
  it('管理员与运营可以维护账号与录入指标', () => {
    expect(canManageAccounts([ADMIN_ROLE])).toBe(true);
    expect(canManageAccounts([OPERATIONS_ROLE])).toBe(true);
    expect(ACCOUNT_MANAGER_ROLES).toEqual([ADMIN_ROLE, OPERATIONS_ROLE]);
  });

  it('其他业务角色与访客一律只读', () => {
    expect(canManageAccounts([SALES_ROLE])).toBe(false);
    expect(canManageAccounts([ANALYST_ROLE])).toBe(false);
    expect(canManageAccounts(['user'])).toBe(false);
    expect(canManageAccounts([])).toBe(false);
    expect(canManageAccounts(null)).toBe(false);
  });
});

describe('判定阈值权限', () => {
  it('只有管理员能改阈值', () => {
    expect(canEditThresholds([ADMIN_ROLE])).toBe(true);
    expect(canEditThresholds([OPERATIONS_ROLE])).toBe(false);
    expect(canEditThresholds(['user'])).toBe(false);
    expect(ADMIN_ONLY_ROLES).toEqual([ADMIN_ROLE]);
  });
});

describe('提示文案', () => {
  it('角色标识翻译为界面名称', () => {
    expect(formatRoleList([ADMIN_ROLE, OPERATIONS_ROLE])).toBe('管理员 / 运营');
  });

  it('只读说明里直接写清需要哪些角色', () => {
    const notice = readOnlyNotice();
    expect(notice).toContain('访客');
    expect(notice).toContain('管理员 / 运营');
  });
});

describe('商务单与内容计划权限', () => {
  it('商务单编辑只开给管理员与商务，财务只能推进阶段', () => {
    expect(canEditDeals([ADMIN_ROLE])).toBe(true);
    expect(canEditDeals([SALES_ROLE])).toBe(true);
    expect(canEditDeals([FINANCE_ROLE])).toBe(false);
    expect(canAdvanceDeal([FINANCE_ROLE])).toBe(true);
    expect(canEditDeals(['user'])).toBe(false);
  });

  it('内容计划编辑开给管理员、策划、运营、剪辑', () => {
    expect(canEditPlans([ADMIN_ROLE])).toBe(true);
    expect(canEditPlans([PLANNER_ROLE])).toBe(true);
    expect(canEditPlans([OPERATIONS_ROLE])).toBe(true);
    expect(canEditPlans([EDITOR_ROLE])).toBe(true);
    expect(canEditPlans([SALES_ROLE])).toBe(false);
    expect(canEditPlans([ANALYST_ROLE])).toBe(false);
  });

  it('人工待办只有管理员与运营能建', () => {
    expect(canCreateTodos([ADMIN_ROLE])).toBe(true);
    expect(canCreateTodos([OPERATIONS_ROLE])).toBe(true);
    expect(canCreateTodos([SALES_ROLE])).toBe(false);
  });

  it('财务单独持有时按报价复核处理，叠加商务则等同商务', () => {
    expect(isFinanceOnly([FINANCE_ROLE])).toBe(true);
    expect(isFinanceOnly([FINANCE_ROLE, SALES_ROLE])).toBe(false);
    expect(isFinanceOnly([FINANCE_ROLE, ADMIN_ROLE])).toBe(false);
    expect(isFinanceOnly([])).toBe(false);
  });
});
