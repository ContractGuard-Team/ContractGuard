import { lazy, Suspense, useEffect, useState } from "react";

import { ApiError, apiFetch, type Schemas } from "~/lib/api";
import { errorMessage } from "~/lib/errors";
import { useAction } from "~/lib/useAction";

import { Button } from "./Button";
import { Notice } from "./Notice";
import type { PdfHighlight } from "./PdfViewer";
import { ViewerSkeleton } from "./ViewerParts";

// Loaded only when a contract is opened: both libraries are large and PDF.js needs a browser.
const PdfViewer = lazy(() => import("./PdfViewer"));
const DocxViewer = lazy(() => import("./DocxViewer"));

function fileError(error: unknown) {
  return error instanceof ApiError && error.status === 404
    ? "Không tìm thấy file gốc trong kho lưu trữ, nên không xem hay tải được. Thông tin hợp đồng vẫn còn."
    : errorMessage(error);
}

// The original file of a version, readable in the page and downloadable (F-VIEW-002). Highlights are
// drawn on PDFs only: DOCX and images have no page coordinates in the browser renderer.
export function FileViewer({
  contractId,
  versionId,
  fileName,
  fileType,
  highlights,
  onSelectClause,
}: {
  contractId: string;
  versionId: string;
  fileName: string;
  fileType: Schemas["FileType"];
  highlights?: PdfHighlight[];
  onSelectClause?: (clauseKey: string) => void;
}) {
  const path = `/contracts/${contractId}/versions/${versionId}/file`;
  const [url, setUrl] = useState<string | null>(null);
  const [linkError, setLinkError] = useState<unknown>(null);
  const [downloadStarted, setDownloadStarted] = useState(false);
  const download = useAction();

  useEffect(() => {
    let current = true;
    apiFetch<Schemas["ContractFileLink"]>(path)
      .then((link) => current && setUrl(link.url))
      .catch((error: unknown) => current && setLinkError(error));
    return () => {
      current = false;
    };
  }, [path]);

  // A new link for each click, because the one used for viewing may have expired. Storage answers
  // it as an attachment, so the browser saves the file and stays on this page. The page cannot see
  // where the browser saves it, so it says where to look.
  function onDownload() {
    setDownloadStarted(false);
    void download.run(async () => {
      const link = await apiFetch<Schemas["ContractFileLink"]>(path);
      window.location.assign(link.url);
      setDownloadStarted(true);
    });
  }

  return (
    <section aria-labelledby="file-title" className="flex flex-col gap-3">
      {/* Padding here, not on the scrolling area: the toolbar below would stick under that padding. */}
      <div className="flex flex-wrap items-center justify-between gap-3 pt-3">
        <h2 id="file-title" className="text-title">
          Văn bản gốc
        </h2>
        {linkError === null && (
          <div className="flex flex-wrap items-center justify-end gap-3">
            <p role="status" className="text-caption text-ink-secondary">
              {downloadStarted && "Đang tải xuống. Xem file trong danh sách tải về của trình duyệt (Ctrl+J)."}
            </p>
            <Button variant="secondary" size="sm" loading={download.pending} onClick={onDownload}>
              Tải xuống
            </Button>
          </div>
        )}
      </div>

      {linkError !== null && <Notice tone="error">{fileError(linkError)}</Notice>}
      {download.error !== null && <Notice tone="error">{fileError(download.error)}</Notice>}
      {url === null && linkError === null && <ViewerSkeleton />}
      {url !== null && (
        <Suspense fallback={<ViewerSkeleton />}>
          {fileType === "pdf" && <PdfViewer url={url} highlights={highlights} onSelectClause={onSelectClause} />}
          {fileType === "docx" && <DocxViewer url={url} />}
        </Suspense>
      )}
    </section>
  );
}
