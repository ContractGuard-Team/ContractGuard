import { expect, test } from "@playwright/test";

import { lastEmail, linkIn, logIn, logOut, signUp, uploadSample } from "./helpers";

// F-SHARE-001: the contract owner hands an organization contract to another member from the share
// dialog. The dialog fits its content, closes afterwards, and the old owner loses the owner actions.
test("an owner hands a contract to another member from the share dialog", async ({ page }) => {
  const memberEmail = await signUp(page, "tx-member", "E2E Người nhận hợp đồng có tên rất dài");
  await logOut(page);
  const ownerEmail = await signUp(page, "tx-owner", "E2E TX Owner");
  await page.getByLabel("Không gian làm việc").selectOption({ label: "+ Tạo tổ chức mới" });
  await page.getByLabel("Tên tổ chức").fill("Công ty TX");
  await page.getByRole("button", { name: "Tạo tổ chức" }).click();
  await page.getByLabel("Mời thành viên qua email").fill(memberEmail);
  await page.getByRole("button", { name: "Gửi lời mời" }).click();
  await expect(page.getByText("Đã gửi lời mời", { exact: false })).toBeVisible();
  const contractId = await uploadSample(page);
  await page.keyboard.press("Escape");
  await logOut(page);

  await logIn(page, memberEmail);
  await page.goto(linkIn(await lastEmail(page.request, memberEmail), "/invitations/"));
  await page.getByRole("button", { name: "Tham gia Công ty TX" }).click();
  await expect(page.getByLabel("Không gian làm việc")).toHaveValue(/[0-9a-f-]{36}/);
  await logOut(page);

  await logIn(page, ownerEmail);
  await page.goto(`/contracts/${contractId}`);
  await page.getByRole("dialog", { name: /ngữ cảnh/ }).waitFor();
  await page.keyboard.press("Escape");
  await page.getByRole("button", { name: "Chia sẻ" }).click();
  const share = page.getByRole("dialog", { name: /Chia sẻ/ });

  // Sharing with the whole organization asks first, in the app's own dialog; cancel shares nothing.
  await share.getByRole("button", { name: "Chia sẻ cho toàn tổ chức" }).click();
  const confirm = page.getByRole("dialog", { name: "Chia sẻ cho toàn tổ chức?" });
  await expect(confirm).toContainText("1 thành viên");
  await confirm.getByRole("button", { name: "Hủy" }).click();
  await expect(confirm).toBeHidden();
  await expect(share).toBeVisible();
  await expect(share.getByRole("button", { name: "Thu hồi" })).toHaveCount(0);

  // The email box suggests members without access; once shared, the person shows their role and a revoke button.
  await expect(share.locator(`datalist option[value="${memberEmail}"]`)).toHaveCount(1);
  await share.getByLabel("Chia sẻ theo email").fill(memberEmail);
  await share.getByRole("button", { name: "Chia sẻ", exact: true }).click();
  const memberRow = share.getByRole("listitem").filter({ hasText: memberEmail });
  await expect(memberRow.getByText("Được chia sẻ", { exact: true })).toBeVisible();
  await expect(memberRow.getByRole("button", { name: "Thu hồi" })).toBeVisible();
  await expect(share.locator(`datalist option[value="${memberEmail}"]`)).toHaveCount(0);
  await share.getByLabel("Chuyển người phụ trách hợp đồng cho").selectOption({ label: `E2E Người nhận hợp đồng có tên rất dài (${memberEmail})` });
  // A long member name must not push the dialog wider than the screen.
  expect(await share.evaluate((d) => d.scrollWidth <= d.clientWidth)).toBe(true);

  // Asks first: cancel keeps the owner, confirm hands it over and closes the share dialog.
  const confirmTransfer = page.getByRole("dialog", { name: /Giao hợp đồng cho .* phụ trách/ });
  await share.getByRole("button", { name: "Chuyển", exact: true }).click();
  await confirmTransfer.getByRole("button", { name: "Hủy" }).click();
  await expect(confirmTransfer).toBeHidden();
  await expect(share.getByRole("listitem").first()).toContainText(ownerEmail);
  await share.getByRole("button", { name: "Chuyển", exact: true }).click();
  await confirmTransfer.getByRole("button", { name: "Chuyển quyền" }).click();
  await expect(share).toBeHidden();
  // Still an owner of the organization, so still allowed to manage it (BR-SHARE-002); the new owner is listed.
  await page.getByRole("button", { name: "Chia sẻ" }).click();
  await expect(share.getByRole("listitem").first()).toContainText(memberEmail);
  await expect(share.getByRole("listitem").first()).toContainText("Người phụ trách");
});
