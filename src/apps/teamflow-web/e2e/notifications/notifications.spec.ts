import { test, expect } from "../fixtures";

const API_URL = process.env.API_URL ?? "http://localhost:5210/api/v1";

test.describe("Notification Bell", () => {
  let projectId: string;
  let token: string;

  test.beforeAll(async ({ request }) => {
    const email = `e2e-notif-${Date.now()}@teamflow.dev`;
    const regRes = await request.post(`${API_URL}/auth/register`, {
      data: { email, password: "Test@1234", name: "Notification E2E" },
    });
    const regBody = await regRes.json();
    token = regBody.accessToken;

    const orgRes = await request.post(`${API_URL}/organizations`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { name: `Notif Org ${Date.now()}` },
    });
    const org = await orgRes.json();

    const projRes = await request.post(`${API_URL}/projects`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { orgId: org.id, name: `Notif Project ${Date.now()}` },
    });
    const proj = await projRes.json();
    projectId = proj.id;
  });

  test("notification bell is visible in the top bar", async ({ page }) => {
    await page.goto(`/projects/${projectId}/backlog`);
    await expect(page.getByTestId("notification-bell")).toBeVisible({
      timeout: 10_000,
    });
  });

  test("clicking the bell opens the notification dropdown", async ({
    page,
  }) => {
    await page.goto(`/projects/${projectId}/backlog`);
    const bell = page.getByTestId("notification-bell");
    await expect(bell).toBeVisible({ timeout: 10_000 });

    await bell.click();

    // The dropdown should show the "Notifications" heading
    await expect(page.getByText("Notifications").last()).toBeVisible({
      timeout: 5_000,
    });
  });

  test("empty notification dropdown shows placeholder message", async ({
    page,
  }) => {
    await page.goto(`/projects/${projectId}/backlog`);
    const bell = page.getByTestId("notification-bell");
    await expect(bell).toBeVisible({ timeout: 10_000 });

    await bell.click();

    // For a fresh user, the dropdown should show the empty state
    await expect(page.getByText(/no new notifications/i)).toBeVisible({
      timeout: 5_000,
    });
  });

  test("clicking outside the dropdown closes it", async ({ page }) => {
    await page.goto(`/projects/${projectId}/backlog`);
    const bell = page.getByTestId("notification-bell");
    await expect(bell).toBeVisible({ timeout: 10_000 });

    await bell.click();
    await expect(page.getByText("Notifications").last()).toBeVisible({
      timeout: 5_000,
    });

    // Click on the overlay to close the dropdown
    await page.mouse.click(10, 10);

    // The "No new notifications" message should no longer be visible
    await expect(page.getByText(/no new notifications/i)).not.toBeVisible({
      timeout: 5_000,
    });
  });
});

test.describe("Notifications Page", () => {
  let projectId: string;
  let token: string;

  test.beforeAll(async ({ request }) => {
    const email = `e2e-notifpage-${Date.now()}@teamflow.dev`;
    const regRes = await request.post(`${API_URL}/auth/register`, {
      data: { email, password: "Test@1234", name: "NotifPage E2E" },
    });
    const regBody = await regRes.json();
    token = regBody.accessToken;

    const orgRes = await request.post(`${API_URL}/organizations`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { name: `NotifPage Org ${Date.now()}` },
    });
    const org = await orgRes.json();

    const projRes = await request.post(`${API_URL}/projects`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { orgId: org.id, name: `NotifPage Project ${Date.now()}` },
    });
    const proj = await projRes.json();
    projectId = proj.id;
  });

  test("notifications page loads with heading", async ({ page }) => {
    await page.goto(`/projects/${projectId}/notifications`);
    await expect(
      page.getByRole("heading", { name: /notifications/i }),
    ).toBeVisible({ timeout: 10_000 });
  });

  test("inbox and preferences tabs are visible", async ({ page }) => {
    await page.goto(`/projects/${projectId}/notifications`);
    await expect(page.getByRole("button", { name: /inbox/i })).toBeVisible({
      timeout: 10_000,
    });
    await expect(
      page.getByRole("button", { name: /preferences/i }),
    ).toBeVisible({ timeout: 10_000 });
  });

  test("can switch between inbox and preferences tabs", async ({ page }) => {
    await page.goto(`/projects/${projectId}/notifications`);
    const preferencesTab = page.getByRole("button", { name: /preferences/i });
    await expect(preferencesTab).toBeVisible({ timeout: 10_000 });

    await preferencesTab.click();

    // After clicking Preferences, the tab should appear active (has bg-blue-600)
    await expect(preferencesTab).toHaveClass(/bg-blue-600/);
  });
});
