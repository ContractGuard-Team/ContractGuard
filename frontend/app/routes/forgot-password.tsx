import { useEffect, useState, type FormEvent } from "react";
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

// FR-AUTH-015: 3 minutes before asking again. The time of the last request is kept in localStorage, so
// the countdown survives a reload and shows the same in another tab of the browser. The server keeps
// its own limit (FR-AUTH-014); this only tells the user when another email can come.
const cooldownMs = 3 * 60 * 1000;
const storageKey = "cg.passwordResetRequestedAt";

function readSentAt(): number | null {
  try {
    const value = Number(window.localStorage.getItem(storageKey));
    return value > 0 ? value : null;
  } catch {
    return null;
  }
}

function useCountdown() {
  const [sentAt, setSentAt] = useState<number | null>(readSentAt);
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    const onStorage = () => setSentAt(readSentAt());
    window.addEventListener("storage", onStorage);
    return () => {
      window.clearInterval(timer);
      window.removeEventListener("storage", onStorage);
    };
  }, []);

  function markSent() {
    const at = Date.now();
    try {
      window.localStorage.setItem(storageKey, String(at));
    } catch {
      // Private mode: the countdown lasts for this page only.
    }

    setSentAt(at);
    setNow(at);
  }

  const remaining = sentAt === null ? 0 : Math.max(0, sentAt + cooldownMs - now);
  return { remainingSeconds: Math.ceil(remaining / 1000), sentBefore: sentAt !== null, markSent };
}

export default function ForgotPassword() {
  const [email, setEmail] = useState("");
  const [sent, setSent] = useState(false);
  const request = useAction();
  const { remainingSeconds, markSent } = useCountdown();
  const waiting = remainingSeconds > 0;
  const clock = `${Math.floor(remainingSeconds / 60)}:${String(remainingSeconds % 60).padStart(2, "0")}`;

  function onSubmit(event: FormEvent) {
    event.preventDefault();
    void request.run(async () => {
      await apiPost("/auth/forgot-password", { email });
      markSent();
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
      <form onSubmit={onSubmit} className="flex flex-col gap-4">
        {sent ? (
          <Notice tone="info">Nếu email đã đăng ký, hướng dẫn đặt lại mật khẩu đã được gửi. Liên kết có hiệu lực trong 10 phút.</Notice>
        ) : (
          <p className="text-ink-secondary">Nhập email đã đăng ký để nhận liên kết đặt lại mật khẩu.</p>
        )}
        {request.error !== null && <Notice tone="error">{errorMessage(request.error)}</Notice>}
        <TextField label="Email" type="email" name="email" autoComplete="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
        <Button type="submit" loading={request.pending} disabled={waiting}>
          {sent || waiting ? "Gửi lại liên kết" : "Gửi liên kết"}
        </Button>
        {waiting && (
          <p role="timer" aria-live="off" className="text-caption text-ink-secondary">
            Có thể gửi lại sau <span className="tabular-nums">{clock}</span>.
          </p>
        )}
      </form>
    </AuthPage>
  );
}
