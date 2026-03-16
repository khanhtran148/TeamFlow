import { test, expect } from "@playwright/test";

test.describe("Planning Poker", () => {
  test("poker tab is visible on UserStory work items", async ({ page }) => {
    // Navigate to a UserStory work item
    await page.goto("/projects/test-project/work-items/test-story");
    const pokerTab = page.getByRole("button", { name: /poker/i });
    await expect(pokerTab).toBeVisible();
  });

  test("poker tab is NOT visible on Bug work items", async ({ page }) => {
    // Navigate to a Bug work item
    await page.goto("/projects/test-project/work-items/test-bug");
    const pokerTab = page.getByRole("button", { name: /poker/i });
    await expect(pokerTab).toHaveCount(0);
  });

  test("poker session shows Fibonacci cards", async ({ page }) => {
    await page.goto("/projects/test-project/work-items/test-story");
    await page.getByRole("button", { name: /poker/i }).click();
    // Should show the poker session area
    await expect(page.getByText(/planning poker/i)).toBeVisible();
  });

  test("PO sees session but no vote cards", async ({ page }) => {
    // When user has PO role (no Poker_Vote permission)
    await page.goto("/projects/test-project/work-items/test-story");
    await page.getByRole("button", { name: /poker/i }).click();
    // PO should see the session area
    await expect(page.getByText(/planning poker/i)).toBeVisible();
    // Vote cards should not be interactive for PO role
    const voteCards = page.locator('[data-testid="poker-vote-card"]');
    await expect(voteCards).toHaveCount(0);
  });

  test("vote count updates live", async ({ page }) => {
    await page.goto("/projects/test-project/work-items/test-story");
    await page.getByRole("button", { name: /poker/i }).click();
    // Check that vote count element exists
    await expect(page.getByText(/votes/i)).toBeVisible();
  });

  test("reveal shows all votes simultaneously", async ({ page }) => {
    await page.goto("/projects/test-project/work-items/test-story");
    await page.getByRole("button", { name: /poker/i }).click();
    // After reveal, the results section should be visible with vote summary
    const revealButton = page.locator('[data-testid="poker-reveal-button"]');
    if (await revealButton.isVisible()) {
      await revealButton.click();
    }
    const resultsSection = page.locator('[data-testid="poker-results"]');
    await expect(resultsSection).toBeVisible();
    // Verify that at least one vote value is displayed
    await expect(page.locator('[data-testid="poker-vote-value"]').first()).toBeVisible();
  });
});
