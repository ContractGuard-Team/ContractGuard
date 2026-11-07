import { useState, type FormEvent } from "react";
import { Link } from "react-router";

import { AuthPage, linkClass } from "~/components/AuthPage";
import { Button } from "~/components/Button";
import { TextField } from "~/components/Field";
import { Notice } from "~/components/Notice";
import { ApiError, apiPost } from "~/lib/api";
import { errorMessage } from "~/lib/errors";
import { useAction } from "~/lib/useAction";

import type { Route } from "./+types/register";

export function meta(_: Route.MetaArgs) {
  return [{ title: "Đăng ký · ContractGuard" }];
}

// FR-AUTH-001..005. After this step the account exists but cannot log in until the email is verified.
export default function Register() {
  const [displayName, setDisplayName] = useState("");
  const [nameError, setNameError] = useState<string>();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [sentTo, setSentTo] = useState<string | null>(null);
  const [sendFailed, setSendFailed] = useState(false);
  const register = useAction();
  const resend = useAction();

  const passwordError = register.error instanceof ApiError && register.error.code === "ERR-AUTH-002" ? errorMessage(register.error) : undefined;

  function onSubmit(event: FormEvent) {
    event.preventDefault();

    // "required" lets a name of only spaces through.
    setNameError(displayName.trim() ? undefined : "Hãy nhập họ và tên.");
    if (!displayName.trim()) {
      return;
    }

    setSendFailed(false);
    void register.run(async () => {
      try {
        await apiPost("/auth/register", { displayName, email, password });
      } catch (error) {
        // ERR-INTG-001: the account was created, only the email is missing.
        if (error instanceof ApiError && error.code === "ERR-INTG-001") {
          setSendFailed(true);
        }

        throw error;
      }

      setSentTo(email);
    });
  }

  function onResend() {
    void resend.run(async () => {
      await apiPost("/auth/resend-verification", { email });
      register.clearError();
      setSendFailed(false);
      setSentTo(email);
    });
  }

  if (sentTo) {
    return (
      <AuthPage title="Kiểm tra email của bạn">
        <p className="text-ink-secondary">
          Chúng tôi đã gửi liên kết xác thực tới <span className="font-medium break-all text-ink">{sentTo}</span>. Liên kết có hiệu lực trong 10 phút.
        </p>
        {resend.error !== null && (
          <div className="mt-4">
            <Notice tone="error">{errorMessage(resend.error)}</Notice>
          </div>
        )}
        <Button variant="secondary" className="mt-4" loading={resend.pending} onClick={onResend}>
          Gửi lại email
        </Button>
      </AuthPage>
    );
  }

  return (
    <AuthPage
      title="Đăng ký tài khoản"
      footer={
        <>
          Đã có tài khoản?{" "}
          <Link to="/login" className={linkClass}>
            Đăng nhập
          </Link>
        </>
      }
    >
      <form onSubmit={onSubmit} className="flex flex-col gap-4">
        <TextField
          label="Họ và tên"
          name="name"
          autoComplete="name"
          required
          maxLength={100}
          error={nameError}
          value={displayName}
          onChange={(e) => setDisplayName(e.target.value)}
        />
        <TextField label="Email" type="email" name="email" autoComplete="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
        <TextField
          label="Mật khẩu"
          type="password"
          name="new-password"
          autoComplete="new-password"
          required
          hint="Ít nhất 8 ký tự, có cả chữ và số."
          error={passwordError}
          value={password}
          onChange={(e) => setPassword(e.target.value)}
        />
        {register.error !== null && !passwordError && (
          <Notice tone="error">
            <p>{errorMessage(register.error)}</p>
            {sendFailed && (
              <Button variant="secondary" size="sm" className="mt-2" loading={resend.pending} onClick={onResend}>
                Gửi lại email
              </Button>
            )}
          </Notice>
        )}
        <Button type="submit" loading={register.pending}>
          Đăng ký
        </Button>
      </form>
    </AuthPage>
  );
}
