import { useState } from "react";
import { Link } from "react-router";

import { apiFetch, apiPost, type Schemas } from "~/lib/api";
import { errorMessage } from "~/lib/errors";
import { uploadVersion } from "~/lib/upload";
import { useAction } from "~/lib/useAction";

import { Button } from "./Button";
import { ConfirmDialog } from "./ConfirmDialog";
import { ContractDropzone } from "./ContractDropzone";
import { Notice } from "./Notice";
import { SeverityTag } from "./SeverityTag";
import { VersionCompare } from "./VersionCompare";

const dateFormat = new Intl.DateTimeFormat("vi-VN", { dateStyle: "short", timeStyle: "short" });
const runLabels: Record<NonNullable<Schemas["RunStatus"]>, string> = { running: "Đang phân tích", completed: "Đã phân tích", failed: "Phân tích thất bại", cancelled: "Đã dừng phân tích" };

// F-CTR-005: every version with its own result (FR-CTR-016, FR-CTR-017), and the upload of the next one
// (FR-CTR-014). A new version starts without analysis; its context form is prefilled (FR-CTR-015).
// The owner can soft delete a version: it stays listed, greyed, with a restore button.
export function VersionsPanel({
  contractId,
  versions,
  shownVersionId,
  busy,
  isOwner,
  onUploaded,
  onChanged,
}: {
  contractId: string;
  versions: Schemas["VersionItem"][];
  shownVersionId: string;
  busy: boolean;
  isOwner: boolean;
  onUploaded: () => Promise<void>;
  onChanged: () => Promise<void>;
}) {
  const action = useAction();
  const [deleting, setDeleting] = useState<Schemas["VersionItem"] | null>(null);
  const active = versions.filter((v) => v.deletedAt === null);
  const latestId = active[0]?.contractVersionId;

  function run(work: () => Promise<unknown>) {
    void action.run(async () => {
      await work();
      await onChanged();
    });
  }

  return (
    <section aria-labelledby="versions-title" className="mx-auto flex w-full max-w-sheet flex-col gap-4 py-4">
      <h2 id="versions-title" className="text-title">
        Lịch sử phiên bản
      </h2>
      {action.error !== null && <Notice tone="error">{errorMessage(action.error)}</Notice>}
      <ol className="divide-y divide-rule rounded-md border border-rule bg-sheet">
        {versions.map((version) => {
          const shown = version.contractVersionId === shownVersionId;
          const deleted = version.deletedAt !== null;
          return (
            <li key={version.contractVersionId} className={`flex flex-wrap items-center justify-between gap-3 px-4 py-3 ${shown ? "bg-review-ink-wash" : ""}`}>
              <div className={`min-w-0 ${deleted ? "text-ink-muted" : ""}`}>
                <p className="font-medium">
                  V{version.versionNumber} · <span className={`font-normal ${deleted ? "line-through" : ""}`}>{version.originalFileName}</span>
                </p>
                <p className="text-caption text-ink-secondary">
                  {deleted
                    ? `Đã xóa lúc ${dateFormat.format(new Date(version.deletedAt!))}`
                    : `${dateFormat.format(new Date(version.uploadedAt))} · ${version.latestRunStatus ? runLabels[version.latestRunStatus] : "Chưa phân tích"}`}
                </p>
              </div>
              <div className="flex items-center gap-3">
                {!deleted && version.riskLevel && (
                  <span className="flex items-center gap-2">
                    <span className="tabular-nums">{version.overallScore}</span>
                    <SeverityTag severity={version.riskLevel} />
                  </span>
                )}
                {deleted ? (
                  isOwner && (
                    <Button variant="secondary" size="sm" disabled={action.pending} onClick={() => run(() => apiPost(`/contracts/${contractId}/versions/${version.contractVersionId}/restore`))}>
                      Khôi phục
                    </Button>
                  )
                ) : (
                  <>
                    {shown ? (
                      <span className="text-label text-ink-secondary">Đang xem</span>
                    ) : (
                      <Link
                        to={version.contractVersionId === latestId ? `/contracts/${contractId}?view=versions` : `/contracts/${contractId}?v=${version.contractVersionId}`}
                        className="text-label text-review-ink underline underline-offset-2"
                      >
                        Mở phiên bản này
                      </Link>
                    )}
                    {isOwner && active.length > 1 && !busy && (
                      <Button variant="danger" size="sm" disabled={action.pending} onClick={() => setDeleting(version)}>
                        Xóa
                      </Button>
                    )}
                  </>
                )}
              </div>
            </li>
          );
        })}
      </ol>

      <VersionCompare contractId={contractId} versions={active} />

      <div className="flex flex-col gap-2">
        <h3 className="text-label">Tải lên phiên bản mới</h3>
        {busy ? (
          <p className="text-ink-secondary">Đang phân tích, hãy chờ xong rồi tải phiên bản mới.</p>
        ) : (
          <ContractDropzone upload={(file) => uploadVersion(contractId, file)} onUploaded={onUploaded} />
        )}
      </div>

      <ConfirmDialog
        open={deleting !== null}
        title={`Xóa phiên bản V${deleting?.versionNumber ?? ""}?`}
        confirmLabel="Xóa phiên bản"
        danger
        onCancel={() => setDeleting(null)}
        onConfirm={() => {
          const version = deleting!;
          setDeleting(null);
          void action.run(async () => {
            await apiFetch(`/contracts/${contractId}/versions/${version.contractVersionId}`, { method: "DELETE" });
            // The open version is gone: go to the latest one (same as after an upload).
            await (version.contractVersionId === shownVersionId ? onUploaded() : onChanged());
          });
        }}
      >
        Phiên bản vẫn nằm trong lịch sử phiên bản và có thể khôi phục bất cứ lúc nào.
      </ConfirmDialog>
    </section>
  );
}
