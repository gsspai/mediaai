import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import BusinessView from '@/views/BusinessView';
import ConnectionsView from '@/views/ConnectionsView';
import DealDetailView from '@/views/DealDetailView';
import ExampleView from '@/views/ExampleView';
import LoginView from '@/views/LoginView';
import NotFoundView from '@/views/NotFoundView';
import PlanDetailView from '@/views/PlanDetailView';
import PlansView from '@/views/PlansView';
import ProtectedRoute from '@/routes/ProtectedRoute';
import SettingsView from '@/views/SettingsView';
import UnauthorizedView from '@/views/UnauthorizedView';
import WorkspaceDetailPlaceholderView from '@/views/WorkspaceDetailPlaceholderView';
import { ADMIN_ROLE } from '@/utils/roles';

/**
 * 路由表：`/login` 与 `/unauthorized` 公开，其余页面统一挂在 ProtectedRoute 之下。
 * ProtectedRoute 在无会话时重定向到 `/login` 并带上来源路径，登录成功后回到原处。
 *
 * `/analysis/:viewId` 已移除：产品文档只有 6 个页面，这个路由没有对应页面，
 * 数据分析并入首页的信号区块与后续的报告页。
 */
const AppRoutes = () => {
  return (
    <Router>
      <Routes>
        <Route path="/login" element={<LoginView />} />
        <Route path="/unauthorized" element={<UnauthorizedView />} />

        <Route element={<ProtectedRoute />}>
          <Route index element={<ExampleView />} />
          <Route path="/business" element={<BusinessView />} />
          <Route path="/business/:dealId" element={<DealDetailView />} />
          <Route path="/plans" element={<PlansView />} />
          <Route path="/plans/:planId" element={<PlanDetailView />} />
          <Route path="/connections" element={<ConnectionsView />} />
          <Route path="/connections/:platform" element={<WorkspaceDetailPlaceholderView />} />
        </Route>

        {/* 判定阈值属于管理层面板，按权限矩阵只对管理员开放；其他角色落到 /unauthorized。 */}
        <Route element={<ProtectedRoute roles={[ADMIN_ROLE]} />}>
          <Route path="/settings" element={<SettingsView />} />
        </Route>

        <Route path="*" element={<NotFoundView />} />
      </Routes>
    </Router>
  );
};

export default AppRoutes;
