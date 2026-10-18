import { afterEach, vi } from 'vitest';

// vite.config.ts 的 test.setupFiles 指向本文件；缺它时 `npm test` 直接失败。
// jsdom 未实现 matchMedia，部分 UI 组件依赖它。
if (!window.matchMedia) {
  window.matchMedia = ((query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  })) as unknown as typeof window.matchMedia;
}

afterEach(() => {
  vi.clearAllMocks();
  localStorage.clear();
});
