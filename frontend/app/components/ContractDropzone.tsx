import { useState, type ChangeEvent, type DragEvent } from "react";

import { ApiError } from "~/lib/api";
import { errorMessage } from "~/lib/errors";
import type { Schemas } from "~/lib/api";
import { acceptedFileTypes, checkFile, UploadFailedError, uploadContract } from "~/lib/upload";

import { Notice } from "./Notice";

// UI-GEN-003, FR-CTR-001: drop one file or pick it. The page decides what follows a successful upload.
// upload: what to do with the file; a new contract by default, a new version on the contract page.
export function ContractDropzone({
  onUploaded,
  upload = uploadContract,
}: {
  onUploaded: (contractId: string) => Promise<void>;
  upload?: (file: File) => Promise<Schemas["CreatedContract"]>;
}) {
  const [dragging, setDragging] = useState(false);
  const [uploading, setUploading] = useState<string | null>(null);
  const [uploaded, setUploaded] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function start(files: FileList | null) {
    const file = files?.[0];
    if (!file || uploading) {
      return;
    }

    setError(null);
    setUploaded(null);
    if (files.length > 1) {
      setError("Mỗi lần chỉ tải lên một file.");
      return;
    }

    const code = checkFile(file);
    if (code) {
      setError(errorMessage(new ApiError(400, { code })));
      return;
    }

    setUploading(file.name);
    try {
      const created = await upload(file);
      await onUploaded(created.contractId);
      setUploaded(file.name);
    } catch (caught) {
      setError(caught instanceof UploadFailedError ? "Chưa tải được file lên. Vui lòng thử lại." : errorMessage(caught));
    } finally {
      setUploading(null);
    }
  }

  function onDrop(event: DragEvent) {
    event.preventDefault();
    setDragging(false);
    void start(event.dataTransfer.files);
  }

  function onPick(event: ChangeEvent<HTMLInputElement>) {
    const files = event.target.files;
    void start(files).finally(() => {
      // Lets the user pick the same file again after an error.
      event.target.value = "";
    });
  }

  return (
    <div className="flex flex-col gap-3">
      <label
        onDragOver={(event) => {
          event.preventDefault();
          setDragging(true);
        }}
        onDragLeave={() => setDragging(false)}
        onDrop={onDrop}
        className={`flex cursor-pointer flex-col items-center gap-2 rounded-md border border-dashed px-6 py-8 text-center transition-colors duration-150 ease-out-quart focus-within:outline-2 focus-within:outline-offset-2 focus-within:outline-review-ink ${
          dragging ? "border-review-ink bg-review-ink-wash" : "border-rule-strong bg-sheet hover:bg-desk"
        } ${uploading ? "cursor-progress" : ""}`}
      >
        <input
          type="file"
          accept={acceptedFileTypes}
          className="sr-only"
          disabled={uploading !== null}
          onChange={onPick}
          data-testid="contract-file"
        />
        {uploading ? (
          <span role="status" className="text-title">
            Đang tải lên {uploading}…
          </span>
        ) : (
          <>
            <span className="text-title">Kéo thả file hợp đồng vào đây, hoặc bấm để chọn file</span>
            <span className="text-ink-muted">PDF hoặc DOCX, tối đa 20 MB và 100 trang</span>
          </>
        )}
      </label>
      {error && <Notice tone="error">{error}</Notice>}
      {uploaded && <Notice tone="info">Đã tải lên {uploaded}.</Notice>}
    </div>
  );
}
