import { useEffect, useState, type FormEvent } from "react";

import { apiFetch, apiPost, apiPut, type Schemas } from "~/lib/api";
import { errorMessage } from "~/lib/errors";
import { useAction } from "~/lib/useAction";

import { Button } from "./Button";
import { TextField } from "./Field";
import { Notice } from "./Notice";

// F-SHARE-001 (DESIGN: a modal is allowed for sharing). The contract owner sees who has access, adds
// people by email (organization contracts: members only; personal: any account), revokes access and,
// for organization contracts, hands the contract to another member.
export function ShareDialog({
  contractId,
  organizationId,
  onChanged,
  onClose,
}: {
  contractId: string;
  organizationId: string | null;
  onChanged: () => void;
  onClose: () => void;
}) {
  const [access, setAccess] = useState<Schemas["ContractAccessView"] | null>(null);
  const [members, setMembers] = useState<Schemas["MemberItem"][]>([]);
  const [email, setEmail] = useState("");
  const [newOwner, setNewOwner] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const action = useAction();

  async function reload() {
    setAccess(await apiFetch<Schemas["ContractAccessView"]>(`/contracts/${contractId}/shares`));
    if (organizationId) {
      setMembers((await apiFetch<Schemas["OrganizationDetail"]>(`/organizations/${organizationId}`)).members);
    }
  }

  useEffect(() => {
    let current = true;
    const load = async () => {
      const loaded = await apiFetch<Schemas["ContractAccessView"]>(`/contracts/${contractId}/shares`);
      const people = organizationId ? (await apiFetch<Schemas["OrganizationDetail"]>(`/organizations/${organizationId}`)).members : [];
      if (current) {
        setAccess(loaded);
        setMembers(people);
      }
    };
    load().catch(() => current && setMessage("Không tải được danh sách chia sẻ."));
    return () => {
      current = false;
    };
  }, [contractId, organizationId]);

  function run(work: () => Promise<void>, done: string) {
    setMessage(null);
    void action.run(async () => {
      await work();
      await reload();
      setMessage(done);
      onChanged();
    });
  }

  function onShare(event: FormEvent) {
    event.preventDefault();
    run(async () => {
      await apiPost(`/contracts/${contractId}/shares`, { email: email.trim() });
      setEmail("");
    }, "Đã chia sẻ.");
  }

  const candidates = members.filter((m) => m.userId !== access?.owner.userId);

  return (
    <div className="flex flex-col gap-4">
      {action.error !== null && <Notice tone="error">{errorMessage(action.error)}</Notice>}
      {message && <Notice tone="info">{message}</Notice>}

      <form onSubmit={onShare} className="flex items-end gap-2">
        <div className="flex-1">
          <TextField
            label="Chia sẻ theo email"
            type="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            hint={organizationId ? "Chỉ chia sẻ được cho thành viên của tổ chức." : "Người nhận cần có tài khoản ContractGuard."}
          />
        </div>
        <Button type="submit" loading={action.pending} className="mb-6">
          Chia sẻ
        </Button>
      </form>
      {organizationId && (
        <Button variant="secondary" size="sm" className="self-start" disabled={action.pending} onClick={() => run(() => apiPost(`/contracts/${contractId}/shares`, { wholeOrganization: true }), "Đã chia sẻ cho mọi thành viên hiện có của tổ chức.")}>
          Chia sẻ cho toàn tổ chức
        </Button>
      )}

      <div>
        <h3 className="mb-2 text-label">Người có quyền truy cập</h3>
        {access === null ? (
          <div className="h-16 animate-pulse rounded-md bg-rule/40" aria-hidden="true" />
        ) : (
          <ul className="divide-y divide-rule rounded-md border border-rule">
            <li className="flex items-center justify-between gap-2 px-3 py-2">
              <span>
                {access.owner.displayName} <span className="text-caption text-ink-muted">{access.owner.email}</span>
              </span>
              <span className="text-label text-ink-secondary">Owner</span>
            </li>
            {access.shares.map((share) => (
              <li key={share.userId} className="flex items-center justify-between gap-2 px-3 py-2">
                <span>
                  {share.displayName} <span className="text-caption text-ink-muted">{share.email}</span>
                </span>
                <Button variant="danger" size="sm" disabled={action.pending} onClick={() => run(() => apiFetch(`/contracts/${contractId}/shares/${share.userId}`, { method: "DELETE" }), "Đã thu hồi quyền.")}>
                  Thu hồi
                </Button>
              </li>
            ))}
          </ul>
        )}
      </div>

      {organizationId && candidates.length > 0 && (
        <div className="flex items-end gap-2 border-t border-rule pt-4">
          <label className="flex flex-1 flex-col gap-1 text-label">
            Chuyển quyền Owner cho
            <select
              value={newOwner}
              onChange={(e) => setNewOwner(e.target.value)}
              className="h-10 rounded-md border border-rule-strong bg-sheet px-2 text-body focus:border-review-ink focus:ring-3 focus:ring-review-ink/15 focus:outline-none"
            >
              <option value="">Chọn thành viên</option>
              {candidates.map((m) => (
                <option key={m.userId} value={m.userId}>
                  {m.displayName} ({m.email})
                </option>
              ))}
            </select>
          </label>
          <Button variant="secondary" disabled={!newOwner || action.pending} onClick={() => run(() => apiPut(`/contracts/${contractId}/owner`, { userId: newOwner }), "Đã chuyển quyền Owner. Bạn vẫn có quyền Member.")}>
            Chuyển
          </Button>
        </div>
      )}

      <Button variant="ghost" className="self-end" onClick={onClose}>
        Đóng
      </Button>
    </div>
  );
}
