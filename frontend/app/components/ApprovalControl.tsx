import { apiPut, type Schemas } from "~/lib/api";
import { errorMessage } from "~/lib/errors";
import { approvalStatusLabels } from "~/lib/labels";
import { useAction } from "~/lib/useAction";

type Status = Schemas["ApprovalStatus"];

// SRS §13.2, user transitions only (the server enforces the same list, FR-CTR-013).
const transitions: Partial<Record<Status, Status[]>> = {
  pending_review: ["negotiating", "approved", "rejected"],
  negotiating: ["approved", "rejected"],
  approved: ["negotiating"],
  rejected: ["negotiating"],
};

// F-CTR-004: the approval status in the workspace header. New and Analyzing are set by the system.
export function ApprovalControl({ contractId, status, onChanged }: { contractId: string; status: Status; onChanged: () => void }) {
  const change = useAction();
  const targets = transitions[status] ?? [];

  if (targets.length === 0) {
    return null;
  }

  return (
    <div className="flex items-center gap-2">
      <label htmlFor="approval-status" className="sr-only">
        Trạng thái duyệt
      </label>
      <select
        id="approval-status"
        value={status}
        disabled={change.pending}
        onChange={(e) =>
          void change.run(async () => {
            await apiPut(`/contracts/${contractId}/approval-status`, { status: e.target.value as Status });
            onChanged();
          })
        }
        className="h-9 rounded-md border border-rule-strong bg-sheet px-2 text-label font-semibold focus:border-review-ink focus:ring-3 focus:ring-review-ink/15 focus:outline-none disabled:opacity-50"
      >
        <option value={status}>{approvalStatusLabels[status]}</option>
        {targets.map((target) => (
          <option key={target} value={target}>
            → {approvalStatusLabels[target]}
          </option>
        ))}
      </select>
      {change.error !== null && (
        <span role="alert" className="text-caption text-severity-critical">
          {errorMessage(change.error)}
        </span>
      )}
    </div>
  );
}
