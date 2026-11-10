import { useState, type FormEvent } from "react";
import { redirect, useRevalidator } from "react-router";

import { Button } from "~/components/Button";
import { TextField } from "~/components/Field";
import { Notice } from "~/components/Notice";
import { apiFetch, apiLoad, apiPost, apiPut, type Schemas } from "~/lib/api";
import { errorMessage } from "~/lib/errors";
import { useAction } from "~/lib/useAction";
import { currentWorkspace } from "~/lib/workspace";

import type { Route } from "./+types/organization";

export function meta({ loaderData }: Route.MetaArgs) {
  return [{ title: `${loaderData?.organizationName ?? "Tổ chức"} · ContractGuard` }];
}

// The organization of the current workspace; in the personal workspace there is none to show.
export async function clientLoader() {
  const organizationId = currentWorkspace();
  if (!organizationId) {
    throw redirect("/");
  }

  return apiLoad<Schemas["OrganizationDetail"]>(`/organizations/${organizationId}`);
}

const dateFormat = new Intl.DateTimeFormat("vi-VN", { dateStyle: "short" });
const roleLabels = { owner: "Owner", member: "Member" } as const;

// F-ORG-001, F-ORG-002: members with email and role for everyone (FR-ORG-006); invitations, roles,
// removal and the name for owners (FR-ORG-003, 007, 008, 009, 012).
export default function OrganizationPage({ loaderData: organization }: Route.ComponentProps) {
  const { revalidate } = useRevalidator();
  const isOwner = organization.myRole === "owner";
  const [email, setEmail] = useState("");
  const [name, setName] = useState(organization.organizationName);
  const [message, setMessage] = useState<string | null>(null);
  const action = useAction();
  const base = `/organizations/${organization.organizationId}`;

  function run(work: () => Promise<unknown>, done: string) {
    setMessage(null);
    void action.run(async () => {
      await work();
      setMessage(done);
      await revalidate();
    });
  }

  function onInvite(event: FormEvent) {
    event.preventDefault();
    run(async () => {
      await apiPost(`${base}/invitations`, { email: email.trim() });
      setEmail("");
    }, `Đã gửi lời mời tới ${email.trim()}. Liên kết có hiệu lực 7 ngày.`);
  }

  return (
    <section aria-labelledby="org-title" className="flex max-w-4xl flex-col gap-6">
      <h1 id="org-title" className="text-headline">
        {organization.organizationName}
      </h1>

      {action.error !== null && <Notice tone="error">{errorMessage(action.error)}</Notice>}
      {message && <Notice tone="info">{message}</Notice>}

      {isOwner && (
        <div className="flex flex-wrap gap-6">
          <form onSubmit={onInvite} className="flex min-w-72 flex-1 items-end gap-2">
            <div className="flex-1">
              <TextField label="Mời thành viên qua email" type="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
            </div>
            <Button type="submit" loading={action.pending}>
              Gửi lời mời
            </Button>
          </form>
          <form
            onSubmit={(e) => {
              e.preventDefault();
              run(() => apiPut(base, { name: name.trim() }), "Đã đổi tên tổ chức.");
            }}
            className="flex min-w-72 flex-1 items-end gap-2"
          >
            <div className="flex-1">
              <TextField label="Tên tổ chức" required maxLength={200} value={name} onChange={(e) => setName(e.target.value)} />
            </div>
            <Button type="submit" variant="secondary" disabled={name.trim() === organization.organizationName || !name.trim()}>
              Đổi tên
            </Button>
          </form>
        </div>
      )}

      <div className="overflow-x-auto rounded-md border border-rule bg-sheet">
        <table className="w-full text-left">
          <caption className="border-b border-rule px-4 py-2.5 text-left text-title">{organization.members.length} thành viên</caption>
          <thead className="border-b border-rule text-label text-ink-secondary">
            <tr>
              <th scope="col" className="px-4 py-2 font-medium">
                Thành viên
              </th>
              <th scope="col" className="px-4 py-2 font-medium">
                Vai trò
              </th>
              <th scope="col" className="px-4 py-2 font-medium whitespace-nowrap">
                Tham gia
              </th>
              {isOwner && (
                <th scope="col" className="px-4 py-2 font-medium">
                  <span className="sr-only">Thao tác</span>
                </th>
              )}
            </tr>
          </thead>
          <tbody className="divide-y divide-rule">
            {organization.members.map((member) => (
              <tr key={member.userId}>
                <td className="px-4 py-3">
                  {member.displayName}
                  <span className="block text-caption text-ink-secondary">{member.email}</span>
                </td>
                <td className="px-4 py-3">
                  {isOwner ? (
                    <select
                      aria-label={`Vai trò của ${member.displayName}`}
                      value={member.role ?? "member"}
                      disabled={action.pending}
                      onChange={(e) => run(() => apiPut(`${base}/members/${member.userId}/role`, { role: e.target.value }), "Đã đổi vai trò.")}
                      className="h-9 rounded-md border border-rule-strong bg-sheet px-2 text-body focus:border-review-ink focus:ring-3 focus:ring-review-ink/15 focus:outline-none"
                    >
                      <option value="owner">Owner</option>
                      <option value="member">Member</option>
                    </select>
                  ) : (
                    roleLabels[member.role ?? "member"]
                  )}
                </td>
                <td className="px-4 py-3 whitespace-nowrap text-ink-secondary">{dateFormat.format(new Date(member.joinedAt))}</td>
                {isOwner && (
                  <td className="px-4 py-3 text-right">
                    <Button
                      variant="danger"
                      size="sm"
                      disabled={action.pending}
                      onClick={() => {
                        if (window.confirm(`Xóa ${member.displayName} khỏi tổ chức? Hợp đồng người này sở hữu sẽ chuyển cho bạn.`)) {
                          run(() => apiFetch(`${base}/members/${member.userId}`, { method: "DELETE" }), "Đã xóa thành viên.");
                        }
                      }}
                    >
                      Xóa
                    </Button>
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {isOwner && organization.invitations.length > 0 && (
        <div className="rounded-md border border-rule bg-sheet">
          <h2 className="border-b border-rule px-4 py-2.5 text-title">Lời mời đang chờ</h2>
          <ul className="divide-y divide-rule">
            {organization.invitations.map((invitation) => (
              <li key={invitation.invitationId} className="flex items-center justify-between gap-3 px-4 py-3">
                <span>
                  {invitation.email}
                  <span className="block text-caption text-ink-secondary">Hết hạn {dateFormat.format(new Date(invitation.expiresAt))}</span>
                </span>
                <Button variant="danger" size="sm" disabled={action.pending} onClick={() => run(() => apiFetch(`${base}/invitations/${invitation.invitationId}`, { method: "DELETE" }), "Đã hủy lời mời.")}>
                  Hủy lời mời
                </Button>
              </li>
            ))}
          </ul>
        </div>
      )}
    </section>
  );
}
