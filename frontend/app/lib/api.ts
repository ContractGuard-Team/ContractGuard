// Thin wrapper around fetch for the ContractGuard API (SD-09: REST under /api/v1).
// - Same origin: in development Vite forwards /api to the .NET API, in production CloudFront does.
// - credentials "include" sends the httpOnly session cookie (SD-04).
// - Errors come back as Problem Details (RFC 9457) with an ERR-* code (SRS §14.1) and are turned
//   into ApiError so pages can react to the code, not to English text.

export const API_BASE = "/api/v1";

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  code?: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  readonly status: number;
  readonly code: string | undefined;
  readonly fieldErrors: Record<string, string[]>;

  constructor(status: number, problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? `Request failed with status ${status}`);
    this.name = "ApiError";
    this.status = status;
    this.code = problem.code;
    this.fieldErrors = problem.errors ?? {};
  }
}

export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers);
  if (init.body !== undefined && !(init.body instanceof FormData) && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(`${API_BASE}${path}`, { ...init, headers, credentials: "include" });
  if (!response.ok) {
    throw new ApiError(response.status, await readProblem(response));
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

async function readProblem(response: Response): Promise<ProblemDetails> {
  const type = response.headers.get("Content-Type") ?? "";
  if (type.includes("json")) {
    try {
      return (await response.json()) as ProblemDetails;
    } catch {
      // Fall through: the body was not valid JSON.
    }
  }

  return { status: response.status, title: response.statusText };
}
