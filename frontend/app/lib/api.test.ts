import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { ApiError, apiFetch, apiPost, forgetCsrfToken } from "./api";

function problem(status: number, code: string, detail = "Problem") {
  return new Response(JSON.stringify({ status, detail, code }), {
    status,
    headers: { "Content-Type": "application/problem+json" },
  });
}

// Answers /auth/csrf with token-1, token-2, ... and everything else from the given responses.
function mockApi(...responses: Response[]) {
  let tokens = 0;
  const spy = vi.fn(async (url: string) => {
    if (url.endsWith("/auth/csrf")) {
      tokens += 1;
      return Response.json({ token: `token-${tokens}` });
    }

    return responses.shift() ?? new Response(null, { status: 204 });
  });
  vi.stubGlobal("fetch", spy);
  return spy;
}

function tokensSentTo(spy: ReturnType<typeof mockApi>, path: string) {
  return spy.mock.calls
    .filter(([url]) => url === `/api/v1${path}`)
    .map((call) => new Headers((call as unknown as [string, RequestInit])[1].headers).get("X-CSRF-TOKEN"));
}

describe("apiFetch", () => {
  beforeEach(() => {
    forgetCsrfToken();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("calls /api/v1 with the session cookie and parses JSON", async () => {
    const spy = mockApi(Response.json({ ok: true }));

    const result = await apiFetch<{ ok: boolean }>("/contracts");

    expect(result).toEqual({ ok: true });
    const [url, init] = spy.mock.calls[0] as unknown as [string, RequestInit];
    expect(url).toBe("/api/v1/contracts");
    expect(init.credentials).toBe("include");
  });

  it("does not ask for a CSRF token on reads", async () => {
    const spy = mockApi(Response.json([]));

    await apiFetch("/contracts");

    expect(spy).toHaveBeenCalledTimes(1);
  });

  it("sends the CSRF token on writes and reuses it", async () => {
    const spy = mockApi();

    await apiPost("/uploads", {});
    await apiPost("/contracts", {});

    expect(tokensSentTo(spy, "/uploads")).toEqual(["token-1"]);
    expect(tokensSentTo(spy, "/contracts")).toEqual(["token-1"]);
  });

  it("gets a new token after login, because the token is tied to the user", async () => {
    const spy = mockApi();

    await apiPost("/auth/login", {});
    await apiPost("/contracts", {});

    expect(tokensSentTo(spy, "/auth/login")).toEqual(["token-1"]);
    expect(tokensSentTo(spy, "/contracts")).toEqual(["token-2"]);
  });

  it("retries once with a new token when the API refuses the old one", async () => {
    const spy = mockApi(problem(400, "ERR-GEN-CSRF"), Response.json({ jobId: "1" }));

    const result = await apiPost<{ jobId: string }>("/contracts", {});

    expect(result).toEqual({ jobId: "1" });
    expect(tokensSentTo(spy, "/contracts")).toEqual(["token-1", "token-2"]);
  });

  it("gives up after the second CSRF refusal", async () => {
    const spy = mockApi(problem(400, "ERR-GEN-CSRF"), problem(400, "ERR-GEN-CSRF"));

    const error = (await apiPost("/contracts", {}).catch((e: unknown) => e)) as ApiError;

    expect(error.code).toBe("ERR-GEN-CSRF");
    expect(tokensSentTo(spy, "/contracts")).toHaveLength(2);
  });

  it("turns Problem Details into ApiError with the ERR code", async () => {
    mockApi(problem(409, "ERR-AUTH-001", "Email đã được đăng ký."));

    const error = await apiPost("/auth/register", {}).catch((e: unknown) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).status).toBe(409);
    expect((error as ApiError).code).toBe("ERR-AUTH-001");
    expect((error as ApiError).message).toBe("Email đã được đăng ký.");
  });

  it("reads the body of a 202 and accepts an empty 204", async () => {
    mockApi(new Response(JSON.stringify({ jobId: "1" }), { status: 202 }), new Response(null, { status: 204 }));

    expect(await apiPost("/a", {})).toEqual({ jobId: "1" });
    expect(await apiPost("/b", {})).toBeUndefined();
  });

  it("survives an error response that is not JSON", async () => {
    mockApi(new Response("Bad Gateway", { status: 502, statusText: "Bad Gateway" }));

    const error = (await apiFetch("/contracts").catch((e: unknown) => e)) as ApiError;

    expect(error.status).toBe(502);
    expect(error.code).toBeUndefined();
  });
});
