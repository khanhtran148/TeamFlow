import { test, expect } from "../fixtures";

const API_URL = process.env.API_URL ?? "http://localhost:5210/api/v1";

/**
 * Visual regression baselines for Sprint pages.
 *
 * Each screenshot is captured in both light and dark mode using
 * the theme-toggle data-testid. Baselines are stored in
 * e2e/visual/snapshots/ and compared on subsequent runs.
 */
test.describe("Sprint Visual Regression", () => {
  let token: string;
  let projectId: string;

  test.beforeAll(async ({ request }) => {
    const email = `e2e-${Date.now()}-visual@teamflow.dev`;
    const password = "Test@1234";
    const response = await request.post(`${API_URL}/auth/register`, {
      data: { email, password, name: "E2E Visual" },
    });
    expect(response.status()).toBe(201);
    const body = await response.json();
    token = body.accessToken;

    // Create org + project
    const orgRes = await request.get(`${API_URL}/organizations`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    let orgId: string;
    if (orgRes.ok()) {
      const orgs = await orgRes.json();
      const items = Array.isArray(orgs) ? orgs : orgs.items ?? [];
      if (items.length > 0) {
        orgId = items[0].id;
      } else {
        const createOrgRes = await request.post(`${API_URL}/organizations`, {
          headers: { Authorization: `Bearer ${token}` },
          data: { name: `E2E Visual Org ${Date.now()}` },
        });
        orgId = (await createOrgRes.json()).id;
      }
    } else {
      const createOrgRes = await request.post(`${API_URL}/organizations`, {
        headers: { Authorization: `Bearer ${token}` },
        data: { name: `E2E Visual Org ${Date.now()}` },
      });
      orgId = (await createOrgRes.json()).id;
    }

    const projRes = await request.post(`${API_URL}/projects`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { orgId, name: "Visual Regression E2E" },
    });
    const project = await projRes.json();
    projectId = project.id;
  });

  test.afterAll(async ({ request }) => {
    if (token && projectId) {
      try {
        await request.delete(`${API_URL}/projects/${projectId}`, {
          headers: { Authorization: `Bearer ${token}` },
        });
      } catch {
        // Best-effort cleanup
      }
    }
  });

  /**
   * Helper: toggle theme using the data-testid="theme-toggle" button.
   * Returns the current theme class for verification.
   */
  async function toggleTheme(page: import("@playwright/test").Page): Promise<void> {
    const themeToggle = page.getByTestId("theme-toggle");
    if (await themeToggle.isVisible()) {
      await themeToggle.click();
      // Allow theme transition to settle
      await page.waitForTimeout(500);
    }
  }

  test("sprint list page - empty state", async ({ page, sprintHelpers }) => {
    await sprintHelpers.authenticatePage(page, { accessToken: token, refreshToken: "" });
    await page.goto(`/projects/${projectId}/sprints`);

    await expect(page.getByRole("heading", { name: "Sprints" })).toBeVisible({
      timeout: 10_000,
    });
    // Wait for empty state to render
    await expect(page.getByText(/no sprints yet/i)).toBeVisible();

    // Light mode screenshot
    await expect(page).toHaveScreenshot("sprint-list-empty-light.png", {
      maxDiffPixelRatio: 0.05,
    });

    // Dark mode screenshot
    await toggleTheme(page);
    await expect(page).toHaveScreenshot("sprint-list-empty-dark.png", {
      maxDiffPixelRatio: 0.05,
    });
  });

  test("sprint list page - with sprints", async ({ page, sprintHelpers }) => {
    // Create several sprints in different states
    const sprint1 = await sprintHelpers.createSprint(token, {
      projectId,
      name: "Planning Sprint VR",
      goal: "Visual regression baseline",
      startDate: new Date().toISOString().split("T")[0],
      endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
        .toISOString()
        .split("T")[0],
    });

    const sprint2 = await sprintHelpers.createSprint(token, {
      projectId,
      name: "Active Sprint VR",
      startDate: new Date().toISOString().split("T")[0],
      endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
        .toISOString()
        .split("T")[0],
    });

    const item = await sprintHelpers.createWorkItem(token, {
      projectId,
      title: "VR Item for Active",
      estimationValue: 5,
    });
    await sprintHelpers.addItemToSprint(token, sprint2.id, item.id);
    await sprintHelpers.startSprint(token, sprint2.id);

    await sprintHelpers.authenticatePage(page, { accessToken: token, refreshToken: "" });
    await page.goto(`/projects/${projectId}/sprints`);

    // Wait for sprint cards to appear
    await expect(page.getByTestId(`sprint-card-${sprint1.id}`)).toBeVisible({
      timeout: 10_000,
    });

    // Light mode
    await expect(page).toHaveScreenshot("sprint-list-with-sprints-light.png", {
      maxDiffPixelRatio: 0.05,
    });

    // Dark mode
    await toggleTheme(page);
    await expect(page).toHaveScreenshot("sprint-list-with-sprints-dark.png", {
      maxDiffPixelRatio: 0.05,
    });
  });

  test("sprint detail page - Planning state", async ({ page, sprintHelpers }) => {
    const sprint = await sprintHelpers.createSprint(token, {
      projectId,
      name: "Planning Detail VR",
      goal: "Visual regression for planning state",
      startDate: new Date().toISOString().split("T")[0],
      endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
        .toISOString()
        .split("T")[0],
    });

    await sprintHelpers.authenticatePage(page, { accessToken: token, refreshToken: "" });
    await page.goto(`/projects/${projectId}/sprints/${sprint.id}`);

    await expect(
      page.getByRole("heading", { name: "Planning Detail VR" }),
    ).toBeVisible({ timeout: 10_000 });

    await expect(page.getByTestId("sprint-status-planning")).toBeVisible();

    // Light mode
    await expect(page).toHaveScreenshot("sprint-detail-planning-light.png", {
      maxDiffPixelRatio: 0.05,
    });

    // Dark mode
    await toggleTheme(page);
    await expect(page).toHaveScreenshot("sprint-detail-planning-dark.png", {
      maxDiffPixelRatio: 0.05,
    });
  });

  test("sprint detail page - Active state", async ({ page, sprintHelpers }) => {
    const sprint = await sprintHelpers.createSprint(token, {
      projectId,
      name: "Active Detail VR",
      startDate: new Date().toISOString().split("T")[0],
      endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
        .toISOString()
        .split("T")[0],
    });

    const item = await sprintHelpers.createWorkItem(token, {
      projectId,
      title: "Active VR Item",
      estimationValue: 8,
    });
    await sprintHelpers.addItemToSprint(token, sprint.id, item.id);
    await sprintHelpers.startSprint(token, sprint.id);

    await sprintHelpers.authenticatePage(page, { accessToken: token, refreshToken: "" });
    await page.goto(`/projects/${projectId}/sprints/${sprint.id}`);

    await expect(
      page.getByRole("heading", { name: "Active Detail VR" }),
    ).toBeVisible({ timeout: 10_000 });

    await expect(page.getByTestId("sprint-status-active")).toBeVisible();

    // Light mode
    await expect(page).toHaveScreenshot("sprint-detail-active-light.png", {
      maxDiffPixelRatio: 0.05,
    });

    // Dark mode
    await toggleTheme(page);
    await expect(page).toHaveScreenshot("sprint-detail-active-dark.png", {
      maxDiffPixelRatio: 0.05,
    });
  });

  test("sprint detail page - Completed state", async ({ page, sprintHelpers }) => {
    const sprint = await sprintHelpers.createSprint(token, {
      projectId,
      name: "Completed Detail VR",
      startDate: new Date().toISOString().split("T")[0],
      endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
        .toISOString()
        .split("T")[0],
    });

    const item = await sprintHelpers.createWorkItem(token, {
      projectId,
      title: "Completed VR Item",
      estimationValue: 5,
    });
    await sprintHelpers.addItemToSprint(token, sprint.id, item.id);
    await sprintHelpers.startSprint(token, sprint.id);
    await sprintHelpers.completeSprint(token, sprint.id);

    await sprintHelpers.authenticatePage(page, { accessToken: token, refreshToken: "" });
    await page.goto(`/projects/${projectId}/sprints/${sprint.id}`);

    await expect(
      page.getByRole("heading", { name: "Completed Detail VR" }),
    ).toBeVisible({ timeout: 10_000 });

    await expect(page.getByTestId("sprint-status-completed")).toBeVisible();

    // Light mode
    await expect(page).toHaveScreenshot("sprint-detail-completed-light.png", {
      maxDiffPixelRatio: 0.05,
    });

    // Dark mode
    await toggleTheme(page);
    await expect(page).toHaveScreenshot("sprint-detail-completed-dark.png", {
      maxDiffPixelRatio: 0.05,
    });
  });

  test("burndown chart", async ({ page, sprintHelpers }) => {
    const sprint = await sprintHelpers.createSprint(token, {
      projectId,
      name: "Burndown VR Sprint",
      startDate: new Date().toISOString().split("T")[0],
      endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
        .toISOString()
        .split("T")[0],
    });

    const item = await sprintHelpers.createWorkItem(token, {
      projectId,
      title: "Burndown VR Item",
      estimationValue: 10,
    });
    await sprintHelpers.addItemToSprint(token, sprint.id, item.id);
    await sprintHelpers.startSprint(token, sprint.id);

    await sprintHelpers.authenticatePage(page, { accessToken: token, refreshToken: "" });
    await page.goto(`/projects/${projectId}/sprints/${sprint.id}`);

    await expect(
      page.getByRole("heading", { name: "Burndown VR Sprint" }),
    ).toBeVisible({ timeout: 10_000 });

    // Wait for burndown chart to render (or empty state)
    const burndownChart = page.getByTestId("burndown-chart");
    await expect(burndownChart).toBeVisible({ timeout: 10_000 });

    // Light mode - capture just the burndown area
    await expect(burndownChart).toHaveScreenshot("burndown-chart-light.png", {
      maxDiffPixelRatio: 0.05,
    });

    // Dark mode
    await toggleTheme(page);
    await expect(burndownChart).toHaveScreenshot("burndown-chart-dark.png", {
      maxDiffPixelRatio: 0.05,
    });
  });

  test("sprint planning board with backlog panel", async ({ page, sprintHelpers }) => {
    const sprint = await sprintHelpers.createSprint(token, {
      projectId,
      name: "Planning Board VR",
      startDate: new Date().toISOString().split("T")[0],
      endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
        .toISOString()
        .split("T")[0],
    });

    // Create backlog items
    await sprintHelpers.createWorkItem(token, {
      projectId,
      title: "Backlog VR Item 1",
      estimationValue: 3,
    });
    await sprintHelpers.createWorkItem(token, {
      projectId,
      title: "Backlog VR Item 2",
      estimationValue: 5,
    });

    // Add one item to sprint
    const sprintItem = await sprintHelpers.createWorkItem(token, {
      projectId,
      title: "Sprint VR Item",
      estimationValue: 8,
    });
    await sprintHelpers.addItemToSprint(token, sprint.id, sprintItem.id);

    await sprintHelpers.authenticatePage(page, { accessToken: token, refreshToken: "" });
    await page.goto(`/projects/${projectId}/sprints/${sprint.id}`);

    const planningBoard = page.getByTestId("sprint-planning-board");
    await expect(planningBoard).toBeVisible({ timeout: 10_000 });

    // Verify both panels are visible
    await expect(page.getByTestId("backlog-panel")).toBeVisible();
    await expect(page.getByTestId("sprint-panel")).toBeVisible();

    // Light mode
    await expect(planningBoard).toHaveScreenshot("sprint-planning-board-light.png", {
      maxDiffPixelRatio: 0.05,
    });

    // Dark mode
    await toggleTheme(page);
    await expect(planningBoard).toHaveScreenshot("sprint-planning-board-dark.png", {
      maxDiffPixelRatio: 0.05,
    });
  });
});
