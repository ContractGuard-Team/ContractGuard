import { useState, type FormEvent } from "react";
import { Link } from "react-router";

import { AuthPage, linkClass } from "~/components/AuthPage";
import { Button } from "~/components/Button";
import { TextField } from "~/components/Field";
import { Notice } from "~/components/Notice";
import { apiPost } from "~/lib/api";
import { errorMessage } from "~/lib/errors";
import { useAction } from "~/lib/useAction";

import type { Route } from "./+types/forgot-password";

export function meta(_: Route.MetaArgs) {
  return [{ title: "Quên mật khẩu · ContractGuard" }];
}

// FR-AUTH-009. The countdown before asking again (FR-AUTH-015) comes in P5-07.
export default function ForgotPassword() {
  const [email, setEmail] = useState("");
  const [sent, setSent] = useState(false);
  const request = useAction();

  function onSubmit(event: FormEvent) {
    event.preventDefault();
    void request.run(async () => {
      await apiPost("/auth/forgot-password", { email });
      setSent(true);
    });
  }

  return (
    <AuthPage
      title="Quên mật khẩu"
      footer={
        <Link to="/login" className={linkClass}>
          Về trang đăng nhập
        </Link>
      }
    >
      {sent ? (
        <Notice tone="info">Nếu email đã đăng ký, hướng dẫn đặt lại mật khẩu đã được gửi. Liên kết có hiệu lực trong 10 phút.</Notice>
      ) : (
        <form onSubmit={onSubmit} className="flex flex-col gap-4">
          <p className="text-ink-secondary">Nhập email đã đăng ký để nhận liên kết đặt lại mật khẩu.</p>
          {request.error !== null && <Notice tone="error">{errorMessage(request.error)}</Notice>}
          <TextField label="Email" type="email" name="email" autoComplete="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
          <Button type="submit" loading={request.pending}>
            Gửi liên kết
          </Button>
        </form>
      )}
    </AuthPage>
  );
}
