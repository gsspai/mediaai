import SectionHeader from '@/components/dashboard/SectionHeader';
import { cn } from '@/lib/utils';
import type { ComponentType, ReactNode } from 'react';

type PanelProps = {
  children: ReactNode;
  className?: string;
};

/** 统一面板：圆角、描边、背景、阴影都由 `surface-panel` 定义，页面不再自写卡片样式。 */
const Panel = ({ children, className }: PanelProps) => (
  <section className={cn('surface-panel overflow-hidden', className)}>{children}</section>
);

type PanelHeaderProps = {
  eyebrow?: string;
  title: string;
  description?: string;
  actions?: ReactNode;
  icon?: ComponentType<{ className?: string }>;
  className?: string;
};

/**
 * 面板头：左右内边距固定 24px，与 PanelBody、表格首列完全对齐。
 * 不要再给单页面板叠加 lg:px-7，否则标题会比正文和表格往右错开 4px。
 */
const PanelHeader = ({ eyebrow, title, description, actions, icon, className }: PanelHeaderProps) => (
  <header className={cn('border-b border-border/60 px-6 py-5', className)}>
    <SectionHeader eyebrow={eyebrow} title={title} description={description} actions={actions} icon={icon} titleAs="h2" />
  </header>
);

/** 正文内边距与 PanelHeader、表格首列保持一致（都是 24px），窄屏也不例外。 */
const PanelBody = ({ children, className }: PanelProps) => (
  <div className={cn('p-6', className)}>{children}</div>
);

export { Panel, PanelHeader, PanelBody };
export default Panel;
