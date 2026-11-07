import { useState, type FormEvent } from "react";
import { Link, useNavigate, useSearchParams } from "react-router";

import { AuthPage, linkClass } from "~/components/AuthPage";
import { Button } from "~/components/Button";
import { TextField } from "~/components/Field";
import { Notice } from "~/components/Notice";
import { ApiError, apiPost } from "~/lib/api";
import { errorMessage } from "~/lib/errors";
import { useAction } from "~/lib/useAction";

import type { Route } from "./+types/reset-password";

export function meta(_: Route.MetaArgs) {
  return [{ title: "Đặt lại mật khẩu · ContractGuard" }];
}

// FR-AUTH-010.
export default function ResetPassword() {
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const [newPassword, setNewPassword] = useState("");
  const reset = useAction();

  const code = reset.error instanceof ApiError ? reset.error.code : undefined;

  function onSubmit(event: FormEvent) {
    event.preventDefault();
    void reset.run(async () => {
      await apiPost("/auth/reset-password", { userId: params.get("userId"), token: params.get("token"), newPassword });
      await navigate("/login?notice=reset");
    });
  }

  return (
    <AuthPage title="Đặt mật khẩu mới">
      <form onSubmit={onSubmit} className="flex flex-col gap-4">
        {reset.error !== null && code !== "ERR-AUTH-002" && (
          <Notice tone="error">
            <p>{errorMessage(reset.error)}</p>
            {(code === "ERR-AUTH-004" || code === "ERR-GEN-VALIDATION") && (
              <Link to="/forgot-password" className={linkClass}>
                Yêu cầu liên kết mới
              </Link>
            )}
          </Notice>
        )}
        <TextField
          label="Mật khẩu mới"
          type="password"
          name="new-password"
          autoComplete="new-password"
          required
          hint="Ít nhất 8 ký tự, có cả chữ và số."
          error={code === "ERR-AUTH-002" ? errorMessage(reset.error) : undefined}
          value={newPassword}
          onChange={(e) => setNewPassword(e.target.value)}
        />
        <Button type="submit" loading={reset.pending}>
          Đặt mật khẩu
        </Button>
      </form>
    </AuthPage>
  );
}
