/**
 * 平台标识的唯一前端来源，与后端 PlatformAccount.Platform 取值保持一致。
 * 后端只存标识（douyin），中文名在展示层映射，避免一个平台出现两套写法。
 */
export const PLATFORM_LABELS: Record<string, string> = {
  douyin: '抖音',
  xiaohongshu: '小红书',
  'wechat-official': '公众号',
  'wechat-video': '视频号',
  weibo: '微博',
  toutiao: '头条',
  bilibili: 'B站',
};

/** 未知平台原样返回，避免出现空白标签 */
export const getPlatformLabel = (platform: string): string => PLATFORM_LABELS[platform] ?? platform;
