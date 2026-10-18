import { describe, expect, it } from 'vitest';
import {
  ADMIN_ROLE,
  BUSINESS_ROLES,
  Roles,
  SALES_ROLE,
  VISITOR_LABEL,
  getRoleLabels,
  hasAnyRole,
  hasRole,
  isBusinessUser,
} from './roles';

describe('getRoleLabels', () => {
  it('未加入业务用户组时显示访客', () => {
    expect(getRoleLabels([])).toEqual([VISITOR_LABEL]);
    expect(getRoleLabels(null)).toEqual([VISITOR_LABEL]);
    expect(getRoleLabels(undefined)).toEqual([VISITOR_LABEL]);
  });

  it('业务角色映射为产品文档中的界面名称', () => {
    expect(getRoleLabels([ADMIN_ROLE, SALES_ROLE])).toEqual(['管理员', '商务']);
  });

  it('未知角色原样返回，避免出现空白徽章', () => {
    expect(getRoleLabels(['custom_role'])).toEqual(['custom_role']);
  });

  it('技术角色不显示，只剩 user 时按访客处理', () => {
    expect(getRoleLabels(['user'])).toEqual([VISITOR_LABEL]);
    expect(getRoleLabels(['authenticated'])).toEqual([VISITOR_LABEL]);
    expect(getRoleLabels(['user', ADMIN_ROLE])).toEqual(['管理员']);
  });
});

describe('角色清单', () => {
  it('Roles 以业务角色开头，与 BUSINESS_ROLES 一致', () => {
    expect(Roles.slice(0, BUSINESS_ROLES.length)).toEqual([...BUSINESS_ROLES]);
    expect(Roles.length).toBeGreaterThan(BUSINESS_ROLES.length);
  });
});

describe('角色判断', () => {
  it('hasRole 精确匹配', () => {
    expect(hasRole([ADMIN_ROLE], ADMIN_ROLE)).toBe(true);
    expect(hasRole(['user'], ADMIN_ROLE)).toBe(false);
    expect(hasRole(null, ADMIN_ROLE)).toBe(false);
  });

  it('hasAnyRole 命中任一即通过', () => {
    expect(hasAnyRole([SALES_ROLE], [...BUSINESS_ROLES])).toBe(true);
    expect(hasAnyRole(['user'], [...BUSINESS_ROLES])).toBe(false);
  });

  it('只持有 user 的账号按访客只读处理', () => {
    expect(isBusinessUser([ADMIN_ROLE])).toBe(true);
    expect(isBusinessUser(['user'])).toBe(false);
    expect(isBusinessUser([])).toBe(false);
  });
});
