import { useState, type FormEvent } from "react";
import { Link, redirect } from "react-router";

import { AuthPage, linkClass } from "~/components/AuthPage";
import { Button } from "~/components/Button";
import { TextField } from "~/components/Field";
import { Notice } from "~/components/Notice";
import { apiPost } from "~/lib/api";
import { errorMessage } from "~/lib/errors";
import { useAction } from "~/lib/useAction";

import type { Route } from "./+types/verify-email";

export function meta(_: Route.MetaArgs) {
  return [{ title: "Xác thực email · ContractGuard" }];
}

// FR-AUTH-004: opening the link is the whole action. When it worked the user goes on to log in;
// this page itself only shows for a link that no longer works.
export async function clientLoader({ request }: Route.ClientLoaderArgs) {
  const params = new URL(request.url).searchParams;
  try {
    await apiPost("/auth/verify-email", { userId: params.get("userId"), token: params.get("token") });
  } catch {
    return null;
  }

  return redirect("/login?notice=verified");
}

export default function VerifyEmail() {
  const [email, setEmail] = useState("");
  const [sent, setSent] = useState(false);
  const resend = useAction();

  function onSubmit(event: FormEvent) {
    event.preventDefault();
    void resend.run(async () => {
      await apiPost("/auth/resend-verification", { email });
      setSent(true);
    });
  }

  return (
    <AuthPage
      title="Liên kết không còn hiệu lực"
      footer={
        <Link to="/login" className={linkClass}>
          Về trang đăng nhập
        </Link>
      }
    >
      {sent ? (
        <Notice tone="info">Nếu email này đang chờ xác thực, liên kết mới đã được gửi. Liên kết có hiệu lực trong 10 phút.</Notice>
      ) : (
        <form onSubmit={onSubmit} className="flex flex-col gap-4">
          <p className="text-ink-secondary">Liên kết xác thực chỉ có hiệu lực trong 10 phút. Nhập email của bạn để nhận liên kết mới.</p>
          {resend.error !== null && <Notice tone="error">{errorMessage(resend.error)}</Notice>}
          <TextField label="Email" type="email" autoComplete="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
          <Button type="submit" loading={resend.pending}>
            Gửi liên kết mới
          </Button>
        </form>
      )}
    </AuthPage>
  );
}
