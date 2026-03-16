import { test, expect } from "../fixtures";

const API_URL = process.env.API_URL ?? "http://localhost:5210/api/v1";

test.describe("Search Page", () => {
  let projectId: string;
  let token: string;

  test.beforeAll(async ({ request }) => {
    const email = `e2e-search-${Date.now()}@teamflow.dev`;
    const regRes = await request.post(`${API_URL}/auth/register`, {
      data: { email, password: "Test@1234", name: "Search E2E" },
    });
    const regBody = await regRes.json();
    token = regBody.accessToken;

    const orgRes = await request.post(`${API_URL}/organizations`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { name: `Search Org ${Date.now()}` },
    });
    const org = await orgRes.json();

    const projRes = await request.post(`${API_URL}/projects`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { orgId: org.id, name: `Search Project ${Date.now()}` },
    });
    const proj = await projRes.json();
    projectId = proj.id;
  });

  test("search page loads with search input", async ({ page }) => {
    await page.goto(`/projects/${projectId}/search`);
    await expect(
      page.getByPlaceholder(/search work items/i),
    ).toBeVisible({ timeout: 10_000 });
  });

  test("can type in search input", async ({ page }) => {
    await page.goto(`/projects/${projectId}/search`);
    const searchInput = page.getByPlaceholder(/search work items/i);
    await expect(searchInput).toBeVisible({ timeout: 10_000 });

    await searchInput.fill("login bug");
    await expect(searchInput).toHaveValue("login bug");
  });

  test("filter panel renders status chips", async ({ page }) => {
    await page.goto(`/projects/${projectId}/search`);
    // Status filter chips should be present
    await expect(page.getByRole("button", { name: "ToDo" })).toBeVisible({
      timeout: 10_000,
    });
    await expect(
      page.getByRole("button", { name: "InProgress" }),
    ).toBeVisible({ timeout: 10_000 });
    await expect(page.getByRole("button", { name: "Done" })).toBeVisible({
      timeout: 10_000,
    });
  });

  test("filter chips are clickable and toggle active state", async ({
    page,
  }) => {
    await page.goto(`/projects/${projectId}/search`);
    const todoChip = page.getByRole("button", { name: "ToDo" });
    await expect(todoChip).toBeVisible({ timeout: 10_000 });

    // Initially should have the inactive style (bg-white)
    await expect(todoChip).toHaveClass(/bg-white/);

    // Click to activate
    await todoChip.click();
    await expect(todoChip).toHaveClass(/bg-blue-600/);

    // Click again to deactivate
    await todoChip.click();
    await expect(todoChip).toHaveClass(/bg-white/);
  });

  test("priority filter chips are visible", async ({ page }) => {
    await page.goto(`/projects/${projectId}/search`);
    await expect(
      page.getByRole("button", { name: "Critical" }),
    ).toBeVisible({ timeout: 10_000 });
    await expect(page.getByRole("button", { name: "High" })).toBeVisible({
      timeout: 10_000,
    });
    await expect(page.getByRole("button", { name: "Medium" })).toBeVisible({
      timeout: 10_000,
    });
    await expect(page.getByRole("button", { name: "Low" })).toBeVisible({
      timeout: 10_000,
    });
  });

  test("type filter chips are visible", async ({ page }) => {
    await page.goto(`/projects/${projectId}/search`);
    await expect(page.getByRole("button", { name: "Epic" })).toBeVisible({
      timeout: 10_000,
    });
    await expect(page.getByRole("button", { name: "Task" })).toBeVisible({
      timeout: 10_000,
    });
    await expect(page.getByRole("button", { name: "Bug" })).toBeVisible({
      timeout: 10_000,
    });
  });

  test("clear all button resets filters", async ({ page }) => {
    await page.goto(`/projects/${projectId}/search`);
    const todoChip = page.getByRole("button", { name: "ToDo" });
    await expect(todoChip).toBeVisible({ timeout: 10_000 });

    // Activate a filter
    await todoChip.click();
    await expect(todoChip).toHaveClass(/bg-blue-600/);

    // Click "Clear all"
    await page.getByRole("button", { name: /clear all/i }).click();

    // Filter should be deactivated
    await expect(todoChip).toHaveClass(/bg-white/);
  });
});
