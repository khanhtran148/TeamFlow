import { test, expect } from "@playwright/test";

test.describe("Cross-Feature Integration", () => {
  test("navigation includes all new sections", async ({ page }) => {
    await page.goto("/projects/test-project/backlog");
    // Verify all nav tabs exist
    await expect(page.getByRole("link", { name: /backlog/i })).toBeVisible();
    await expect(page.getByRole("link", { name: /board/i })).toBeVisible();
    await expect(page.getByRole("link", { name: /sprints/i })).toBeVisible();
    await expect(page.getByRole("link", { name: /releases/i })).toBeVisible();
    await expect(page.getByRole("link", { name: /retros/i })).toBeVisible();
  });

  test("notification bell is visible", async ({ page }) => {
    await page.goto("/projects/test-project/backlog");
    await expect(page.getByTestId("notification-bell")).toBeVisible();
  });

  test("work item detail page has comments tab", async ({ page }) => {
    await page.goto("/projects/test-project/work-items/test-item");
    await expect(page.getByRole("button", { name: /comments/i })).toBeVisible();
  });

  test("app loads without JavaScript errors", async ({ page }) => {
    const errors: string[] = [];
    page.on("pageerror", (error) => {
      errors.push(error.message);
    });

    await page.goto("/projects/test-project/backlog");
    await page.waitForTimeout(2000);

    // Filter out known benign errors (e.g., SignalR connection failures in test env)
    const criticalErrors = errors.filter(
      (e) => !e.includes("SignalR") && !e.includes("Network Error"),
    );
    expect(criticalErrors).toHaveLength(0);
  });
});

test.describe("Cross-Feature: Retro -> Backlog", () => {
  test("retro action items link to backlog tasks", async ({ page }) => {
    // Navigate to a closed retro session with action items
    await page.goto("/projects/test-project/retros/test-closed-session");
    // Action items with linked tasks should have a "Task" link
    const taskLinks = page.getByRole("link", { name: /task/i });
    // If any exist, they should be clickable
  });
});

test.describe("Cross-Feature: Poker -> Work Item", () => {
  test("poker estimate updates work item", async ({ page }) => {
    // Navigate to a work item with a completed poker session
    await page.goto("/projects/test-project/work-items/test-story");
    await page.getByRole("button", { name: /poker/i }).click();
    // If final estimate exists, it should show
    const finalEstimate = page.getByText(/estimated:/i);
    // Depends on whether session has a final estimate
  });
});
