import { expect, test } from "@playwright/test";

import { signUp } from "./helpers";

// "/" is public; someone with a session is sent on to the library instead.
test("the landing page is public and forwards a signed-in user to the library", async ({ page }) => {
  await page.goto("/");
  await expect(page.getByRole("heading", { level: 1 })).toContainText("trước khi ký");

  await signUp(page, "landing", "E2E Landing");
  await expect(page).toHaveURL(/\/contracts$/);

  await page.goto("/");
  await expect(page).toHaveURL(/\/contracts$/);
});
