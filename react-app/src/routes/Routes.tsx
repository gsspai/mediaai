import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import ExampleView from '@/views/ExampleView';
import LoginView from '@/views/LoginView';
import NotFoundView from '@/views/NotFoundView';
import ProtectedRoute from '@/routes/ProtectedRoute';
import UnauthorizedView from '@/views/UnauthorizedView';
import WorkspaceDetailPlaceholderView from '@/views/WorkspaceDetailPlaceholderView';

/**
 * 路由表：`/login` 与 `/unauthorized` 公开，其余页面统一挂在 ProtectedRoute 之下。
 * ProtectedRoute 在无会话时重定向到 `/login` 并带上来源路径，登录成功后回到原处。
 */
const AppRoutes = () => {
  return (
    <Router>
      <Routes>
        <Route path="/login" element={<LoginView />} />
        <Route path="/unauthorized" element={<UnauthorizedView />} />

        <Route element={<ProtectedRoute />}>
          <Route index element={<ExampleView />} />
          <Route path="/business" element={<WorkspaceDetailPlaceholderView />} />
          <Route path="/business/:dealId" element={<WorkspaceDetailPlaceholderView />} />
          <Route path="/plans/:planId" element={<WorkspaceDetailPlaceholderView />} />
          <Route path="/connections/:platform" element={<WorkspaceDetailPlaceholderView />} />
          <Route path="/analysis/:viewId" element={<WorkspaceDetailPlaceholderView />} />
        </Route>

        <Route path="*" element={<NotFoundView />} />
      </Routes>
    </Router>
  );
};

export default AppRoutes;
