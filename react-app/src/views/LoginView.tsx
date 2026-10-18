import { type FormEvent, useState } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import AuthManager from '@/api/AuthManager';
import useAuthStore from '@/auth/AuthStore';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

type LoginLocationState = { from?: string } | null;

/**
 * 登录页（M0）。当前只提供账号密码登录，登录成功写入 AuthStore 后回到来源路由。
 * 飞书免登接入时，在此并列一个「使用飞书登录」入口，二者共用同一个换 JWT 的出口。
 */
const LoginView = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const session = useAuthStore((state) => state.session);
  const accessToken = useAuthStore((state) => state.accessToken);
  const setAuth = useAuthStore((state) => state.setAuth);

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const from = (location.state as LoginLocationState)?.from || '/';

  if (session && accessToken) {
    return <Navigate to={from} replace />;
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (submitting) return;

    setError(null);

    if (!email.trim() || !password) {
      setError('请填写邮箱与密码。');
      return;
    }

    setSubmitting(true);
    try {
      const result = await AuthManager.Login({ Email: email.trim(), Password: password });

      if (!result.IsSuccess || !result.AccessToken || !result.RefreshToken || !result.Session) {
        setError(result.ErrorMessage?.trim() || '登录失败，请检查邮箱与密码。');
        return;
      }

      setAuth(result.AccessToken, result.RefreshToken, result.Session);
      navigate(from, { replace: true });
    } catch (e: any) {
      const message = e?.message === 'Unauthorized' ? '邮箱或密码不正确。' : '无法连接服务，请确认后端已启动后重试。';
      setError(message);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <main className="min-h-screen bg-background text-foreground">
      <div className="mx-auto grid min-h-screen w-full max-w-[1240px] items-center gap-10 px-5 py-10 lg:grid-cols-[1.05fr_0.95fr] lg:gap-16 lg:px-10">
        <section className="space-y-6">
          <div className="flex items-center gap-3 text-[11px] font-medium uppercase tracking-[0.24em] text-muted-foreground">
            <span className="grid size-7 place-items-center rounded-lg bg-primary text-[11px] font-bold leading-none text-primary-foreground">
              M
            </span>
            <span>MediaAI</span>
          </div>

          <h1 className="max-w-lg text-3xl font-semibold leading-[1.15] tracking-tight sm:text-4xl">
            把个人自媒体矩阵
            <br />
            放进同一块屏幕。
          </h1>

          <p className="max-w-md text-sm leading-7 text-muted-foreground">
            平台数据、爆款机会、限流风险，连同商务合作与内容排期收敛到一处。登录后按你的角色进入对应视图。
          </p>

          <ul className="max-w-md space-y-2 border-t border-border/60 pt-5 text-sm text-muted-foreground">
            <li>· 每个数字都能回溯到「谁、哪个平台、哪一天」</li>
            <li>· 角色决定默认视图与可写范围</li>
            <li>· 未加入业务用户组的账号只读</li>
          </ul>
        </section>

        <section className="w-full max-w-md rounded-3xl border border-border bg-card p-6 shadow-sm sm:p-8 lg:justify-self-end">
          <h2 className="text-lg font-semibold tracking-tight">登录</h2>
          <p className="mt-1 text-sm text-muted-foreground">使用账号密码登录，角色由账号决定。</p>

          <form onSubmit={handleSubmit} className="mt-6 space-y-4" noValidate>
            <div className="space-y-2">
              <Label htmlFor="login-email">邮箱</Label>
              <Input
                id="login-email"
                name="email"
                type="email"
                autoComplete="username"
                placeholder="you@example.com"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                disabled={submitting}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="login-password">密码</Label>
              <Input
                id="login-password"
                name="password"
                type="password"
                autoComplete="current-password"
                placeholder="••••••••"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                disabled={submitting}
              />
            </div>

            {error ? (
              <div
                role="alert"
                className="rounded-lg border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm leading-6 text-destructive"
              >
                {error}
              </div>
            ) : null}

            <Button type="submit" className="w-full" disabled={submitting}>
              {submitting ? '登录中…' : '登录'}
            </Button>
          </form>

          <div className="mt-6 space-y-2 border-t border-border/60 pt-4 text-xs leading-6 text-muted-foreground">
            <p>飞书免登待接入。接入后此处并列「使用飞书登录」，与账号登录共用同一签发出口。</p>
            <p>
              设计与指标口径见
              <a
                className="ml-1 underline underline-offset-2 hover:text-foreground"
                href="https://gsspai.github.io/mediaai/"
                target="_blank"
                rel="noreferrer"
              >
                产品文档
              </a>
              。
            </p>
          </div>
        </section>
      </div>
    </main>
  );
};

export default LoginView;
