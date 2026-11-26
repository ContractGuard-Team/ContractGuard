import { Link } from "react-router";

import type { Schemas } from "~/lib/api";
import { uploadVersion } from "~/lib/upload";

import { ContractDropzone } from "./ContractDropzone";
import { SeverityTag } from "./SeverityTag";
import { VersionCompare } from "./VersionCompare";

const dateFormat = new Intl.DateTimeFormat("vi-VN", { dateStyle: "short", timeStyle: "short" });
const runLabels: Record<NonNullable<Schemas["RunStatus"]>, string> = { running: "Đang phân tích", completed: "Đã phân tích", failed: "Phân tích thất bại", cancelled: "Đã dừng phân tích" };

// F-CTR-005: every version with its own result (FR-CTR-016, FR-CTR-017), and the upload of the next one
// (FR-CTR-014). A new version starts without analysis; its context form is prefilled (FR-CTR-015).
export function VersionsPanel({
  contractId,
  versions,
  shownVersionId,
  busy,
  onUploaded,
}: {
  contractId: string;
  versions: Schemas["VersionItem"][];
  shownVersionId: string;
  busy: boolean;
  onUploaded: () => Promise<void>;
}) {
  return (
    <section aria-labelledby="versions-title" className="mx-auto flex w-full max-w-sheet flex-col gap-4 py-4">
      <h2 id="versions-title" className="text-title">
        Lịch sử phiên bản
      </h2>
      <ol className="divide-y divide-rule rounded-md border border-rule bg-sheet">
        {versions.map((version, index) => {
          const shown = version.contractVersionId === shownVersionId;
          return (
            <li key={version.contractVersionId} className={`flex flex-wrap items-center justify-between gap-3 px-4 py-3 ${shown ? "bg-review-ink-wash" : ""}`}>
              <div className="min-w-0">
                <p className="font-medium">
                  V{version.versionNumber} · <span className="font-normal">{version.originalFileName}</span>
                </p>
                <p className="text-caption text-ink-secondary">
                  {dateFormat.format(new Date(version.uploadedAt))} · {version.latestRunStatus ? runLabels[version.latestRunStatus] : "Chưa phân tích"}
                </p>
              </div>
              <div className="flex items-center gap-3">
                {version.riskLevel && (
                  <span className="flex items-center gap-2">
                    <span className="tabular-nums">{version.overallScore}</span>
                    <SeverityTag severity={version.riskLevel} />
                  </span>
                )}
                {shown ? (
                  <span className="text-label text-ink-secondary">Đang xem</span>
                ) : (
                  <Link
                    to={index === 0 ? `/contracts/${contractId}?view=versions` : `/contracts/${contractId}?v=${version.contractVersionId}`}
                    className="text-label text-review-ink underline underline-offset-2"
                  >
                    Mở phiên bản này
                  </Link>
                )}
              </div>
            </li>
          );
        })}
      </ol>

      <VersionCompare contractId={contractId} versions={versions} />

      <div className="flex flex-col gap-2">
        <h3 className="text-label">Tải lên phiên bản mới</h3>
        {busy ? (
          <p className="text-ink-secondary">Đang phân tích, hãy chờ xong rồi tải phiên bản mới.</p>
        ) : (
          <ContractDropzone upload={(file) => uploadVersion(contractId, file)} onUploaded={onUploaded} />
        )}
      </div>
    </section>
  );
}
