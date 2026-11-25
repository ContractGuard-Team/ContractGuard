import { expect, test } from "@playwright/test";

import { completeAnalysis, confirmContext, sampleContract, signUp, uploadSample } from "./helpers";

// The demo flow (SC-GEN-005) on the real stack, with the worker played by the test: upload, context,
// analysis result, overview, clauses with margin notes, checklist, metadata, approval, chat, report,
// new version, delete.
test("a contract goes from upload to report, chat, new version and deletion", async ({ page }) => {
  await signUp(page, "flow", "E2E Người dùng");
  const contractId = await uploadSample(page);

  await confirmContext(page);
  await expect(page.getByRole("heading", { name: "Đang phân tích hợp đồng" })).toBeVisible();
  await completeAnalysis(page, contractId);

  // F-VIEW-001: the overview appears when the job completes (the page polls the job).
  await expect(page.getByRole("tab", { name: "Tổng quan" })).toBeVisible({ timeout: 30_000 });
  await expect(page.getByRole("meter", { name: "Điểm rủi ro tổng" })).toHaveAttribute("aria-valuenow", "72");
  await expect(page.getByText("Có 1 rủi ro Nghiêm trọng và 2 rủi ro Cao cần xem xét.")).toBeVisible();
  await expect(page.getByText("N/A", { exact: true })).toBeVisible();

  // FR-VIEW-006, FR-VIEW-007: filter, open a finding, record how it is handled (F-VIEW-003).
  const findings = page.getByRole("region", { name: /Rủi ro phát hiện/ });
  await page.getByLabel("Lọc theo mức").selectOption("critical");
  await expect(findings.getByRole("listitem")).toHaveCount(1);
  await findings.getByRole("button", { name: /Sự cố bảo mật bị loại trừ/ }).click();
  await expect(findings.getByText("In no event shall Provider be liable", { exact: false })).toBeVisible();
  await findings.getByLabel("Trạng thái xử lý", { exact: true }).selectOption("needs_negotiation");
  await findings.getByLabel("Ghi chú").fill("Gửi đề nghị sửa cho đối tác");
  await findings.getByRole("button", { name: "Lưu" }).click();
  await expect(findings.getByRole("button", { name: /Sự cố bảo mật bị loại trừ.*Cần đàm phán/ })).toBeVisible();

  // FR-VIEW-011: from the finding to its clause in the document tab.
  await findings.getByRole("button", { name: "Xem trong văn bản" }).click();
  await expect(page).toHaveURL(/view=document/);
  const clause = page.getByRole("article", { name: "6. Limitation of Liability" });
  await expect(clause).toBeInViewport();
  await expect(page.getByRole("button", { name: /Nghiêm trọng: Sự cố bảo mật bị loại trừ/ }).first()).toBeVisible();

  // FR-VIEW-012
  await page.getByRole("tab", { name: "Checklist" }).click();
  await expect(page.getByRole("row", { name: /CY-08/ })).toContainText("PARTIAL");

  // FR-CTR-007 in place, and FR-CTR-012 from the header.
  await page.getByRole("tab", { name: "Tổng quan" }).click();
  await page.getByRole("button", { name: "Sửa" }).click();
  await page.getByLabel("Luật áp dụng").fill("Việt Nam");
  await page.getByRole("button", { name: "Lưu thông tin" }).click();
  await expect(page.getByText("Đã được sửa thủ công", { exact: false })).toBeVisible();
  await page.getByLabel("Trạng thái duyệt").selectOption({ label: "→ Đang đàm phán" });
  await expect(page.getByText("Đang đàm phán ·", { exact: false })).toBeVisible();

  // F-CHAT-001 through the stand-in model (CHAT_RESPONDER=fake): stream, store, cite, jump.
  await page.getByLabel("Câu hỏi về hợp đồng").fill("Provider có được retain Customer Data sau termination không?");
  await page.getByRole("button", { name: "Gửi" }).click();
  const log = page.getByRole("log", { name: "Hội thoại với trợ lý AI" });
  await expect(log.getByText("[Giả lập, chưa có AI]", { exact: false })).toBeVisible();
  await log.getByRole("button", { name: "7. Term and Termination" }).click();
  await expect(page.getByRole("article", { name: "7. Term and Termination" })).toBeInViewport();

  // F-EXP-001, F-EXP-002: both formats download with the contract's name.
  for (const format of ["PDF", "DOCX"]) {
    await page.getByRole("button", { name: "Xuất báo cáo" }).click();
    const download = page.waitForEvent("download");
    await page.getByRole("button", { name: format, exact: true }).click();
    expect((await download).suggestedFilename()).toBe(`sample-contract-V1-report.${format.toLowerCase()}`);
  }

  // F-CTR-005: V2 starts without result, its context form is prefilled from V1 (FR-CTR-015).
  await page.getByRole("tab", { name: "Phiên bản" }).click();
  await page.getByRole("region", { name: "Lịch sử phiên bản" }).locator('input[type="file"]').setInputFiles(sampleContract);
  const context = page.getByRole("dialog", { name: "Khai báo ngữ cảnh hợp đồng" });
  await expect(context).toBeVisible();
  await expect(context.getByRole("textbox", { name: "Tên đối tác (không bắt buộc)" })).toHaveValue("Example Cloud Pte. Ltd.");
  await context.getByRole("button", { name: "Hủy" }).click();
  await page.getByRole("tab", { name: "Phiên bản" }).click();
  await expect(page.getByRole("region", { name: "Lịch sử phiên bản" }).getByRole("listitem")).toHaveCount(2);

  // FR-CTR-016, 017: V1 still opens with its own result.
  await page.getByRole("link", { name: "Mở phiên bản này" }).click();
  await expect(page.getByText("Bạn đang xem phiên bản cũ V1.", { exact: false })).toBeVisible();
  await expect(page.getByRole("meter", { name: "Điểm rủi ro tổng" })).toBeVisible();

  // F-CTR-006 with CR-CTR-001: to the trash, then the contract is gone from the library.
  await page.goto(`/contracts/${contractId}?view=versions`);
  await context.getByRole("button", { name: "Hủy" }).click(); // V2 still asks for its context on arrival.
  await page.getByRole("button", { name: "Chuyển vào thùng rác" }).click();
  await page.getByRole("dialog", { name: "Chuyển hợp đồng vào thùng rác?" }).getByRole("button", { name: "Chuyển vào thùng rác" }).click();
  await expect(page).toHaveURL(/\/contracts$/);
  await expect(page.getByText("Chưa có hợp đồng nào.", { exact: false })).toBeVisible();
  const response = await page.request.get(`/api/v1/contracts/${contractId}`);
  expect(response.status()).toBe(404);
});

test("the forgot password page counts down before another email (FR-AUTH-015)", async ({ page }) => {
  await page.goto("/forgot-password");
  await page.getByLabel("Email").fill("nobody-e2e@example.com");
  await page.getByRole("button", { name: "Gửi liên kết" }).click();
  await expect(page.getByRole("timer")).toContainText(/Có thể gửi lại sau 2:5\d|3:00/);

  // Kept across a reload.
  await page.reload();
  await expect(page.getByRole("button", { name: "Gửi lại liên kết" })).toBeDisabled();
  await expect(page.getByRole("timer")).toBeVisible();
});
