import { test, expect } from "@playwright/test";

test.describe("Smoke Tests", () => {
  test("app loads and redirects unauthenticated users", async ({ page }) => {
    await page.goto("/");
    // After auth is implemented, unauthenticated users should be redirected to /login
    // For now, just verify the page loads without errors
    await expect(page).toHaveTitle(/TeamFlow/i);
  });
});
