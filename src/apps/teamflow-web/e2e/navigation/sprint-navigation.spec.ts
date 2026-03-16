import { test, expect } from "../fixtures";

const API_URL = process.env.API_URL ?? "http://localhost:5210/api/v1";

/**
 * Cross-page navigation tests for sprint flows.
 *
 * Verifies navigation from Projects list -> Project detail -> Sprints tab
 * -> Sprint detail, including deep-linking and back navigation.
 */
test.describe("Sprint Navigation", () => {
  let token: string;
  let projectId: string;
  let projectName: string;
  let sprintId: string;
  let sprintName: string;

  test.beforeAll(async ({ request }) => {
    // Register user
    const email = `e2e-${Date.now()}-nav@teamflow.dev`;
    const password = "Test@1234";
    const regRes = await request.post(`${API_URL}/auth/register`, {
      data: { email, password, name: "E2E Nav" },
    });
    expect(regRes.status()).toBe(201);
    const body = await regRes.json();
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
          data: { name: `E2E Nav Org ${Date.now()}` },
        });
        orgId = (await createOrgRes.json()).id;
      }
    } else {
      const createOrgRes = await request.post(`${API_URL}/organizations`, {
        headers: { Authorization: `Bearer ${token}` },
        data: { name: `E2E Nav Org ${Date.now()}` },
      });
      orgId = (await createOrgRes.json()).id;
    }

    projectName = `Nav Test Project ${Date.now()}`;
    const projRes = await request.post(`${API_URL}/projects`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { orgId, name: projectName },
    });
    const project = await projRes.json();
    projectId = project.id;

    // Create a sprint with items
    sprintName = "Nav Test Sprint";
    const sprintRes = await request.post(`${API_URL}/sprints`, {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        projectId,
        name: sprintName,
        goal: "Navigation test sprint",
        startDate: new Date().toISOString().split("T")[0],
        endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
          .toISOString()
          .split("T")[0],
      },
    });
    const sprint = await sprintRes.json();
    sprintId = sprint.id;

    // Add an item and start sprint for burndown visibility
    const itemRes = await request.post(`${API_URL}/workitems`, {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        projectId,
        type: "Task",
        title: "Nav Test Item",
        priority: "Medium",
      },
    });
    const item = await itemRes.json();
    const addItemRes = await request.post(`${API_URL}/sprints/${sprintId}/items/${item.id}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    expect(addItemRes.ok()).toBe(true);
    const startRes = await request.post(`${API_URL}/sprints/${sprintId}/start`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    expect(startRes.ok()).toBe(true);
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

  test("navigate from Projects list to Project detail to Sprints to Sprint detail", async ({
    page,
    sprintHelpers,
  }) => {
    await sprintHelpers.authenticatePage(page, { accessToken: token, refreshToken: "" });

    // Step 1: Navigate directly to the project backlog
    // (The /projects list page now redirects to /onboarding for org-based routing)
    await page.goto(`/projects/${projectId}/backlog`);
    await expect(page).toHaveURL(new RegExp(`/projects/${projectId}`));

    // Step 3: Navigate to Sprints tab
    // Find the "Sprints" navigation link/tab
    const sprintsNav = page.getByRole("link", { name: /sprints/i }).or(
      page.getByRole("tab", { name: /sprints/i }),
    );
    await expect(sprintsNav.first()).toBeVisible({ timeout: 5_000 });
    await sprintsNav.first().click();

    // Verify URL updated to sprints page
    await expect(page).toHaveURL(new RegExp(`/projects/${projectId}/sprints`));

    // Verify sprints page loaded
    await expect(page.getByRole("heading", { name: "Sprints" })).toBeVisible({
      timeout: 10_000,
    });

    // Step 4: Click on the sprint card to navigate to sprint detail
    await expect(page.getByText(sprintName)).toBeVisible({ timeout: 5_000 });
    await page.getByText(sprintName).click();

    // Verify URL updated to sprint detail
    await expect(page).toHaveURL(
      new RegExp(`/projects/${projectId}/sprints/${sprintId}`),
    );

    // Verify sprint detail loaded
    await expect(
      page.getByRole("heading", { name: sprintName }),
    ).toBeVisible({ timeout: 10_000 });
  });

  test("verify back navigation works from sprint detail", async ({
    page,
    sprintHelpers,
  }) => {
    await sprintHelpers.authenticatePage(page, { accessToken: token, refreshToken: "" });

    // Start at sprint detail
    await page.goto(`/projects/${projectId}/sprints/${sprintId}`);
    await expect(
      page.getByRole("heading", { name: sprintName }),
    ).toBeVisible({ timeout: 10_000 });

    // Click "Back to Sprints" link
    const backButton = page.getByRole("button", { name: /back to sprints/i }).or(
      page.getByText(/back to sprints/i),
    );
    await expect(backButton.first()).toBeVisible();
    await backButton.first().click();

    // Verify we are back on the sprints list
    await expect(page).toHaveURL(
      new RegExp(`/projects/${projectId}/sprints$`),
    );
    await expect(page.getByRole("heading", { name: "Sprints" })).toBeVisible({
      timeout: 10_000,
    });
  });

  test("deep-link to sprint detail loads correctly", async ({
    page,
    sprintHelpers,
  }) => {
    await sprintHelpers.authenticatePage(page, { accessToken: token, refreshToken: "" });

    // Navigate directly to sprint detail URL (deep link)
    await page.goto(`/projects/${projectId}/sprints/${sprintId}`);

    // Verify the page loads correctly without prior navigation
    await expect(page).toHaveURL(
      new RegExp(`/projects/${projectId}/sprints/${sprintId}`),
    );

    await expect(
      page.getByRole("heading", { name: sprintName }),
    ).toBeVisible({ timeout: 10_000 });

    // Verify key elements are present
    await expect(page.getByTestId("sprint-status-active")).toBeVisible({ timeout: 5_000 });

    // Verify burndown section is visible (chart with data or empty state message)
    const burndownChart = page.getByTestId("burndown-chart");
    const burndownEmpty = page.getByText(/no burndown data available yet/i);
    const hasBurndown = await burndownChart.isVisible().catch(() => false);
    const hasEmpty = await burndownEmpty.isVisible().catch(() => false);
    expect(hasBurndown || hasEmpty).toBe(true);
  });

  test("deep-link to sprints list loads correctly", async ({
    page,
    sprintHelpers,
  }) => {
    await sprintHelpers.authenticatePage(page, { accessToken: token, refreshToken: "" });

    await page.goto(`/projects/${projectId}/sprints`);

    await expect(page).toHaveURL(
      new RegExp(`/projects/${projectId}/sprints`),
    );
    await expect(page.getByRole("heading", { name: "Sprints" })).toBeVisible({
      timeout: 10_000,
    });

    // Verify our sprint card is present
    await expect(page.getByText(sprintName)).toBeVisible({ timeout: 5_000 });
  });

  test("navigating to non-existent sprint shows error state", async ({
    page,
    sprintHelpers,
  }) => {
    await sprintHelpers.authenticatePage(page, { accessToken: token, refreshToken: "" });

    const fakeSprintId = "00000000-0000-0000-0000-000000000000";
    await page.goto(`/projects/${projectId}/sprints/${fakeSprintId}`);

    // Should show error state
    await expect(
      page.getByText(/failed to load sprint/i).or(
        page.getByText(/not found/i),
      ),
    ).toBeVisible({ timeout: 10_000 });
  });
});
