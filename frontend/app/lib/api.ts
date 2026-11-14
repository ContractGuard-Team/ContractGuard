// Thin wrapper around fetch for the ContractGuard API (SD-09: REST under /api/v1).
// - Same origin: in development Vite forwards /api to the .NET API, in production CloudFront does.
// - credentials "include" sends the httpOnly session cookie (SD-04).
// - Write requests carry the CSRF token in a header (SD-04).
// - Errors come back as Problem Details (RFC 9457) with an ERR-* code (SRS §14.1) and are turned
//   into ApiError so pages can react to the code, not to English text.

import { data, redirect } from "react-router";

import type { components } from "./api-types";
import { currentWorkspace, setWorkspace } from "./workspace";

export const API_BASE = "/api/v1";

// Shapes generated from the API's OpenAPI document: npm run gen:api
export type Schemas = components["schemas"];

const CSRF_ERROR = "ERR-GEN-CSRF";

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

// The token is tied to who is signed in, so it is thrown away when that changes.
let csrfToken: string | null = null;

export function forgetCsrfToken() {
  csrfToken = null;
}

export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  return send<T>(path, init, true);
}

export function apiPost<T = void>(path: string, body?: unknown): Promise<T> {
  return apiFetch<T>(path, { method: "POST", body: JSON.stringify(body ?? {}) });
}

export function apiPut<T = void>(path: string, body: unknown): Promise<T> {
  return apiFetch<T>(path, { method: "PUT", body: JSON.stringify(body) });
}

// One parsed server-sent event (SD-15).
export interface SseEvent {
  event: string;
  data: string;
}

// Splits a text buffer into complete SSE events; returns the unfinished tail to keep for the next chunk.
export function parseSse(buffer: string): { events: SseEvent[]; rest: string } {
  const blocks = buffer.replace(/\r\n/g, "\n").split("\n\n");
  const rest = blocks.pop() ?? "";
  const events = blocks
    .map((block) => {
      let event = "message";
      const data: string[] = [];
      for (const line of block.split("\n")) {
        if (line.startsWith("event:")) {
          event = line.slice(6).trim();
        } else if (line.startsWith("data:")) {
          data.push(line.slice(5).replace(/^ /, ""));
        }
      }
      return { event, data: data.join("\n") };
    })
    .filter((e) => e.data !== "" || e.event !== "message");
  return { events, rest };
}

// POST that answers with an SSE stream (chat). Errors before the stream are Problem Details as usual.
export async function apiStream(path: string, body: unknown, onEvent: (event: SseEvent) => void, signal?: AbortSignal): Promise<void> {
  const send = async (retry: boolean): Promise<Response> => {
    const response = await fetch(`${API_BASE}${path}`, {
      method: "POST",
      credentials: "include",
      headers: withWorkspace(new Headers({ "Content-Type": "application/json", Accept: "text/event-stream", "X-CSRF-TOKEN": await getCsrfToken() })),
      body: JSON.stringify(body),
      signal,
    });
    if (!response.ok) {
      const problem = await readProblem(response);
      if (retry && problem.code === CSRF_ERROR) {
        forgetCsrfToken();
        return send(false);
      }

      throw new ApiError(response.status, problem);
    }

    return response;
  };

  const response = await send(true);
  const reader = response.body!.pipeThrough(new TextDecoderStream()).getReader();
  let buffer = "";
  for (;;) {
    const { value, done } = await reader.read();
    if (done) {
      break;
    }

    const parsed = parseSse(buffer + value);
    buffer = parsed.rest;
    parsed.events.forEach(onEvent);
  }

  parseSse(buffer + "\n\n").events.forEach(onEvent);
}

// A file from the API (report export). Errors arrive as Problem Details like any other call, so the
// page can say what failed instead of the browser opening a JSON error page.
export async function apiDownload(path: string): Promise<{ blob: Blob; fileName: string }> {
  const response = await fetch(`${API_BASE}${path}`, { credentials: "include", headers: withWorkspace(new Headers()) });
  if (!response.ok) {
    throw new ApiError(response.status, await readProblem(response));
  }

  const disposition = response.headers.get("Content-Disposition") ?? "";
  const encoded = /filename\*=UTF-8''([^;]+)/i.exec(disposition)?.[1];
  const plain = /filename="?([^";]+)"?/i.exec(disposition)?.[1];
  return { blob: await response.blob(), fileName: encoded ? decodeURIComponent(encoded) : (plain ?? "download") };
}

// Hands a blob to the browser as a download, without leaving the page.
export function saveBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  link.click();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}

// For route loaders: no session goes to the login page, a missing or foreign resource to the 404 page.
export async function apiLoad<T>(path: string): Promise<T> {
  try {
    return await apiFetch<T>(path);
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      throw redirect("/login");
    }

    if (error instanceof ApiError && error.status === 404) {
      throw data(null, { status: 404 });
    }

    // Removed from the organization since it was chosen (SEC-AUTHZ-008): back to the personal workspace.
    if (error instanceof ApiError && error.code === "ERR-SEC-001" && currentWorkspace() !== null) {
      setWorkspace(null);
      throw redirect("/contracts");
    }

    throw error;
  }
}

async function send<T>(path: string, init: RequestInit, retryOnCsrfError: boolean): Promise<T> {
  const isWrite = !["GET", "HEAD", "OPTIONS"].includes((init.method ?? "GET").toUpperCase());
  const headers = new Headers(init.headers);
  if (init.body !== undefined && !(init.body instanceof FormData) && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  if (isWrite) {
    headers.set("X-CSRF-TOKEN", await getCsrfToken());
  }

  withWorkspace(headers);

  const response = await fetch(`${API_BASE}${path}`, { ...init, headers, credentials: "include" });
  if (!response.ok) {
    const problem = await readProblem(response);

    // The session changed since the token was issued (logged in or expired in another tab).
    if (isWrite && retryOnCsrfError && problem.code === CSRF_ERROR) {
      forgetCsrfToken();
      return send<T>(path, init, false);
    }

    throw new ApiError(response.status, problem);
  }

  if (path === "/auth/login" || path === "/auth/logout") {
    forgetCsrfToken();
  }

  if (response.status === 202 || response.status === 204) {
    const text = await response.text();
    return (text ? JSON.parse(text) : undefined) as T;
  }

  return (await response.json()) as T;
}

function withWorkspace(headers: Headers): Headers {
  const workspace = currentWorkspace();
  if (workspace) {
    headers.set("X-Workspace-Id", workspace);
  }

  return headers;
}

async function getCsrfToken(): Promise<string> {
  if (csrfToken === null) {
    const response = await fetch(`${API_BASE}/auth/csrf`, { credentials: "include" });
    if (!response.ok) {
      throw new ApiError(response.status, await readProblem(response));
    }

    csrfToken = ((await response.json()) as Schemas["CsrfTokenResponse"]).token;
  }

  return csrfToken;
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
