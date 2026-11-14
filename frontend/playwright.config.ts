import { readFileSync } from "node:fs";

import { defineConfig, devices } from "@playwright/test";

// E2E against the running local stack (docker compose --profile app), never against a dev server
// started here: the point is to test what users run. See docs/devlog/Runbook.md, section E2E.
// The API container must run with ANALYSIS_WORKER_ENABLED=true and CHAT_RESPONDER=fake.

// The repo's .env may move Mailpit to other ports on this machine.
function envFile(name: string): string | undefined {
  try {
    const line = readFileSync(new URL("../.env", import.meta.url), "utf8")
      .split(/\r?\n/)
      .find((l) => l.trim().startsWith(`${name}`));
    return line?.split("=")[1]?.trim();
  } catch {
    return undefined;
  }
}

process.env.MAILPIT_URL ??= `http://localhost:${envFile("MAILPIT_UI_PORT") ?? "8025"}`;

export default defineConfig({
  testDir: "./e2e",
  // One stack, shared state (LocalStack queues): run the scenarios one after another.
  workers: 1,
  fullyParallel: false,
  timeout: 90_000,
  expect: { timeout: 15_000 },
  retries: 0,
  reporter: [["list"], ["html", { open: "never" }]],
  use: {
    baseURL: process.env.E2E_BASE_URL ?? "http://localhost:5173",
    locale: "vi-VN",
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"], viewport: { width: 1440, height: 900 } } }],
});
