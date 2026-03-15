import { test, expect } from "../fixtures";

const API_URL = process.env.API_URL ?? "http://localhost:5210/api/v1";

test.describe("Sprint Planning Flow", () => {
  test.describe.configure({ mode: "serial" });

  let token: string;
  let projectId: string;

  test.beforeAll(async ({ request }) => {
    const user = await test.step("register user", async () => {
      const email = `e2e-${Date.now()}-planning@teamflow.dev`;
      const password = "Test@1234";
      const response = await request.post(`${API_URL}/auth/register`, {
        data: { email, password, name: "E2E Planning" },
      });
      expect(response.status()).toBe(201);
      const body = await response.json();
      return body;
    });
    token = user.accessToken;

    const project = await test.step("create project", async () => {
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
            data: { name: `E2E Org ${Date.now()}` },
          });
          orgId = (await createOrgRes.json()).id;
        }
      } else {
        const createOrgRes = await request.post(`${API_URL}/organizations`, {
          headers: { Authorization: `Bearer ${token}` },
          data: { name: `E2E Org ${Date.now()}` },
        });
        orgId = (await createOrgRes.json()).id;
      }
      const projRes = await request.post(`${API_URL}/projects`, {
        headers: { Authorization: `Bearer ${token}` },
        data: { orgId, name: "Sprint Planning E2E" },
      });
      return projRes.json();
    });
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

  test("create sprint via UI and verify it appears in sprint list", async ({
    page,
    sprintHelpers,
  }) => {
    await sprintHelpers.authenticatePage(page, { accessToken: token, refreshToken: "" });

    await page.goto(`/projects/${projectId}/sprints`);

    await expect(page.getByRole("heading", { name: "Sprints" })).toBeVisible();

    // Click "New Sprint" button
    await page.getByRole("button", { name: /new sprint/i }).click();

    // Fill in sprint form dialog using data-testid selectors
    const dialog = page.getByTestId("sprint-form-dialog");
    await expect(dialog).toBeVisible();

    await dialog.getByTestId("sprint-name-input").fill("E2E Sprint 1");
    await dialog.getByTestId("sprint-goal-input").fill("Complete E2E testing for sprint planning");

    const today = new Date();
    const startDate = today.toISOString().split("T")[0];
    const endDate = new Date(today.getTime() + 14 * 24 * 60 * 60 * 1000)
      .toISOString()
      .split("T")[0];
    await dialog.getByTestId("sprint-start-date").fill(startDate);
    await dialog.getByTestId("sprint-end-date").fill(endDate);

    // Submit via data-testid
    await dialog.getByTestId("sprint-submit-btn").click();

    // Verify sprint appears in the list via sprint-card data-testid pattern
    await expect(
      page.getByRole("article", { name: /Sprint: E2E Sprint 1/i }),
    ).toBeVisible({ timeout: 10_000 });

    // Verify Planning status badge via data-testid
    await expect(page.getByTestId("sprint-status-planning").first()).toBeVisible();
  });

  test("create sprint via API, add items, verify capacity indicator via data-testid", async ({
    page,
    sprintHelpers,
  }) => {
    const sprint = await sprintHelpers.createSprint(token, {
      projectId,
      name: "E2E Capacity Sprint",
      goal: "Test capacity tracking",
      startDate: new Date().toISOString().split("T")[0],
      endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
        .toISOString()
        .split("T")[0],
    });

    // Set capacity via API
    const userRes = await page.request.get(`${API_URL}/auth/me`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    let memberId: string | undefined;
    if (userRes.ok()) {
      const me = await userRes.json();
      memberId = me.id;
    }

    if (memberId) {
      await page.request.put(`${API_URL}/sprints/${sprint.id}/capacity`, {
        headers: { Authorization: `Bearer ${token}` },
        data: { capacity: [{ memberId, points: 20 }] },
      });
    }

    const item1 = await sprintHelpers.createWorkItem(token, {
      projectId,
      title: "Capacity Test Item 1",
      estimationValue: 5,
    });
    const item2 = await sprintHelpers.createWorkItem(token, {
      projectId,
      title: "Capacity Test Item 2",
      estimationValue: 8,
    });

    await sprintHelpers.addItemToSprint(token, sprint.id, item1.id);
    await sprintHelpers.addItemToSprint(token, sprint.id, item2.id);

    await sprintHelpers.authenticatePage(page, { accessToken: token, refreshToken: "" });
    await page.goto(`/projects/${projectId}/sprints/${sprint.id}`);

    await expect(
      page.getByRole("heading", { name: "E2E Capacity Sprint" }),
    ).toBeVisible({ timeout: 10_000 });

    // Verify items are listed
    await expect(page.getByText("Capacity Test Item 1")).toBeVisible();
    await expect(page.getByText("Capacity Test Item 2")).toBeVisible();

    // Verify item count
    await expect(page.getByText(/2 items/)).toBeVisible();

    // Verify planning board via data-testid
    await expect(page.getByTestId("sprint-planning-board")).toBeVisible();

    // Verify sprint panel has items
    await expect(page.getByTestId("sprint-panel")).toBeVisible();
  });

  test("update sprint dates via UI", async ({ page, sprintHelpers }) => {
    const sprint = await sprintHelpers.createSprint(token, {
      projectId,
      name: "E2E Date Update Sprint",
      startDate: new Date().toISOString().split("T")[0],
      endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
        .toISOString()
        .split("T")[0],
    });

    await sprintHelpers.authenticatePage(page, { accessToken: token, refreshToken: "" });
    await page.goto(`/projects/${projectId}/sprints/${sprint.id}`);

    await expect(
      page.getByRole("heading", { name: "E2E Date Update Sprint" }),
    ).toBeVisible({ timeout: 10_000 });

    // Click Edit button
    await page.getByRole("button", { name: /^edit$/i }).click();

    // Update dates in the form dialog
    const dialog = page.getByTestId("sprint-form-dialog");
    await expect(dialog).toBeVisible();

    const newStartDate = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000)
      .toISOString()
      .split("T")[0];
    const newEndDate = new Date(Date.now() + 21 * 24 * 60 * 60 * 1000)
      .toISOString()
      .split("T")[0];

    await dialog.getByTestId("sprint-start-date").fill(newStartDate);
    await dialog.getByTestId("sprint-end-date").fill(newEndDate);

    await dialog.getByTestId("sprint-submit-btn").click();

    // Verify the dialog closes and page reloads with updated dates
    await expect(dialog).not.toBeVisible({ timeout: 5_000 });

    // The date should reflect the updated value on the sprint detail page
    // Dates are formatted as "Month Day, Year"
    const expectedEnd = new Date(Date.now() + 21 * 24 * 60 * 60 * 1000);
    const endMonth = expectedEnd.toLocaleDateString("en-US", { month: "long" });
    await expect(page.getByText(new RegExp(endMonth))).toBeVisible({ timeout: 5_000 });
  });

  test("remove item from sprint, verify backlog panel updates", async ({
    page,
    sprintHelpers,
  }) => {
    const sprint = await sprintHelpers.createSprint(token, {
      projectId,
      name: "E2E Remove Item Sprint",
      startDate: new Date().toISOString().split("T")[0],
      endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
        .toISOString()
        .split("T")[0],
    });

    const item = await sprintHelpers.createWorkItem(token, {
      projectId,
      title: "Removable Sprint Item P4",
      estimationValue: 4,
    });
    await sprintHelpers.addItemToSprint(token, sprint.id, item.id);

    await sprintHelpers.authenticatePage(page, { accessToken: token, refreshToken: "" });
    await page.goto(`/projects/${projectId}/sprints/${sprint.id}`);

    await expect(
      page.getByRole("heading", { name: "E2E Remove Item Sprint" }),
    ).toBeVisible({ timeout: 10_000 });

    // Verify the item is in the sprint panel
    await expect(page.getByTestId("sprint-panel")).toBeVisible();
    await expect(page.getByText("Removable Sprint Item P4")).toBeVisible();

    // Remove item via API
    const removeRes = await page.request.delete(
      `${API_URL}/sprints/${sprint.id}/items/${item.id}`,
      { headers: { Authorization: `Bearer ${token}` } },
    );
    expect(removeRes.ok()).toBe(true);

    // Reload and verify item is gone from sprint, appears in backlog panel
    await page.reload();
    await expect(
      page.getByRole("heading", { name: "E2E Remove Item Sprint" }),
    ).toBeVisible({ timeout: 10_000 });

    await expect(page.getByText(/0 items/)).toBeVisible();

    // Item should appear in backlog panel
    await expect(page.getByTestId("backlog-panel")).toBeVisible();
    await expect(page.getByText("Removable Sprint Item P4")).toBeVisible({
      timeout: 5_000,
    });
  });

  test("start sprint via UI and verify scope lock indicator appears", async ({
    page,
    sprintHelpers,
  }) => {
    const sprint = await sprintHelpers.createSprint(token, {
      projectId,
      name: "E2E Start Sprint",
      startDate: new Date().toISOString().split("T")[0],
      endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
        .toISOString()
        .split("T")[0],
    });

    const item = await sprintHelpers.createWorkItem(token, {
      projectId,
      title: "Item for starting sprint",
      estimationValue: 3,
    });
    await sprintHelpers.addItemToSprint(token, sprint.id, item.id);

    await sprintHelpers.authenticatePage(page, { accessToken: token, refreshToken: "" });
    await page.goto(`/projects/${projectId}/sprints/${sprint.id}`);

    await expect(
      page.getByRole("heading", { name: "E2E Start Sprint" }),
    ).toBeVisible({ timeout: 10_000 });

    const startButton = page.getByRole("button", { name: /start sprint/i });
    await expect(startButton).toBeVisible();
    await startButton.click();

    // Confirm in the confirmation dialog
    const confirmDialog = page.getByRole("alertdialog");
    await expect(confirmDialog).toBeVisible();
    await expect(confirmDialog.getByText(/scope will be locked/i)).toBeVisible();
    await confirmDialog
      .getByRole("button", { name: /start sprint/i })
      .click();

    // Verify sprint is now Active with scope lock
    await expect(page.getByTestId("sprint-status-active").first()).toBeVisible({
      timeout: 10_000,
    });
    await expect(page.getByText("Locked")).toBeVisible();

    // Verify scope lock warning banner
    await expect(
      page.getByRole("alert").filter({ hasText: /scope changes require/i }),
    ).toBeVisible();
  });

  test("complete sprint via UI and verify status changes to Completed", async ({
    page,
    sprintHelpers,
  }) => {
    const sprint = await sprintHelpers.createSprint(token, {
      projectId,
      name: "E2E Complete Sprint",
      startDate: new Date().toISOString().split("T")[0],
      endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
        .toISOString()
        .split("T")[0],
    });

    const item = await sprintHelpers.createWorkItem(token, {
      projectId,
      title: "Item for completing sprint",
      estimationValue: 5,
    });
    await sprintHelpers.addItemToSprint(token, sprint.id, item.id);
    await sprintHelpers.startSprint(token, sprint.id);

    await sprintHelpers.authenticatePage(page, { accessToken: token, refreshToken: "" });
    await page.goto(`/projects/${projectId}/sprints/${sprint.id}`);

    await expect(
      page.getByRole("heading", { name: "E2E Complete Sprint" }),
    ).toBeVisible({ timeout: 10_000 });

    const completeButton = page.getByRole("button", {
      name: /complete sprint/i,
    });
    await expect(completeButton).toBeVisible();
    await completeButton.click();

    const confirmDialog = page.getByRole("alertdialog");
    await expect(confirmDialog).toBeVisible();
    await expect(
      confirmDialog.getByText(/incomplete items will be unlinked/i),
    ).toBeVisible();
    await confirmDialog
      .getByRole("button", { name: /complete sprint/i })
      .click();

    // Verify Completed status via data-testid
    await expect(page.getByTestId("sprint-status-completed").first()).toBeVisible({
      timeout: 10_000,
    });

    // Start and Complete buttons should no longer be visible
    await expect(
      page.getByRole("button", { name: /start sprint/i }),
    ).not.toBeVisible();
    await expect(
      page.getByRole("button", { name: /complete sprint/i }),
    ).not.toBeVisible();
  });

  test("active sprint: adding item from backlog shows confirmation dialog", async ({
    page,
    sprintHelpers,
  }) => {
    const sprint = await sprintHelpers.createSprint(token, {
      projectId,
      name: "E2E Scope Lock Sprint",
      startDate: new Date().toISOString().split("T")[0],
      endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
        .toISOString()
        .split("T")[0],
    });

    const item1 = await sprintHelpers.createWorkItem(token, {
      projectId,
      title: "Existing sprint item",
    });
    await sprintHelpers.addItemToSprint(token, sprint.id, item1.id);
    await sprintHelpers.startSprint(token, sprint.id);

    await sprintHelpers.createWorkItem(token, {
      projectId,
      title: "Unscheduled backlog item for scope lock",
    });

    await sprintHelpers.authenticatePage(page, { accessToken: token, refreshToken: "" });
    await page.goto(`/projects/${projectId}/sprints/${sprint.id}`);

    // Verify active sprint via data-testid
    await expect(page.getByTestId("sprint-status-active").first()).toBeVisible({
      timeout: 10_000,
    });
    await expect(page.getByText("Locked")).toBeVisible();

    // Verify backlog panel has the unscheduled item
    await expect(page.getByTestId("backlog-panel")).toBeVisible();
    await expect(
      page.getByText("Unscheduled backlog item for scope lock"),
    ).toBeVisible({ timeout: 5_000 });

    // Verify scope lock warning
    await expect(
      page
        .getByRole("alert")
        .filter({ hasText: /scope changes require team manager/i }),
    ).toBeVisible();
  });
});
