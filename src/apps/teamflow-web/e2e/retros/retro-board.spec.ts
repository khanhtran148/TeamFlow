import { test, expect } from "@playwright/test";

test.describe("Retro Board", () => {
  test("retros page is accessible from project nav", async ({ page }) => {
    await page.goto("/projects/test-project/backlog");
    const retroLink = page.getByRole("link", { name: /retros/i });
    await expect(retroLink).toBeVisible();
  });

  test("retros list page renders", async ({ page }) => {
    await page.goto("/projects/test-project/retros");
    await expect(page.getByText(/retrospectives/i)).toBeVisible();
  });

  test("new retro button visible for facilitators", async ({ page }) => {
    await page.goto("/projects/test-project/retros");
    // The "New Retro" button should render for users with facilitator permissions
    const newButton = page.getByRole("button", { name: /new retro/i });
    await expect(newButton).toBeVisible();
  });

  test("retro board has three columns", async ({ page }) => {
    // Navigate to a retro session detail page
    await page.goto("/projects/test-project/retros/test-session");
    // Should see the three columns
    await expect(page.getByText(/went well/i)).toBeVisible();
    await expect(page.getByText(/needs improvement/i)).toBeVisible();
    await expect(page.getByText(/action items/i)).toBeVisible();
  });
});

test.describe("Retro Voting", () => {
  test("vote buttons appear during voting phase", async ({ page }) => {
    await page.goto("/projects/test-project/retros/test-voting-session");
    // Vote buttons should be visible during voting phase
    const voteButtons = page.locator('[data-testid="retro-vote-button"]');
    await expect(voteButtons.first()).toBeVisible();
    // Each card should have a vote button
    const cardCount = await page.locator('[data-testid="retro-card"]').count();
    await expect(voteButtons).toHaveCount(cardCount);
  });
});

test.describe("Retro Summary", () => {
  test("summary visible after session close", async ({ page }) => {
    await page.goto("/projects/test-project/retros/test-closed-session");
    // If session is closed, summary should be visible
    await expect(page.getByText(/session summary/i)).toBeVisible();
  });
});
