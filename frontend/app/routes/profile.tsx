import { useState, type FormEvent } from "react";
import { useRevalidator } from "react-router";

import { Button } from "~/components/Button";
import { SelectField, TextField } from "~/components/Field";
import { Notice } from "~/components/Notice";
import { ApiError, apiLoad, apiPost, apiPut, type Schemas } from "~/lib/api";
import { errorMessage } from "~/lib/errors";
import { useAction } from "~/lib/useAction";

import type { Route } from "./+types/profile";

export function meta(_: Route.MetaArgs) {
  return [{ title: "Hồ sơ · ContractGuard" }];
}

export async function clientLoader() {
  return { me: await apiLoad<Schemas["MeResponse"]>("/me") };
}

// FR-AUTH-016, FR-AUTH-017 (CR-AUTH-001): the user's own name, phone, result language and password.
export default function Profile({ loaderData }: Route.ComponentProps) {
  const { me } = loaderData;
  return (
    <div className="flex max-w-md flex-col gap-8">
      <h1 className="text-headline">Hồ sơ</h1>
      <ProfileForm me={me} />
      <PasswordForm hasPassword={me.hasPassword} />
    </div>
  );
}

function ProfileForm({ me }: { me: Schemas["MeResponse"] }) {
  const [displayName, setDisplayName] = useState(me.displayName);
  const [phoneNumber, setPhoneNumber] = useState(me.phoneNumber ?? "");
  const [uiLanguage, setUiLanguage] = useState(me.uiLanguage);
  const [saved, setSaved] = useState(false);
  const save = useAction();
  const { revalidate } = useRevalidator();

  function onSubmit(event: FormEvent) {
    event.preventDefault();
    setSaved(false);
    void save.run(async () => {
      await apiPut("/me/profile", { displayName: displayName.trim(), phoneNumber: phoneNumber.trim() || null, uiLanguage } satisfies Schemas["ProfileRequest"]);
      setSaved(true);
      await revalidate(); // the sidebar shows the new name
    });
  }

  return (
    <section aria-labelledby="profile-title" className="flex flex-col gap-4">
      <h2 id="profile-title" className="text-title">
        Thông tin cá nhân
      </h2>
      <form onSubmit={onSubmit} className="flex flex-col gap-4">
        {save.error !== null && <Notice tone="error">{errorMessage(save.error)}</Notice>}
        {saved && <Notice tone="info">Đã lưu hồ sơ.</Notice>}
        <TextField label="Email" type="email" value={me.email} readOnly hint="Email dùng để đăng nhập, không đổi được." />
        <TextField label="Tên hiển thị" required maxLength={100} autoComplete="name" value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
        <TextField
          label="Số điện thoại"
          type="tel"
          autoComplete="tel"
          hint="Không bắt buộc. 8 đến 15 chữ số, có thể bắt đầu bằng +."
          value={phoneNumber}
          onChange={(e) => setPhoneNumber(e.target.value)}
        />
        <SelectField
          label="Ngôn ngữ kết quả phân tích và trợ lý AI"
          hint="Áp dụng cho lần phân tích sau và câu trả lời của trợ lý. Giao diện vẫn bằng tiếng Việt."
          value={uiLanguage}
          onChange={(e) => setUiLanguage(e.target.value as Schemas["UiLanguage"])}
        >
          <option value="vi">Tiếng Việt</option>
          <option value="en">English</option>
        </SelectField>
        <Button type="submit" loading={save.pending} disabled={!displayName.trim()} className="self-start">
          Lưu hồ sơ
        </Button>
      </form>
    </section>
  );
}

function PasswordForm({ hasPassword }: { hasPassword: boolean }) {
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [done, setDone] = useState(false);
  const change = useAction();
  const { revalidate } = useRevalidator();
  const mismatch = confirm !== "" && confirm !== newPassword;
  const policyError = change.error instanceof ApiError && change.error.code === "ERR-AUTH-002" ? errorMessage(change.error) : undefined;
  const title = hasPassword ? "Đổi mật khẩu" : "Đặt mật khẩu";

  function onSubmit(event: FormEvent) {
    event.preventDefault();
    setDone(false);
    void change.run(async () => {
      await apiPost("/me/password", { currentPassword: hasPassword ? currentPassword : null, newPassword } satisfies Schemas["ChangePasswordRequest"]);
      setCurrentPassword("");
      setNewPassword("");
      setConfirm("");
      setDone(true);
      await revalidate(); // hasPassword may have changed
    });
  }

  return (
    <section aria-labelledby="password-title" className="flex flex-col gap-4">
      <h2 id="password-title" className="text-title">
        {title}
      </h2>
      {!hasPassword && <p className="text-ink-secondary">Tài khoản này đang đăng nhập bằng Google. Đặt mật khẩu để đăng nhập được cả bằng email.</p>}
      <form onSubmit={onSubmit} className="flex flex-col gap-4">
        {change.error !== null && !policyError && <Notice tone="error">{errorMessage(change.error)}</Notice>}
        {done && <Notice tone="info">Đã lưu mật khẩu mới. Các thiết bị khác sẽ phải đăng nhập lại.</Notice>}
        {hasPassword && (
          <TextField
            label="Mật khẩu hiện tại"
            type="password"
            autoComplete="current-password"
            required
            value={currentPassword}
            onChange={(e) => setCurrentPassword(e.target.value)}
          />
        )}
        <TextField
          label="Mật khẩu mới"
          type="password"
          autoComplete="new-password"
          required
          hint="Ít nhất 8 ký tự, có cả chữ và số."
          error={policyError}
          value={newPassword}
          onChange={(e) => setNewPassword(e.target.value)}
        />
        <TextField
          label="Nhập lại mật khẩu mới"
          type="password"
          autoComplete="new-password"
          required
          error={mismatch ? "Hai mật khẩu chưa khớp." : undefined}
          value={confirm}
          onChange={(e) => setConfirm(e.target.value)}
        />
        <Button type="submit" loading={change.pending} disabled={!newPassword || mismatch || confirm === "" || (hasPassword && !currentPassword)} className="self-start">
          {title}
        </Button>
      </form>
    </section>
  );
}
