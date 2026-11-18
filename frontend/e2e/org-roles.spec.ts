import { expect, test } from "@playwright/test";

import { lastEmail, linkIn, logIn, logOut, signUp } from "./helpers";

// FR-ORG-007, FR-ORG-008: an owner grants and takes back the Owner role; the members page shows the
// new role right away and after opening the page again.
test("an administrator grants and revokes the administrator role of a member", async ({ page }) => {
  const memberEmail = await signUp(page, "role-member", "E2E Vai trò");
  await logOut(page);
  const ownerEmail = await signUp(page, "role-owner", "E2E Chủ");

  await page.getByLabel("Không gian làm việc").selectOption({ label: "+ Tạo tổ chức mới" });
  await page.getByLabel("Tên tổ chức").fill("Công ty Vai trò");
  await page.getByRole("button", { name: "Tạo tổ chức" }).click();
  await page.getByLabel("Mời thành viên qua email").fill(memberEmail);
  await page.getByRole("button", { name: "Gửi lời mời" }).click();
  await expect(page.getByText(`Đã gửi lời mời tới ${memberEmail}`, { exact: false })).toBeVisible();
  await logOut(page);

  await logIn(page, memberEmail);
  await page.goto(linkIn(await lastEmail(page.request, memberEmail), "/invitations/"));
  await page.getByRole("button", { name: "Tham gia Công ty Vai trò" }).click();
  await expect(page.getByLabel("Không gian làm việc")).toHaveValue(/[0-9a-f-]{36}/);
  await logOut(page);

  await logIn(page, ownerEmail);
  await page.getByLabel("Không gian làm việc").selectOption({ label: "Công ty Vai trò" });
  await page.getByRole("link", { name: "Thành viên tổ chức" }).click();
  const row = page.getByRole("row").filter({ hasText: memberEmail });
  await expect(row.getByRole("cell", { name: "Thành viên", exact: true })).toBeVisible();

  // CR-GEN-002: every role change goes through a confirmation.
  await row.getByRole("button", { name: "Cấp quyền quản trị" }).click();
  await page.getByRole("dialog", { name: /^Cấp quyền quản trị/ }).getByRole("button", { name: "Cấp quyền quản trị" }).click();
  await expect(page.getByText("Đã cấp quyền quản trị.")).toBeVisible();
  await expect(row.getByRole("cell", { name: "Quản trị viên", exact: true })).toBeVisible();
  await page.reload();
  await expect(row.getByRole("cell", { name: "Quản trị viên", exact: true })).toBeVisible();

  await row.getByRole("button", { name: "Thu hồi quyền quản trị" }).click();
  await page.getByRole("dialog", { name: /^Thu hồi quyền quản trị/ }).getByRole("button", { name: "Thu hồi quyền quản trị" }).click();
  await expect(page.getByText("Đã thu hồi quyền quản trị.")).toBeVisible();
  await expect(row.getByRole("cell", { name: "Thành viên", exact: true })).toBeVisible();
  await page.reload();
  await expect(row.getByRole("cell", { name: "Thành viên", exact: true })).toBeVisible();

  // FR-ORG-009 behind the app's own confirmation: cancel keeps the member, confirm removes them.
  await row.getByRole("button", { name: "Xóa" }).click();
  const confirm = page.getByRole("dialog", { name: /khỏi tổ chức/ });
  await confirm.getByRole("button", { name: "Hủy" }).click();
  await expect(confirm).toBeHidden();
  await expect(row).toBeVisible();
  await row.getByRole("button", { name: "Xóa" }).click();
  await confirm.getByRole("button", { name: "Xóa thành viên" }).click();
  await expect(page.getByText("Đã xóa thành viên.")).toBeVisible();
  await expect(row).toHaveCount(0);
});
