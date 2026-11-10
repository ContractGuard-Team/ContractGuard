import { Link, useNavigate } from "react-router";

import { AuthPage, linkClass } from "~/components/AuthPage";
import { Button } from "~/components/Button";
import { Notice } from "~/components/Notice";
import { ApiError, apiFetch, apiPost, type Schemas } from "~/lib/api";
import { errorMessage } from "~/lib/errors";
import { useAction } from "~/lib/useAction";
import { setWorkspace } from "~/lib/workspace";

import type { Route } from "./+types/invitation";

export function meta(_: Route.MetaArgs) {
  return [{ title: "Lời mời tham gia tổ chức · ContractGuard" }];
}

// The link of an invitation email works without a session (SEC-AUTH-001). An invalid or expired link
// shows FR-ORG-005; who is signed in decides what can be done next.
export async function clientLoader({ params }: Route.ClientLoaderArgs) {
  let invitation: Schemas["InvitationView"] | null = null;
  let invalid = false;
  try {
    invitation = await apiFetch<Schemas["InvitationView"]>(`/invitations/${encodeURIComponent(params.token)}`);
  } catch (error) {
    if (!(error instanceof ApiError) || error.code !== "ERR-ORG-002") {
      throw error;
    }

    invalid = true;
  }

  const me = await apiFetch<Schemas["MeResponse"]>("/me").catch(() => null);
  return { invitation, invalid, me };
}

export default function Invitation({ loaderData, params }: Route.ComponentProps) {
  const { invitation, invalid, me } = loaderData;
  const accept = useAction();
  const navigate = useNavigate();
  const here = `/invitations/${params.token}`;

  function join() {
    void accept.run(async () => {
      const joined = await apiPost<{ organizationId: string }>(`${here}/accept`);
      setWorkspace(joined.organizationId);
      await navigate("/");
    });
  }

  return (
    <AuthPage title="Lời mời tham gia tổ chức" footer={<Link to="/" className={linkClass}>Về trang chủ</Link>}>
      {invalid || !invitation ? (
        <Notice tone="error">Liên kết mời không còn hiệu lực. Hãy nhờ Owner của tổ chức gửi lời mời mới.</Notice>
      ) : (
        <div className="flex flex-col gap-4">
          <p>
            <strong>{invitation.invitedBy}</strong> mời <strong>{invitation.email}</strong> tham gia tổ chức <strong>{invitation.organizationName}</strong> với vai trò Member.
          </p>
          {accept.error !== null && <Notice tone="error">{errorMessage(accept.error)}</Notice>}
          {me === null ? (
            <p className="text-ink-secondary">
              <Link to={`/login?next=${encodeURIComponent(here)}`} className={linkClass}>
                Đăng nhập
              </Link>{" "}
              hoặc{" "}
              <Link to="/register" className={linkClass}>
                đăng ký
              </Link>{" "}
              bằng {invitation.email} để tham gia.
            </p>
          ) : me.email.toLowerCase() !== invitation.email ? (
            <Notice tone="error">
              Bạn đang đăng nhập bằng {me.email}. Lời mời này dành cho {invitation.email}; hãy đăng xuất rồi đăng nhập bằng đúng email.
            </Notice>
          ) : (
            <Button loading={accept.pending} onClick={join}>
              Tham gia {invitation.organizationName}
            </Button>
          )}
        </div>
      )}
    </AuthPage>
  );
}
