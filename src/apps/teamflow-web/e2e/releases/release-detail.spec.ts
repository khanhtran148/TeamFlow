import { test, expect } from "@playwright/test";

test.describe("Release Detail", () => {
  test("release detail page loads with progress bar", async ({ page }) => {
    await page.goto("/projects/test-project/releases/test-release");
    // The release detail page should show the release name and progress
    await expect(page.getByText(/progress/i)).toBeVisible();
  });

  test("release notes editor is visible", async ({ page }) => {
    await page.goto("/projects/test-project/releases/test-release");
    // Release notes section should be present
    await expect(page.getByText(/release notes/i)).toBeVisible();
  });

  test("ship button triggers confirm dialog for open items", async ({ page }) => {
    await page.goto("/projects/test-project/releases/test-release");
    // If there is a ship button
    const shipButton = page.getByRole("button", { name: /ship/i });
    if (await shipButton.isVisible()) {
      await shipButton.click();
      // Should show confirm dialog if open items exist
      await expect(page.getByText(/ship with open items/i)).toBeVisible();
    }
  });

  test("release notes locked after ship", async ({ page }) => {
    // Navigate to a shipped release
    await page.goto("/projects/test-project/releases/test-shipped-release");
    // Notes editor should show "Locked" badge
    await expect(page.getByText(/locked/i)).toBeVisible();
  });
});
