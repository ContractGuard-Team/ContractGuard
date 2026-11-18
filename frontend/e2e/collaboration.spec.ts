import { expect, test } from "@playwright/test";

import { completeAnalysis, confirmContext, lastEmail, linkIn, logIn, logOut, signUp, uploadSample } from "./helpers";

// Phase 5 on the real stack: an organization, an invitation by email, sharing and revoking a contract,
// the library from the member's side, and the owner's activity log.
test("an owner invites a colleague, shares a contract, then revokes it", async ({ page }) => {
  const memberEmail = await signUp(page, "member", "E2E Thành viên");
  await logOut(page);
  const ownerEmail = await signUp(page, "owner", "E2E Owner");

  // FR-ORG-001 and FR-ORG-014: create, and the app switches into it.
  await page.getByLabel("Không gian làm việc").selectOption({ label: "+ Tạo tổ chức mới" });
  await page.getByLabel("Tên tổ chức").fill("Công ty E2E");
  await page.getByRole("button", { name: "Tạo tổ chức" }).click();
  await expect(page.getByRole("heading", { name: "Công ty E2E" })).toBeVisible();

  // FR-ORG-003
  await page.getByLabel("Mời thành viên qua email").fill(memberEmail);
  await page.getByRole("button", { name: "Gửi lời mời" }).click();
  await expect(page.getByText(`Đã gửi lời mời tới ${memberEmail}`, { exact: false })).toBeVisible();
  await expect(page.getByRole("listitem").filter({ hasText: memberEmail })).toBeVisible();

  // An organization contract, analyzed.
  const contractId = await uploadSample(page);
  await confirmContext(page);
  await completeAnalysis(page, contractId);
  await expect(page.getByRole("tab", { name: "Tổng quan" })).toBeVisible({ timeout: 30_000 });
  await logOut(page);

  // FR-ORG-004: the member opens the link, logs in and joins.
  const invitation = linkIn(await lastEmail(page.request, memberEmail), "/invitations/");
  await page.goto(invitation);
  await expect(page.getByText(/tham gia tổ chức Công ty E2E/)).toBeVisible();
  await page.getByRole("link", { name: "Đăng nhập" }).click();
  await logInHere(page, memberEmail);
  await expect(page).toHaveURL(new RegExp(invitation.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")));
  await page.getByRole("button", { name: "Tham gia Công ty E2E" }).click();
  await expect(page.getByLabel("Không gian làm việc")).toHaveValue(/[0-9a-f-]{36}/);

  // BR-SHARE-001: not shared yet, so nothing in the organization's library for the member.
  await expect(page.getByText("Chưa có hợp đồng nào.", { exact: false })).toBeVisible();
  await logOut(page);

  // F-SHARE-001 by the owner.
  await logIn(page, ownerEmail);
  await page.goto(`/contracts/${contractId}`);
  await page.getByRole("button", { name: "Chia sẻ" }).click();
  const share = page.getByRole("dialog", { name: /Chia sẻ/ });
  await share.getByLabel("Chia sẻ theo email").fill(memberEmail);
  await share.getByRole("button", { name: "Chia sẻ", exact: true }).click();
  await expect(share.getByText(memberEmail, { exact: true })).toBeVisible();
  await share.getByRole("button", { name: "Đóng" }).click();
  await logOut(page);

  // The member sees it, marked as shared, and works on it as a Member: no share button, no log tab.
  await logIn(page, memberEmail);
  await page.getByLabel("Không gian làm việc").selectOption({ label: "Công ty E2E" });
  await expect(page.getByText("Được chia sẻ với bạn", { exact: false })).toBeVisible();
  await page.getByRole("link", { name: "sample-contract" }).click();
  await expect(page.getByRole("meter", { name: "Điểm rủi ro tổng" })).toBeVisible();
  await expect(page.getByRole("button", { name: "Chia sẻ" })).toHaveCount(0);
  await expect(page.getByRole("tab", { name: "Nhật ký" })).toHaveCount(0);
  await logOut(page);

  // FR-SHARE-007 and FR-AUD-002 by the owner.
  await logIn(page, ownerEmail);
  await page.goto(`/contracts/${contractId}`);
  await page.getByRole("button", { name: "Chia sẻ" }).click();
  await page.getByRole("dialog", { name: /Chia sẻ/ }).getByRole("button", { name: "Thu hồi" }).click();
  await page.getByRole("dialog", { name: /^Thu hồi quyền của/ }).getByRole("button", { name: "Thu hồi" }).click();
  await expect(page.getByText("Đã thu hồi quyền.")).toBeVisible();
  await page.getByRole("dialog", { name: /Chia sẻ/ }).getByRole("button", { name: "Đóng" }).click();
  await page.getByRole("tab", { name: "Nhật ký" }).click();
  const log = page.getByRole("region", { name: "Nhật ký hoạt động" });
  await expect(log.getByRole("cell", { name: /^Chia sẻ/ })).toBeVisible();
  await expect(log.getByText("Thu hồi chia sẻ")).toBeVisible();
  await expect(log.getByText("Xem hợp đồng").first()).toBeVisible();
  await logOut(page);

  // Revoked: the member gets the not-found page on the next request.
  await logIn(page, memberEmail);
  await page.goto(`/contracts/${contractId}`);
  await expect(page.getByText(/không tìm thấy/i).first()).toBeVisible();
});

// The login page of an invitation link carries ?next= back to the invitation.
async function logInHere(page: import("@playwright/test").Page, email: string) {
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Mật khẩu").fill("Mat-khau-e2e-1");
  await page.getByRole("button", { name: "Đăng nhập" }).click();
}
