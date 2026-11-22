import { useEffect, useState } from "react";

import { apiFetch, type Schemas } from "~/lib/api";
import { errorMessage } from "~/lib/errors";

const dateFormat = new Intl.DateTimeFormat("vi-VN", { dateStyle: "short", timeStyle: "medium" });

export const auditActionLabels: Record<Schemas["AuditAction"], string> = {
  uploaded: "Tải hợp đồng lên",
  viewed: "Xem hợp đồng",
  version_uploaded: "Tải phiên bản mới",
  context_confirmed: "Xác nhận ngữ cảnh",
  metadata_edited: "Sửa thông tin hợp đồng",
  status_changed: "Đổi trạng thái duyệt",
  finding_handled: "Cập nhật xử lý rủi ro",
  shared: "Chia sẻ",
  share_revoked: "Thu hồi chia sẻ",
  owner_transferred: "Chuyển người phụ trách",
  report_exported: "Xuất báo cáo",
  renamed: "Đổi tên hợp đồng",
  trashed: "Chuyển vào thùng rác",
  restored: "Khôi phục từ thùng rác",
};

// F-AUD-001: who did what and when, for the contract owner and organization owners (FR-AUD-002).
export function AuditPanel({ contractId }: { contractId: string }) {
  const [entries, setEntries] = useState<Schemas["AuditEntry"][] | null>(null);
  const [error, setError] = useState<unknown>(null);

  useEffect(() => {
    let current = true;
    apiFetch<Schemas["AuditEntry"][]>(`/contracts/${contractId}/audit`)
      .then((loaded) => current && setEntries(loaded))
      .catch((caught: unknown) => current && setError(caught));
    return () => {
      current = false;
    };
  }, [contractId]);

  return (
    <section aria-labelledby="audit-title" className="mx-auto flex w-full max-w-sheet flex-col gap-3 py-4">
      <h2 id="audit-title" className="text-title">
        Nhật ký hoạt động
      </h2>
      {error !== null && <p className="text-severity-critical">{errorMessage(error)}</p>}
      {entries === null && error === null && <div className="h-32 animate-pulse rounded-md bg-rule/40" aria-hidden="true" />}
      {entries !== null && (
        <div className="overflow-x-auto rounded-md border border-rule bg-sheet">
          <table className="w-full text-left text-body">
            <thead className="border-b border-rule text-label text-ink-secondary">
              <tr>
                <th scope="col" className="px-3 py-2 font-medium whitespace-nowrap">
                  Thời điểm
                </th>
                <th scope="col" className="px-3 py-2 font-medium">
                  Người thực hiện
                </th>
                <th scope="col" className="px-3 py-2 font-medium">
                  Hành động
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-rule">
              {entries.map((entry) => (
                <tr key={entry.auditEventId}>
                  <td className="px-3 py-2 whitespace-nowrap tabular-nums text-ink-secondary">{dateFormat.format(new Date(entry.occurredAt))}</td>
                  <td className="px-3 py-2">
                    {entry.actorName ?? "Tài khoản đã xóa"}
                    {entry.actorEmail && <span className="block text-caption text-ink-muted">{entry.actorEmail}</span>}
                  </td>
                  <td className="px-3 py-2">
                    {auditActionLabels[entry.action]}
                    {entry.detail && <span className="block text-caption text-ink-muted">{entry.detail}</span>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
