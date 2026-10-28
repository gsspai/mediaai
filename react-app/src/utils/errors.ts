/**
 * 网络层与后端失败文案的统一出口。
 * 后端 403 会带回中文原因（例如「需要角色：admin」），这类信息要原样展示；
 * 只有 fetch 自身的失败（Failed to fetch / Load failed）才回落到页面自己的兜底文案。
 */

const GENERIC_NETWORK_MESSAGES = new Set([
  'Failed to fetch',
  'Load failed',
  'NetworkError when attempting to fetch resource.',
  'fetch failed',
]);

export const resolveErrorText = (error: unknown, fallback: string): string => {
  const message = error instanceof Error ? error.message : typeof error === 'string' ? error : '';
  const trimmed = message.trim();

  if (!trimmed || GENERIC_NETWORK_MESSAGES.has(trimmed)) {
    return fallback;
  }

  return trimmed;
};
