import { type FormEvent, useCallback, useEffect, useRef, useState } from "react";
import { Link, useNavigate, useRevalidator, useSearchParams } from "react-router";

import { AnalysisProgress } from "~/components/AnalysisProgress";
import { ApprovalControl } from "~/components/ApprovalControl";
import { AuditPanel } from "~/components/AuditPanel";
import { Button } from "~/components/Button";
import { ChatPanel } from "~/components/ChatPanel";
import { Checklist } from "~/components/Checklist";
import { ContextForm } from "~/components/ContextForm";
import { ContractSheet } from "~/components/ContractSheet";
import { ContractWorkspace } from "~/components/ContractWorkspace";
import { TextField } from "~/components/Field";
import { FileViewer } from "~/components/FileViewer";
import { MetadataPanel } from "~/components/MetadataPanel";
import { Notice } from "~/components/Notice";
import type { PdfHighlight } from "~/components/PdfViewer";
import { ReportExport } from "~/components/ReportExport";
import { ResultOverview } from "~/components/ResultOverview";
import { ShareDialog } from "~/components/ShareDialog";
import { Tabs } from "~/components/Tabs";
import { VersionsPanel } from "~/components/VersionsPanel";
import { ApiError, apiFetch, apiLoad, apiPut, type Schemas } from "~/lib/api";
import { errorMessage, failureMessage } from "~/lib/errors";
import { approvalStatusLabels, severityOrder } from "~/lib/labels";
import { useAction } from "~/lib/useAction";
import { currentWorkspace, setWorkspace } from "~/lib/workspace";

import type { Route } from "./+types/contract-detail";

// Read by the app layout: this page takes the whole screen and the sidebar shrinks to icons.
export const handle = { workspace: true };

export function meta({ loaderData }: Route.MetaArgs) {
  return [{ title: `${loaderData?.contract.contractName ?? "Hợp đồng"} · ContractGuard` }];
}

// ?v= opens an older version (FR-CTR-016); without it the latest one is shown.
export async function clientLoader({ params, request }: Route.ClientLoaderArgs) {
  const versionId = new URL(request.url).searchParams.get("v");
  const contract = await loadInAnyWorkspace(`/contracts/${params.contractId}${versionId ? `?version=${encodeURIComponent(versionId)}` : ""}`);
  const jobId = contract.completedJobId;
  const [clauses, result] = jobId
    ? await Promise.all([apiLoad<Schemas["ClauseItem"][]>(`/jobs/${jobId}/clauses`), apiLoad<Schemas["AnalysisResultView"]>(`/jobs/${jobId}/result`)])
    : [[], null];
  return { contract, clauses, result };
}

// A link to a contract (from a colleague, an email, another tab) may belong to another of the user's
// workspaces than the one open. Rather than a 404, look in the others and switch to the one that has it.
async function loadInAnyWorkspace(path: string): Promise<Schemas["ContractDetail"]> {
  try {
    return await apiFetch<Schemas["ContractDetail"]>(path);
  } catch (error) {
    if (!(error instanceof ApiError && error.status === 404)) {
      return apiLoad<Schemas["ContractDetail"]>(path); // same error, through the loader's handling
    }
  }

  const original = currentWorkspace();
  const me = await apiLoad<Schemas["MeResponse"]>("/me");
  for (const workspace of me.workspaces) {
    const id = workspace.organizationId ?? null;
    if (id === original) {
      continue;
    }

    setWorkspace(id);
    try {
      return await apiFetch<Schemas["ContractDetail"]>(path);
    } catch {
      // Not in this one either.
    }
  }

  setWorkspace(original);
  return apiLoad<Schemas["ContractDetail"]>(path);
}

type View = "overview" | "document" | "file" | "checklist" | "versions" | "audit";

const allViews: { value: View; label: string; needsResult?: boolean; ownerOnly?: boolean }[] = [
  { value: "overview", label: "Tổng quan", needsResult: true },
  { value: "document", label: "Văn bản", needsResult: true },
  { value: "file", label: "File gốc" },
  { value: "checklist", label: "Checklist", needsResult: true },
  { value: "versions", label: "Phiên bản" },
  { value: "audit", label: "Nhật ký", ownerOnly: true },
];

// The contract workspace: status and result in the center (overview, clauses with margin notes, original
// file, checklist), the context in a dialog, the chat beside them. What it shows depends only on the data
// and the ?view= of the address, so reloading lands in the same place.
export default function ContractDetail({ loaderData }: Route.ComponentProps) {
  const { contract, clauses, result } = loaderData;
  const version = contract.version;
  const isOwner = contract.myRole === "owner";
  const navigate = useNavigate();
  const shareDialog = useRef<HTMLDialogElement>(null);
  const deleteDialog = useRef<HTMLDialogElement>(null);
  const [sharing, setSharing] = useState(false);
  const remove = useAction();
  const renameDialog = useRef<HTMLDialogElement>(null);
  const [newName, setNewName] = useState(contract.contractName);
  const rename = useAction();
  const job = contract.latestJob;
  const { revalidate } = useRevalidator();
  const contextDialog = useRef<HTMLDialogElement>(null);
  const rerun = useAction();
  const [searchParams, setSearchParams] = useSearchParams();
  const [selectedFindingId, setSelectedFindingId] = useState<string | null>(null);
  const [focus, setFocus] = useState<{ clauseKey: string } | null>(null);

  const refresh = useCallback(() => void revalidate(), [revalidate]);
  const running = job?.status === "running";
  const hasContext = version.context !== null;
  const hasResult = result !== null && !running;
  const views = allViews.filter((v) => (!v.needsResult || hasResult) && (!v.ownerOnly || isOwner));
  const requested = searchParams.get("view") as View | null;
  const view: View = views.some((v) => v.value === requested) ? requested! : hasResult ? "overview" : "file";

  function setView(next: View) {
    setSearchParams((params) => {
      params.set("view", next);
      return params;
    }, { replace: true, preventScrollReset: true });
  }

  const clauseName = useCallback(
    (key: string) => {
      const clause = clauses.find((c) => c.clauseKey === key);
      return clause ? [clause.clauseNumber, clause.heading].filter(Boolean).join(". ") || key : key;
    },
    [clauses],
  );

  // FR-VIEW-011, FR-CHAT-004: from a finding, a check or (later) a chat citation to the clause.
  const goToClause = useCallback(
    (clauseKey: string) => {
      setView("document");
      setFocus({ clauseKey });
    },
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [],
  );

  // UC-CTR-001 step 6: a contract without a context asks for it on arrival. Once: the user may close the
  // dialog to read the contract first and open it again from the top bar.
  const asksForContext = !hasContext && !running;
  useEffect(() => {
    if (asksForContext && !contextDialog.current?.open) {
      contextDialog.current?.showModal();
    }
  }, [asksForContext]);

  function runAgain() {
    void rerun.run(async () => {
      await apiPut(`/contracts/${contract.contractId}/versions/${version.contractVersionId}/context`, version.context);
      await revalidate();
    });
  }

  // F-CTR-006 with CR-CTR-001: to the trash, restorable for 30 days. Still asks first (DESIGN: a modal
  // only for deleting and sharing).
  function deleteContract() {
    void remove.run(async () => {
      await apiFetch(`/contracts/${contract.contractId}`, { method: "DELETE" });
      await navigate("/contracts");
    });
  }

  function openRename() {
    setNewName(contract.contractName);
    rename.clearError();
    renameDialog.current?.showModal();
  }

  function saveName(event: FormEvent) {
    event.preventDefault();
    void rename.run(async () => {
      await apiPut(`/contracts/${contract.contractId}/name`, { name: newName });
      renameDialog.current?.close();
      refresh();
    });
  }

  const subtitle = [approvalStatusLabels[contract.approvalStatus], `V${version.versionNumber}`, version.originalFileName, version.pageCount !== null && `${version.pageCount} trang`]
    .filter(Boolean)
    .join(" · ");

  const findings = result?.findings ?? [];
  const highlights = pdfHighlights(clauses, findings, clauseName);
  const openClauseFinding = (clauseKey: string) => {
    const worst = findings.filter((f) => f.clauseKey === clauseKey).sort((a, b) => severityOrder.indexOf(a.severity) - severityOrder.indexOf(b.severity))[0];
    setSelectedFindingId(worst?.findingId ?? null);
    goToClause(clauseKey);
  };

  const fileViewer = (
    <FileViewer
      key={version.contractVersionId}
      contractId={contract.contractId}
      versionId={version.contractVersionId}
      fileType={version.fileType}
      highlights={highlights}
      onSelectClause={openClauseFinding}
    />
  );

  return (
    <>
      <ContractWorkspace
        title={contract.contractName}
        subtitle={subtitle}
        chat={<ChatPanel contractId={contract.contractId} hasResult={hasResult} clauseName={clauseName} onCitation={goToClause} />}
        actions={
          <>
            <ApprovalControl contractId={contract.contractId} status={contract.approvalStatus} onChanged={refresh} />
            {isOwner && (
              <Button variant="secondary" size="sm" onClick={openRename}>
                Đổi tên
              </Button>
            )}
            {isOwner && (
              <Button
                variant="secondary"
                size="sm"
                onClick={() => {
                  setSharing(true);
                  shareDialog.current?.showModal();
                }}
              >
                Chia sẻ
              </Button>
            )}
            {hasResult && contract.completedJobId && <ReportExport jobId={contract.completedJobId} />}
            <Button variant={hasContext ? "secondary" : "primary"} size="sm" disabled={running} onClick={() => contextDialog.current?.showModal()}>
              {hasContext ? "Ngữ cảnh" : "Khai báo ngữ cảnh"}
            </Button>
            {hasContext && !running && contract.analysisAvailable && (
              <Button variant="secondary" size="sm" loading={rerun.pending} onClick={runAgain}>
                {job ? "Chạy lại phân tích" : "Bắt đầu phân tích"}
              </Button>
            )}
          </>
        }
        document={
          <div className="flex flex-col gap-4">
            <div className="flex flex-col gap-3 empty:hidden [&:not(:empty)]:pt-3">
              {!contract.isLatestVersion && (
                <Notice tone="info">
                  Bạn đang xem phiên bản cũ V{version.versionNumber}.{" "}
                  <Link to={`/contracts/${contract.contractId}`} className="text-review-ink underline underline-offset-2">
                    Mở phiên bản mới nhất
                  </Link>
                </Notice>
              )}

              {!contract.analysisAvailable && !running && !hasResult && (
                <Notice tone="info">
                  Dịch vụ phân tích AI chưa sẵn sàng, nên chưa có điều khoản và đánh giá rủi ro. Hợp đồng và ngữ cảnh của bạn vẫn được lưu để phân tích sau.
                </Notice>
              )}

              {rerun.error !== null && <Notice tone="error">{errorMessage(rerun.error)}</Notice>}

              {running && <AnalysisProgress key={job.jobId} job={job} onFinished={refresh} />}

              {job?.status === "failed" && (
                <Notice tone="error">
                  <p className="font-semibold">Phân tích thất bại</p>
                  <p>{failureMessage(job.failureReason)}</p>
                  <p className="mt-1 text-ink-secondary">
                    {contract.completedJobId ? "Bên dưới là kết quả của lần phân tích thành công trước đó." : "Không có kết quả dở dang nào được lưu. Bạn có thể chạy lại phân tích."}
                  </p>
                </Notice>
              )}

              {job?.status === "cancelled" && (
                <Notice tone="info">
                  <p className="font-semibold">Đã dừng phân tích</p>
                  <p className="text-ink-secondary">
                    {contract.completedJobId ? "Bên dưới là kết quả của lần phân tích thành công trước đó." : "Bấm “Chạy lại phân tích” khi bạn muốn phân tích lại."}
                  </p>
                </Notice>
              )}
            </div>

            {/* The tabs are always there: versions and the log exist before any analysis result. */}
            {
              <>
                <div className="sticky top-0 z-10 bg-desk">
                  <Tabs id="result" label="Kết quả phân tích" tabs={views} value={view} onChange={setView} />
                </div>
                <div id="result-panel" role="tabpanel" aria-labelledby={`result-tab-${view}`}>
                  {view === "overview" && result && (
                    <ResultOverview
                      result={result}
                      clauseName={clauseName}
                      selectedFindingId={selectedFindingId}
                      onSelectFinding={setSelectedFindingId}
                      onGoToClause={goToClause}
                      onChanged={refresh}
                      metadata={
                        <MetadataPanel
                          contractId={contract.contractId}
                          versionId={version.contractVersionId}
                          extracted={version.extractedMetadata}
                          edited={version.editedMetadata}
                          onSaved={refresh}
                        />
                      }
                    />
                  )}
                  {view === "document" && result && (
                    <ContractSheet clauses={clauses} findings={findings} focus={focus} selectedFindingId={selectedFindingId} onSelectFinding={setSelectedFindingId} onChanged={refresh} />
                  )}
                  {view === "file" && fileViewer}
                  {view === "checklist" && result && <Checklist checks={result.checks} clauseName={clauseName} onGoToClause={goToClause} />}
                  {view === "versions" && (
                    <VersionsPanel
                      contractId={contract.contractId}
                      versions={contract.versions}
                      shownVersionId={version.contractVersionId}
                      busy={running}
                      onUploaded={async () => {
                        await navigate(`/contracts/${contract.contractId}`);
                      }}
                    />
                  )}
                  {view === "audit" && <AuditPanel contractId={contract.contractId} />}
                  {isOwner && (
                    <section aria-labelledby="danger-title" className="mx-auto mt-10 flex w-full max-w-sheet flex-col gap-2 border-t border-rule pt-4 empty:hidden">
                      {view === "versions" && (
                        <>
                          <h2 id="danger-title" className="text-label text-ink-secondary">
                            Xóa hợp đồng
                          </h2>
                          <Button variant="danger" size="sm" className="self-start" disabled={running} onClick={() => deleteDialog.current?.showModal()}>
                            Chuyển vào thùng rác
                          </Button>
                          {running && <p className="text-caption text-ink-muted">Chờ phân tích xong rồi mới xóa được.</p>}
                        </>
                      )}
                    </section>
                  )}
                </div>
              </>
            }
          </div>
        }
      />

      <dialog
        ref={contextDialog}
        aria-labelledby="context-title"
        className="m-auto max-h-[calc(100vh-2rem)] w-[min(34rem,calc(100vw-2rem))] rounded-md border border-rule bg-sheet p-6 text-ink backdrop:bg-ink/30"
      >
        <h2 id="context-title" className="mb-3 text-title">
          Khai báo ngữ cảnh hợp đồng
        </h2>
        <ContextForm
          contractId={contract.contractId}
          versionId={version.contractVersionId}
          initial={version.context ?? contract.suggestedContext}
          analysisAvailable={contract.analysisAvailable}
          onSaved={() => {
            contextDialog.current?.close();
            refresh();
          }}
          onCancel={() => contextDialog.current?.close()}
        />
      </dialog>

      <dialog
        ref={shareDialog}
        aria-labelledby="share-title"
        // React passes a nested dialog's close (the confirm inside) up to here: only react to our own.
        onClose={(e) => e.target === e.currentTarget && setSharing(false)}
        className="m-auto max-h-[calc(100vh-2rem)] w-[min(36rem,calc(100vw-2rem))] rounded-md border border-rule bg-sheet p-6 text-ink backdrop:bg-ink/30"
      >
        <h2 id="share-title" className="mb-3 text-title">
          Chia sẻ “{contract.contractName}”
        </h2>
        {sharing && <ShareDialog contractId={contract.contractId} organizationId={contract.organizationId} onChanged={refresh} onClose={() => shareDialog.current?.close()} />}
      </dialog>

      <dialog
        ref={deleteDialog}
        aria-labelledby="delete-title"
        className="m-auto w-[min(30rem,calc(100vw-2rem))] rounded-md border border-rule bg-sheet p-6 text-ink backdrop:bg-ink/30"
      >
        <h2 id="delete-title" className="mb-2 text-title">
          Chuyển hợp đồng vào thùng rác?
        </h2>
        <p className="mb-4 max-w-prose">
          “{contract.contractName}” sẽ biến mất khỏi thư viện, kể cả với người được chia sẻ. Bạn khôi phục được trong 30 ngày từ trang Thùng rác; sau đó hợp đồng và mọi dữ liệu của nó bị xóa vĩnh viễn.
        </p>
        {remove.error !== null && (
          <div className="mb-3">
            <Notice tone="error">{errorMessage(remove.error)}</Notice>
          </div>
        )}
        <div className="flex justify-end gap-2">
          <Button variant="ghost" onClick={() => deleteDialog.current?.close()}>
            Hủy
          </Button>
          <Button variant="danger" className="border border-severity-critical/40" loading={remove.pending} onClick={deleteContract}>
            Chuyển vào thùng rác
          </Button>
        </div>
      </dialog>

      <dialog
        ref={renameDialog}
        aria-labelledby="rename-title"
        className="m-auto w-[min(30rem,calc(100vw-2rem))] rounded-md border border-rule bg-sheet p-6 text-ink backdrop:bg-ink/30"
      >
        <h2 id="rename-title" className="mb-3 text-title">
          Đổi tên hợp đồng
        </h2>
        <form onSubmit={saveName} className="flex flex-col gap-4">
          <TextField label="Tên hợp đồng" required maxLength={300} value={newName} onChange={(e) => setNewName(e.target.value)} />
          {rename.error !== null && <Notice tone="error">{errorMessage(rename.error)}</Notice>}
          <div className="flex justify-end gap-2">
            <Button type="button" variant="ghost" onClick={() => renameDialog.current?.close()}>
              Hủy
            </Button>
            <Button type="submit" loading={rename.pending} disabled={!newName.trim() || newName.trim() === contract.contractName}>
              Lưu
            </Button>
          </div>
        </form>
      </dialog>
    </>
  );
}

// Every region of every clause that has findings, washed in the clause's worst severity.
function pdfHighlights(clauses: Schemas["ClauseItem"][], findings: Schemas["FindingItem"][], clauseName: (key: string) => string): PdfHighlight[] {
  return clauses.flatMap((clause) => {
    const worst = severityOrder.find((s) => findings.some((f) => f.clauseKey === clause.clauseKey && f.severity === s));
    if (!worst) {
      return [];
    }

    return clause.location.regions.map((r) => ({
      clauseKey: clause.clauseKey,
      page: Number(r.page),
      x: Number(r.x),
      y: Number(r.y),
      width: Number(r.width),
      height: Number(r.height),
      severity: worst,
      label: clauseName(clause.clauseKey),
    }));
  });
}
