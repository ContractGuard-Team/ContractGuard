import { apiPost, type Schemas } from "./api";

const allowedExtensions = [".pdf", ".docx"];

export const acceptedFileTypes = allowedExtensions.join(",");
export const maxFileBytes = 20 * 1024 * 1024; // FR-UPL-004: exactly 20 MB is still accepted.

// Thrown when the file could not reach storage (network, or storage refused it).
export class UploadFailedError extends Error {}

// Checked in the browser only to answer at once. The API and storage check again: this is no security.
export function checkFile(file: File): string | null {
  const name = file.name.toLowerCase();
  if (!allowedExtensions.some((extension) => name.endsWith(extension))) {
    return "ERR-UPL-001";
  }

  return file.size > maxFileBytes ? "ERR-UPL-003" : null;
}

// SD-08: ask the API for a signed form, send the file straight to storage, then let the API create
// the contract from what actually arrived.
export async function uploadContract(file: File): Promise<Schemas["CreatedContract"]> {
  const uploadId = await uploadToStorage(file);
  return apiPost<Schemas["CreatedContract"]>("/contracts", { uploadId, fileName: file.name });
}

// F-CTR-005: the same upload, attached to an existing contract as its next version.
export async function uploadVersion(contractId: string, file: File): Promise<Schemas["CreatedContract"]> {
  const uploadId = await uploadToStorage(file);
  return apiPost<Schemas["CreatedContract"]>(`/contracts/${contractId}/versions`, { uploadId, fileName: file.name });
}

async function uploadToStorage(file: File): Promise<string> {
  const ticket = await apiPost<Schemas["UploadTicket"]>("/uploads", { fileName: file.name, sizeBytes: file.size });

  const form = new FormData();
  for (const [name, value] of Object.entries(ticket.fields)) {
    form.append(name, value);
  }

  // S3 ignores every field after the file, so the file goes last.
  form.append("file", file);

  let stored: Response;
  try {
    stored = await fetch(ticket.url, { method: "POST", body: form });
  } catch {
    throw new UploadFailedError();
  }

  if (!stored.ok) {
    throw new UploadFailedError();
  }

  return ticket.uploadId;
}
