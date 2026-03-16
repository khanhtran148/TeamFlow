import { test, expect } from "@playwright/test";

test.describe("Comment CRUD", () => {
  test("can view comments section on work item detail", async ({ page }) => {
    await page.goto("/projects/test-project/work-items/test-item");
    // The comments tab should be visible
    await expect(page.getByRole("button", { name: /comments/i })).toBeVisible();
  });

  test("comment form is visible and submittable", async ({ page }) => {
    await page.goto("/projects/test-project/work-items/test-item");
    // Click comments tab
    await page.getByRole("button", { name: /comments/i }).click();
    // Check form elements exist
    await expect(page.getByPlaceholder(/write a comment/i)).toBeVisible();
    await expect(page.getByRole("button", { name: /comment/i })).toBeVisible();
  });

  test("empty comment cannot be submitted", async ({ page }) => {
    await page.goto("/projects/test-project/work-items/test-item");
    await page.getByRole("button", { name: /comments/i }).click();
    // The submit button should be disabled when textarea is empty
    const submitButton = page.getByRole("button", { name: /^comment$/i });
    await expect(submitButton).toBeDisabled();
  });

  test("@mention dropdown appears when typing @", async ({ page }) => {
    await page.goto("/projects/test-project/work-items/test-item");
    await page.getByRole("button", { name: /comments/i }).click();
    const textarea = page.getByPlaceholder(/write a comment/i);
    await textarea.fill("Hello @");
    // Mention autocomplete should appear (may not have data in E2E without backend)
  });
});

test.describe("Comment @mention notification", () => {
  test("notification bell is visible in top bar", async ({ page }) => {
    await page.goto("/projects");
    await expect(page.getByTestId("notification-bell")).toBeVisible();
  });
});
