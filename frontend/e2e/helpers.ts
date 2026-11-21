import { execFileSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

import { expect, type APIRequestContext, type Page } from "@playwright/test";

// Helpers for the e2e scenarios. Each run creates its own accounts (random emails at example.com),
// so the shared local stack and the user's own data are never touched.

export const password = "Mat-khau-e2e-1";
const repoRoot = fileURLToPath(new URL("../../", import.meta.url));
export const sampleContract = `${repoRoot}contracts/examples/sample-contract.pdf`;

export function newEmail(tag: string) {
  return `e2e-${tag}-${Date.now()}-${Math.floor(Math.random() * 1e6)}@example.com`;
}

// The newest email to this address, read through Mailpit's API.
export async function lastEmail(request: APIRequestContext, to: string): Promise<string> {
  const mailpit = process.env.MAILPIT_URL!;
  let id: string | undefined;
  await expect
    .poll(async () => {
      const search = await (await request.get(`${mailpit}/api/v1/search`, { params: { query: `to:"${to}"` } })).json();
      id = search.messages?.[0]?.ID;
      return id;
    })
    .toBeTruthy();
  const message = await (await request.get(`${mailpit}/api/v1/message/${id}`)).json();
  return message.Text as string;
}

export function linkIn(text: string, path: string): string {
  const match = new RegExp(`https?://[^\\s]+${path}[^\\s]*`).exec(text);
  if (!match) {
    throw new Error(`No ${path} link in email: ${text}`);
  }

  // Emails point at the configured frontend; the test may run on another port.
  return new URL(match[0]).pathname + new URL(match[0]).search;
}

// Register through the UI, open the verification link from Mailpit, log in.
export async function signUp(page: Page, tag: string, name: string): Promise<string> {
  const email = newEmail(tag);
  await page.goto("/register");
  await page.getByLabel("Họ và tên").fill(name);
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Mật khẩu", { exact: true }).fill(password);
  await page.getByRole("button", { name: "Đăng ký" }).click();
  await expect(page.getByRole("heading", { name: "Kiểm tra email của bạn" })).toBeVisible();

  await page.goto(linkIn(await lastEmail(page.request, email), "/verify-email"));
  await expect(page).toHaveURL(/\/login/);
  await logIn(page, email);
  return email;
}

export async function logIn(page: Page, email: string, next?: string) {
  await page.goto(next ? `/login?next=${encodeURIComponent(next)}` : "/login");
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Mật khẩu").fill(password);
  await page.getByRole("button", { name: "Đăng nhập" }).click();
  await expect(page).not.toHaveURL(/\/login/);
}

export async function logOut(page: Page) {
  await page.getByRole("button", { name: "Đăng xuất" }).click();
  await expect(page).toHaveURL(/\/login/);
}

// Upload the sample PDF from the library page and open it. Returns the contract id.
export async function uploadSample(page: Page): Promise<string> {
  await page.goto("/contracts");
  await page.locator('input[type="file"]').setInputFiles(sampleContract);
  await expect(page.getByText("Đã tải lên sample-contract.pdf.")).toBeVisible();
  await page.getByRole("link", { name: "sample-contract" }).first().click();
  await expect(page).toHaveURL(/\/contracts\/[0-9a-f-]+/);
  return page.url().split("/contracts/")[1]!.split(/[?#]/)[0]!;
}

// Fill the context dialog that opens on a contract without context and start the analysis.
export async function confirmContext(page: Page) {
  const dialog = page.getByRole("dialog", { name: "Khai báo ngữ cảnh hợp đồng" });
  await expect(dialog).toBeVisible();
  await dialog.getByRole("radio", { name: "Bên mua / khách hàng" }).check();
  await dialog.getByLabel("Loại hợp đồng").selectOption({ label: "Hợp đồng dịch vụ" });
  await dialog.getByRole("radio", { name: "Có" }).check();
  await dialog.getByRole("textbox", { name: "Tên đối tác (không bắt buộc)" }).fill("Example Cloud Pte. Ltd.");
  await dialog.getByRole("button", { name: "Xác nhận và bắt đầu phân tích" }).click();
  await expect(dialog).toBeHidden();
}

// Plays the AI worker for the newest job of the contract: the example extraction result goes to
// LocalStack S3 and a "completed" message to the results queue, like scripts/dev/send-analysis-result.
export async function completeAnalysis(page: Page, contractId: string) {
  const detail = await (await page.request.get(`/api/v1/contracts/${contractId}`, { headers: await workspaceHeader(page) })).json();
  const jobId: string = detail.latestJob.jobId;
  const json = readFileSync(`${repoRoot}contracts/examples/extraction.v2.example.json`, "utf8")
    .replace("{{JOB_ID}}", jobId)
    .replace("{{CONTRACT_ID}}", contractId)
    .replace("{{CONTRACT_VERSION}}", String(detail.version.versionNumber));
  const key = `results/${jobId}/extraction.json`;
  localAws(["s3", "cp", "-", `s3://contractguard-local/${key}`, "--content-type", "application/json"], json);
  const queueUrl = localAws(["sqs", "get-queue-url", "--queue-name", "analysis-results", "--query", "QueueUrl", "--output", "text"]).trim();
  localAws(["sqs", "send-message", "--queue-url", queueUrl, "--message-body", JSON.stringify({ message_version: "1", event_type: "completed", job_id: jobId, result_key: key })]);
}

async function workspaceHeader(page: Page): Promise<Record<string, string>> {
  const workspace = await page.evaluate(() => window.localStorage.getItem("cg.workspace"));
  return workspace ? { "X-Workspace-Id": workspace } : {};
}

function localAws(args: string[], input?: string): string {
  return execFileSync(
    "docker",
    ["compose", "exec", "-T", "-e", "AWS_ACCESS_KEY_ID=test", "-e", "AWS_SECRET_ACCESS_KEY=test", "-e", "AWS_DEFAULT_REGION=ap-southeast-1", "localstack", "aws", "--endpoint-url", "http://localhost:4566", ...args],
    { cwd: repoRoot, input, encoding: "utf8" },
  );
}
