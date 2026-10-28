import { describe, expect, it } from 'vitest';
import {
  ADMIN_ONLY_ROLES,
  ACCOUNT_MANAGER_ROLES,
  canEditThresholds,
  canManageAccounts,
  formatRoleList,
  readOnlyNotice,
} from './permissions';
import { ADMIN_ROLE, ANALYST_ROLE, OPERATIONS_ROLE, SALES_ROLE } from './roles';

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
