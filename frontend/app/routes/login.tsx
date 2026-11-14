import { useState, type FormEvent } from "react";
import { Link, useNavigate, useSearchParams } from "react-router";

import { AuthPage, linkClass } from "~/components/AuthPage";
import { Button } from "~/components/Button";
import { TextField } from "~/components/Field";
import { Notice } from "~/components/Notice";
import { ApiError, apiPost } from "~/lib/api";
import { errorMessage } from "~/lib/errors";
import { useAction } from "~/lib/useAction";
import { safeNext } from "~/lib/workspace";

import type { Route } from "./+types/login";

export function meta(_: Route.MetaArgs) {
  return [{ title: "Đăng nhập · ContractGuard" }];
}

const notices: Record<string, string> = {
  verified: "Email đã được xác thực. Bạn có thể đăng nhập.",
  reset: "Mật khẩu đã được đặt lại. Hãy đăng nhập bằng mật khẩu mới.",
};

// FR-AUTH-006..008.
export default function Login() {
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [resent, setResent] = useState(false);
  const login = useAction();
  const resend = useAction();

  const notice = notices[params.get("notice") ?? ""];
  const code = login.error instanceof ApiError ? login.error.code : undefined;

  function onSubmit(event: FormEvent) {
    event.preventDefault();
    setResent(false);
    void login.run(async () => {
      await apiPost("/auth/login", { email, password });
      await navigate(safeNext(params.get("next")));
    });
  }

  function onResend() {
    void resend.run(async () => {
      await apiPost("/auth/resend-verification", { email });
      login.clearError();
      setResent(true);
    });
  }

  return (
    <AuthPage
      title="Đăng nhập"
      footer={
        <>
          Chưa có tài khoản?{" "}
          <Link to="/register" className={linkClass}>
            Đăng ký
          </Link>
        </>
      }
    >
      <form onSubmit={onSubmit} className="flex flex-col gap-4">
        {notice && !login.error && !resent && <Notice tone="info">{notice}</Notice>}
        {resent && <Notice tone="info">Đã gửi lại email xác thực. Liên kết có hiệu lực trong 10 phút.</Notice>}
        {login.error !== null && (
          <Notice tone="error">
            <p>{errorMessage(login.error)}</p>
            {code === "ERR-GEN-EMAIL-UNCONFIRMED" && (
              <Button variant="secondary" size="sm" className="mt-2" loading={resend.pending} onClick={onResend}>
                Gửi lại email xác thực
              </Button>
            )}
          </Notice>
        )}
        {resend.error !== null && <Notice tone="error">{errorMessage(resend.error)}</Notice>}

        <TextField label="Email" type="email" name="email" autoComplete="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
        <TextField
          label="Mật khẩu"
          type="password"
          name="password"
          autoComplete="current-password"
          required
          value={password}
          onChange={(e) => setPassword(e.target.value)}
        />
        <Button type="submit" loading={login.pending}>
          Đăng nhập
        </Button>
        <Link to="/forgot-password" className={linkClass}>
          Quên mật khẩu?
        </Link>
      </form>
    </AuthPage>
  );
}
