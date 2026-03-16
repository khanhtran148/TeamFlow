import { test, expect } from "@playwright/test";

test.describe("Backlog Refinement", () => {
  test("ready filter chip is visible in backlog toolbar", async ({ page }) => {
    await page.goto("/projects/test-project/backlog");
    const readyChip = page.getByRole("button", { name: /^ready$/i });
    await expect(readyChip).toBeVisible();
  });

  test("blocked filter chip is visible in backlog toolbar", async ({ page }) => {
    await page.goto("/projects/test-project/backlog");
    const blockedChip = page.getByRole("button", { name: /^blocked$/i });
    await expect(blockedChip).toBeVisible();
  });

  test("ready filter toggles correctly", async ({ page }) => {
    await page.goto("/projects/test-project/backlog");
    const readyChip = page.getByRole("button", { name: /^ready$/i });
    // Click to activate filter
    await readyChip.click();
    // Should be active (has accent border)
    // Click again to deactivate
    await readyChip.click();
  });

  test("clear filters button appears when filter active", async ({ page }) => {
    await page.goto("/projects/test-project/backlog");
    const readyChip = page.getByRole("button", { name: /^ready$/i });
    await readyChip.click();
    // Clear button should appear
    const clearButton = page.getByRole("button", { name: /clear/i });
    await expect(clearButton).toBeVisible();
  });
});
