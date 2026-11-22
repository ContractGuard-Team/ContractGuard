import { expect, test } from "@playwright/test";

import { completeAnalysis, confirmContext, signUp, uploadSample } from "./helpers";

// CR-CTR-001 on the real stack: rename, run the analysis again after it finished, move to the trash,
// restore, then delete for good from the trash.
test("an owner renames a contract, trashes it, restores it and deletes it for good", async ({ page }) => {
  await signUp(page, "trash", "E2E Thùng rác");
  const contractId = await uploadSample(page);
  await confirmContext(page);
  await completeAnalysis(page, contractId);
  await expect(page.getByRole("tab", { name: "Tổng quan" })).toBeVisible({ timeout: 30_000 });

  // A finished contract can be run again: the job starts instead of failing on a missing file.
  await page.getByRole("button", { name: "Chạy lại phân tích" }).click();
  await expect(page.getByRole("heading", { name: "Đang phân tích hợp đồng" })).toBeVisible();
  await completeAnalysis(page, contractId);
  await expect(page.getByRole("button", { name: "Chạy lại phân tích" })).toBeVisible({ timeout: 30_000 });

  // Rename: owner only, shown in the title, the library and the log.
  await page.getByRole("button", { name: "Đổi tên" }).click();
  const rename = page.getByRole("dialog", { name: "Đổi tên hợp đồng" });
  await rename.getByLabel("Tên hợp đồng").fill("Hợp đồng dịch vụ đám mây E2E");
  await rename.getByRole("button", { name: "Lưu" }).click();
  await expect(rename).toBeHidden();
  await expect(page.getByRole("heading", { name: "Hợp đồng dịch vụ đám mây E2E" })).toBeVisible();
  await page.getByRole("tab", { name: "Nhật ký" }).click();
  await expect(page.getByRole("tabpanel").getByText("Đổi tên hợp đồng")).toBeVisible();

  // To the trash: gone from the library, listed in the trash with 30 days left.
  await page.getByRole("tab", { name: "Phiên bản" }).click();
  await page.getByRole("button", { name: "Chuyển vào thùng rác" }).click();
  await page.getByRole("dialog", { name: "Chuyển hợp đồng vào thùng rác?" }).getByRole("button", { name: "Chuyển vào thùng rác" }).click();
  await expect(page).toHaveURL(/\/contracts$/);
  await expect(page.getByText("Chưa có hợp đồng nào.", { exact: false })).toBeVisible();
  expect((await page.request.get(`/api/v1/contracts/${contractId}`)).status()).toBe(404);

  await page.getByRole("link", { name: "Thùng rác" }).click();
  await expect(page).toHaveURL(/\/contracts\/trash$/);
  const row = page.getByRole("listitem").filter({ hasText: "Hợp đồng dịch vụ đám mây E2E" });
  await expect(row).toContainText("Còn 30 ngày");
  await expect(row).toContainText("sample-contract.pdf");

  // Restore: back in the library with its result.
  await row.getByRole("button", { name: "Khôi phục" }).click();
  await expect(page.getByText("Đã khôi phục “Hợp đồng dịch vụ đám mây E2E” về thư viện.")).toBeVisible();
  await expect(page.getByText("Thùng rác trống.", { exact: false })).toBeVisible();
  await page.getByRole("link", { name: "Thư viện hợp đồng", exact: true }).click();
  await page.getByRole("link", { name: "Hợp đồng dịch vụ đám mây E2E" }).click();
  await expect(page.getByRole("tab", { name: "Tổng quan" })).toBeVisible();

  // Trash again, then delete for good after confirming.
  await page.getByRole("tab", { name: "Phiên bản" }).click();
  await page.getByRole("button", { name: "Chuyển vào thùng rác" }).click();
  await page.getByRole("dialog", { name: "Chuyển hợp đồng vào thùng rác?" }).getByRole("button", { name: "Chuyển vào thùng rác" }).click();
  await page.goto("/contracts/trash");
  await page.getByRole("listitem").filter({ hasText: "Hợp đồng dịch vụ đám mây E2E" }).getByRole("button", { name: "Xóa vĩnh viễn" }).click();
  await page.getByRole("dialog", { name: "Xóa vĩnh viễn hợp đồng?" }).getByRole("button", { name: "Xóa vĩnh viễn" }).click();
  await expect(page.getByText("Đã xóa vĩnh viễn “Hợp đồng dịch vụ đám mây E2E”.")).toBeVisible();
  await expect(page.getByText("Thùng rác trống.", { exact: false })).toBeVisible();
  expect((await page.request.post(`/api/v1/contracts/${contractId}/restore`)).status()).not.toBe(204);
});
