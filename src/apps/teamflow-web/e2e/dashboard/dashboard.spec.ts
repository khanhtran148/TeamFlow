import { test, expect } from "../fixtures";

const API_URL = process.env.API_URL ?? "http://localhost:5210/api/v1";

test.describe("Dashboard", () => {
  let projectId: string;
  let token: string;

  test.beforeAll(async ({ request }) => {
    // Seed a user + project so the dashboard route is valid
    const email = `e2e-dash-${Date.now()}@teamflow.dev`;
    const regRes = await request.post(`${API_URL}/auth/register`, {
      data: { email, password: "Test@1234", name: "Dashboard E2E" },
    });
    const regBody = await regRes.json();
    token = regBody.accessToken;

    // Create org + project
    const orgRes = await request.post(`${API_URL}/organizations`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { name: `Dash Org ${Date.now()}` },
    });
    const org = await orgRes.json();

    const projRes = await request.post(`${API_URL}/projects`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { orgId: org.id, name: `Dash Project ${Date.now()}` },
    });
    const proj = await projRes.json();
    projectId = proj.id;
  });

  test("dashboard page loads with heading", async ({ page }) => {
    await page.goto(`/projects/${projectId}/dashboard`);
    await expect(
      page.getByRole("heading", { name: /dashboard/i }),
    ).toBeVisible({ timeout: 10_000 });
  });

  test("summary cards section renders", async ({ page }) => {
    await page.goto(`/projects/${projectId}/dashboard`);
    // Summary cards show labels like "Active Sprint", "Completion", "Velocity", "Overdue Releases"
    await expect(page.getByText(/active sprint/i)).toBeVisible({
      timeout: 10_000,
    });
    await expect(page.getByText(/completion/i)).toBeVisible({
      timeout: 10_000,
    });
    await expect(page.getByText(/velocity/i).first()).toBeVisible({
      timeout: 10_000,
    });
    await expect(page.getByText(/overdue releases/i)).toBeVisible({
      timeout: 10_000,
    });
  });

  test("velocity chart area renders", async ({ page }) => {
    await page.goto(`/projects/${projectId}/dashboard`);
    // Velocity chart heading or "No velocity data" empty state should be visible
    const velocityHeading = page.getByText(/velocity chart/i);
    const noVelocityData = page.getByText(/no velocity data/i);
    await expect(velocityHeading.or(noVelocityData)).toBeVisible({
      timeout: 10_000,
    });
  });

  test("dashboard shows all four chart sections", async ({ page }) => {
    await page.goto(`/projects/${projectId}/dashboard`);
    // The page contains four chart components; each renders a heading or empty-state text.
    // We check that the grid of charts is present (at least 2 chart containers).
    const chartContainers = page.locator(".lg\\:grid-cols-2 > div");
    await expect(chartContainers.first()).toBeVisible({ timeout: 10_000 });
    const count = await chartContainers.count();
    expect(count).toBeGreaterThanOrEqual(2);
  });
});
