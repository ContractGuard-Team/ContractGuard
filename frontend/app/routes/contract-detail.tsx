import { useCallback, useEffect, useRef } from "react";
import { useRevalidator } from "react-router";

import { AnalysisProgress } from "~/components/AnalysisProgress";
import { Button } from "~/components/Button";
import { ClauseList } from "~/components/ClauseList";
import { ContextForm } from "~/components/ContextForm";
import { ContractWorkspace } from "~/components/ContractWorkspace";
import { FileViewer } from "~/components/FileViewer";
import { Notice } from "~/components/Notice";
import { apiLoad, apiPut, type Schemas } from "~/lib/api";
import { errorMessage, failureMessage } from "~/lib/errors";
import { approvalStatusLabels } from "~/lib/labels";
import { useAction } from "~/lib/useAction";

import type { Route } from "./+types/contract-detail";

// Read by the app layout: this page takes the whole screen and the sidebar shrinks to icons.
export const handle = { workspace: true };

export function meta({ loaderData }: Route.MetaArgs) {
  return [{ title: `${loaderData?.contract.contractName ?? "Hợp đồng"} · ContractGuard` }];
}

export async function clientLoader({ params }: Route.ClientLoaderArgs) {
  const contract = await apiLoad<Schemas["ContractDetail"]>(`/contracts/${params.contractId}`);
  const clauses = contract.completedJobId ? await apiLoad<Schemas["ClauseItem"][]>(`/jobs/${contract.completedJobId}/clauses`) : [];
  return { contract, clauses };
}

// The contract workspace: the status, the file and the clauses in the center, the context in a dialog,
// the chat beside them. What it shows depends only on the data, so reloading lands in the right state.
export default function ContractDetail({ loaderData }: Route.ComponentProps) {
  const { contract, clauses } = loaderData;
  const version = contract.latestVersion;
  const job = contract.latestJob;
  const { revalidate } = useRevalidator();
  const contextDialog = useRef<HTMLDialogElement>(null);
  const rerun = useAction();

  const refresh = useCallback(() => void revalidate(), [revalidate]);
  const running = job?.status === "running";
  const hasContext = version.context !== null;

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

  const subtitle = [approvalStatusLabels[contract.approvalStatus], `V${version.versionNumber}`, version.originalFileName, version.pageCount !== null && `${version.pageCount} trang`]
    .filter(Boolean)
    .join(" · ");

  return (
    <>
      <ContractWorkspace
        title={contract.contractName}
        subtitle={subtitle}
        actions={
          <>
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
          <div className="flex flex-col gap-6">
            <div className="flex flex-col gap-3 empty:hidden [&:not(:empty)]:pt-3">
              {!contract.analysisAvailable && !running && (
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
            </div>

            <FileViewer
              key={version.contractVersionId}
              contractId={contract.contractId}
              versionId={version.contractVersionId}
              fileName={version.originalFileName}
              fileType={version.fileType}
            />

            {!running && clauses.length > 0 && <ClauseList clauses={clauses} />}
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
          initial={version.context}
          analysisAvailable={contract.analysisAvailable}
          onSaved={() => {
            contextDialog.current?.close();
            refresh();
          }}
          onCancel={() => contextDialog.current?.close()}
        />
      </dialog>
    </>
  );
}
