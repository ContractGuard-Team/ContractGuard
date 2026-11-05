import { afterEach, describe, expect, it, vi } from "vitest";

import { ApiError, apiFetch } from "./api";

function mockFetch(response: Response) {
  const spy = vi.fn().mockResolvedValue(response);
  vi.stubGlobal("fetch", spy);
  return spy;
}

describe("apiFetch", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("calls /api/v1 with the session cookie and parses JSON", async () => {
    const spy = mockFetch(Response.json({ ok: true }));

    const result = await apiFetch<{ ok: boolean }>("/contracts");

    expect(result).toEqual({ ok: true });
    const [url, init] = spy.mock.calls[0] as [string, RequestInit];
    expect(url).toBe("/api/v1/contracts");
    expect(init.credentials).toBe("include");
  });

  it("turns Problem Details into ApiError with the ERR code", async () => {
    mockFetch(
      new Response(JSON.stringify({ status: 409, title: "Conflict", detail: "Email đã được đăng ký.", code: "ERR-AUTH-001" }), {
        status: 409,
        headers: { "Content-Type": "application/problem+json" },
      }),
    );

    const error = await apiFetch("/auth/register", { method: "POST", body: "{}" }).catch((e: unknown) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).status).toBe(409);
    expect((error as ApiError).code).toBe("ERR-AUTH-001");
    expect((error as ApiError).message).toBe("Email đã được đăng ký.");
  });

  it("survives an error response that is not JSON", async () => {
    mockFetch(new Response("Bad Gateway", { status: 502, statusText: "Bad Gateway" }));

    const error = (await apiFetch("/contracts").catch((e: unknown) => e)) as ApiError;

    expect(error.status).toBe(502);
    expect(error.code).toBeUndefined();
  });
});
