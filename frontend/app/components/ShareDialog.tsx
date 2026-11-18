import { useEffect, useId, useState, type FormEvent } from "react";

import { apiFetch, apiPost, apiPut, type Schemas } from "~/lib/api";
import { errorMessage } from "~/lib/errors";
import { contractAccessLabels } from "~/lib/labels";
import { useAction } from "~/lib/useAction";

import { Button } from "./Button";
import { ConfirmDialog } from "./ConfirmDialog";
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
  const [confirmWhole, setConfirmWhole] = useState(false);
  const [confirmTransfer, setConfirmTransfer] = useState(false);
  const [revoking, setRevoking] = useState<Schemas["PersonItem"] | null>(null);
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
  // Organization members who do not have access yet (administrators always do), offered when the email box is clicked.
  const suggestions = candidates.filter((m) => m.role !== "owner" && !access?.shares.some((s) => s.userId === m.userId));
  // CR-GEN-002: everyone who can open the contract, each with one effective role.
  const people = access
    ? [
        {
          ...access.owner,
          // Both roles give full rights, but the owner should still see that the person administers the organization.
          role: members.some((m) => m.userId === access.owner.userId && m.role === "owner")
            ? `${contractAccessLabels.administrator} / ${contractAccessLabels.owner}`
            : contractAccessLabels.owner,
          revocable: false,
        },
        ...access.administrators.map((p) => ({ ...p, role: contractAccessLabels.administrator, revocable: false })),
        ...access.shares.map((p) => ({ ...p, role: contractAccessLabels.shared, revocable: true })),
      ]
    : null;
  const suggestionsId = useId();

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
            list={organizationId ? suggestionsId : undefined}
            autoComplete="off"
            hint={organizationId ? "Chỉ chia sẻ được cho thành viên của tổ chức." : "Người nhận cần có tài khoản ContractGuard."}
          />
          {organizationId && (
            <datalist id={suggestionsId}>
              {suggestions.map((m) => (
                <option key={m.userId} value={m.email}>
                  {m.displayName}
                </option>
              ))}
            </datalist>
          )}
        </div>
        <Button type="submit" loading={action.pending} className="mb-6">
          Chia sẻ
        </Button>
      </form>
      {organizationId && (
        <Button variant="secondary" size="sm" className="self-start" disabled={action.pending} onClick={() => setConfirmWhole(true)}>
          Chia sẻ cho toàn tổ chức
        </Button>
      )}

      <div>
        <h3 className="mb-2 text-label">Người có quyền truy cập</h3>
        {people === null ? (
          <div className="h-16 animate-pulse rounded-md bg-rule/40" aria-hidden="true" />
        ) : (
          <ul className="divide-y divide-rule rounded-md border border-rule">
            {people.map((person) => (
              <li key={person.userId} className="flex items-center justify-between gap-2 px-3 py-2">
                <span>
                  {person.displayName} <span className="text-caption text-ink-muted">{person.email}</span>
                </span>
                <span className="flex items-center gap-3">
                  <span className="text-label text-ink-secondary">{person.role}</span>
                  {person.revocable && (
                    <Button variant="danger" size="sm" disabled={action.pending} onClick={() => setRevoking(person)}>
                      Thu hồi
                    </Button>
                  )}
                </span>
              </li>
            ))}
          </ul>
        )}
        {access?.isOrganizationContract && <p className="mt-2 text-caption text-ink-muted">Quản trị viên của tổ chức luôn có toàn quyền trên hợp đồng.</p>}
      </div>

      {organizationId && candidates.length > 0 && (
        <div className="flex items-end gap-2 border-t border-rule pt-4">
          <label className="flex min-w-0 flex-1 flex-col gap-1 text-label">
            Chuyển người phụ trách hợp đồng cho
            <select
              value={newOwner}
              onChange={(e) => setNewOwner(e.target.value)}
              className="h-10 w-full min-w-0 truncate rounded-md border border-rule-strong bg-sheet px-2 text-body focus:border-review-ink focus:ring-3 focus:ring-review-ink/15 focus:outline-none"
            >
              <option value="">Chọn thành viên</option>
              {candidates.map((m) => (
                <option key={m.userId} value={m.userId}>
                  {m.displayName} ({m.email})
                </option>
              ))}
            </select>
          </label>
          <Button variant="secondary" disabled={!newOwner || action.pending} onClick={() => setConfirmTransfer(true)}>
            Chuyển
          </Button>
        </div>
      )}

      <Button variant="ghost" className="self-end" onClick={onClose}>
        Đóng
      </Button>

      <ConfirmDialog
        open={confirmWhole}
        title="Chia sẻ cho toàn tổ chức?"
        confirmLabel="Chia sẻ cho tất cả"
        onCancel={() => setConfirmWhole(false)}
        onConfirm={() => {
          setConfirmWhole(false);
          run(() => apiPost(`/contracts/${contractId}/shares`, { wholeOrganization: true }), "Đã chia sẻ cho mọi thành viên hiện có của tổ chức.");
        }}
      >
        Tất cả {candidates.filter((m) => m.role !== "owner").length} thành viên còn lại của tổ chức sẽ xem được hợp đồng này và kết quả phân tích (quản trị viên đã có quyền sẵn).
      </ConfirmDialog>

      <ConfirmDialog
        open={confirmTransfer}
        title={`Giao hợp đồng cho ${candidates.find((m) => m.userId === newOwner)?.displayName ?? ""} phụ trách?`}
        confirmLabel="Chuyển quyền"
        danger
        onCancel={() => setConfirmTransfer(false)}
        onConfirm={() => {
          setConfirmTransfer(false);
          // The caller is no longer the owner: the share list is not theirs to manage, so close and reload the page.
          void action.run(async () => {
            await apiPut(`/contracts/${contractId}/owner`, { userId: newOwner });
            onChanged();
            onClose();
          });
        }}
      >
        Người này sẽ phụ trách hợp đồng. Bạn chuyển sang vai trò {contractAccessLabels.shared}: vẫn làm việc trên hợp đồng nhưng không còn quản lý chia sẻ. Nếu bạn là quản trị viên của tổ chức thì vẫn giữ toàn quyền.
      </ConfirmDialog>

      <ConfirmDialog
        open={revoking !== null}
        title={`Thu hồi quyền của ${revoking?.displayName ?? ""}?`}
        confirmLabel="Thu hồi"
        danger
        onCancel={() => setRevoking(null)}
        onConfirm={() => {
          const person = revoking!;
          setRevoking(null);
          run(() => apiFetch(`/contracts/${contractId}/shares/${person.userId}`, { method: "DELETE" }), "Đã thu hồi quyền.");
        }}
      >
        Người này không còn xem được hợp đồng và kết quả phân tích, có hiệu lực ngay.
      </ConfirmDialog>
    </div>
  );
}
