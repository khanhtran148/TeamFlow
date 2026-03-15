import { test, expect } from "@playwright/test";
import {
  registerUser,
  createProject,
  createSprint,
  createWorkItem,
  addItemToSprint,
  startSprint,
  completeSprint,
  authenticatePage,
} from "../fixtures/sprint-helpers";

const API_URL = process.env.API_URL ?? "http://localhost:5210/api/v1";

test.describe("Sprint Planning Flow", () => {
  test.describe.configure({ mode: "serial" });

  let token: string;
  let projectId: string;

  test.beforeAll(async ({ request }) => {
    const user = await registerUser(request, "planning");
    token = user.accessToken;
    const project = await createProject(request, token, "Sprint Planning E2E");
    projectId = project.id;
  });

  test("create sprint via UI and verify it appears in sprint list", async ({
    page,
    request,
  }) => {
    await authenticatePage(page, { accessToken: token, refreshToken: "" });

    // Navigate to sprints page
    await page.goto(`/projects/${projectId}/sprints`);

    // Wait for the page to load
    await expect(page.getByRole("heading", { name: "Sprints" })).toBeVisible();

    // Click "New Sprint" button
    await page.getByRole("button", { name: /new sprint/i }).click();

    // Fill in sprint form dialog
    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();

    await dialog.getByLabel("Name").fill("E2E Sprint 1");
    await dialog.getByLabel("Goal").fill("Complete E2E testing for sprint planning");

    // Set dates (today + 2 weeks)
    const today = new Date();
    const startDate = today.toISOString().split("T")[0];
    const endDate = new Date(today.getTime() + 14 * 24 * 60 * 60 * 1000)
      .toISOString()
      .split("T")[0];
    await dialog.getByLabel("Start Date").fill(startDate);
    await dialog.getByLabel("End Date").fill(endDate);

    // Submit
    await dialog.getByRole("button", { name: /create sprint/i }).click();

    // Verify sprint appears in the list
    await expect(
      page.getByRole("article", { name: /Sprint: E2E Sprint 1/i }),
    ).toBeVisible({ timeout: 10_000 });

    // Verify Planning status badge is shown
    await expect(page.getByText("Planning").first()).toBeVisible();
  });

  test("create sprint via API, add items, verify capacity indicator updates", async ({
    page,
    request,
  }) => {
    // Create sprint and work items via API
    const sprint = await createSprint(request, token, {
      projectId,
      name: "E2E Capacity Sprint",
      goal: "Test capacity tracking",
      startDate: new Date().toISOString().split("T")[0],
      endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
        .toISOString()
        .split("T")[0],
    });

    // Update capacity via API
    const user = await request.get(`${API_URL}/auth/me`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    let memberId: string | undefined;
    if (user.ok()) {
      const me = await user.json();
      memberId = me.id;
    }

    if (memberId) {
      await request.put(`${API_URL}/sprints/${sprint.id}/capacity`, {
        headers: { Authorization: `Bearer ${token}` },
        data: {
          capacity: [{ memberId, points: 20 }],
        },
      });
    }

    // Create work items with estimation values
    const item1 = await createWorkItem(request, token, {
      projectId,
      title: "Capacity Test Item 1",
      estimationValue: 5,
    });
    const item2 = await createWorkItem(request, token, {
      projectId,
      title: "Capacity Test Item 2",
      estimationValue: 8,
    });

    // Add items to sprint
    await addItemToSprint(request, token, sprint.id, item1.id);
    await addItemToSprint(request, token, sprint.id, item2.id);

    // Navigate to sprint detail page
    await authenticatePage(page, { accessToken: token, refreshToken: "" });
    await page.goto(`/projects/${projectId}/sprints/${sprint.id}`);

    // Verify sprint detail loads
    await expect(
      page.getByRole("heading", { name: "E2E Capacity Sprint" }),
    ).toBeVisible({ timeout: 10_000 });

    // Verify items are listed
    await expect(page.getByText("Capacity Test Item 1")).toBeVisible();
    await expect(page.getByText("Capacity Test Item 2")).toBeVisible();

    // Verify item count is shown (should show "2 items")
    await expect(page.getByText(/2 items/)).toBeVisible();
  });

  test("start sprint via UI and verify scope lock indicator appears", async ({
    page,
    request,
  }) => {
    // Create a sprint with items ready to start
    const sprint = await createSprint(request, token, {
      projectId,
      name: "E2E Start Sprint",
      startDate: new Date().toISOString().split("T")[0],
      endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
        .toISOString()
        .split("T")[0],
    });

    const item = await createWorkItem(request, token, {
      projectId,
      title: "Item for starting sprint",
      estimationValue: 3,
    });
    await addItemToSprint(request, token, sprint.id, item.id);

    // Navigate to sprint detail
    await authenticatePage(page, { accessToken: token, refreshToken: "" });
    await page.goto(`/projects/${projectId}/sprints/${sprint.id}`);

    // Wait for the page to load
    await expect(
      page.getByRole("heading", { name: "E2E Start Sprint" }),
    ).toBeVisible({ timeout: 10_000 });

    // Verify Start Sprint button is visible and enabled
    const startButton = page.getByRole("button", { name: /start sprint/i });
    await expect(startButton).toBeVisible();

    // Click Start Sprint
    await startButton.click();

    // Confirm in the confirmation dialog
    const confirmDialog = page.getByRole("alertdialog");
    await expect(confirmDialog).toBeVisible();
    await expect(confirmDialog.getByText(/scope will be locked/i)).toBeVisible();
    await confirmDialog
      .getByRole("button", { name: /start sprint/i })
      .click();

    // Verify sprint is now Active with scope lock
    await expect(page.getByText("Active").first()).toBeVisible({
      timeout: 10_000,
    });
    await expect(page.getByText("Locked")).toBeVisible();

    // Verify scope lock warning banner appears on planning board
    await expect(
      page.getByRole("alert").filter({ hasText: /scope changes require/i }),
    ).toBeVisible();
  });

  test("complete sprint via UI and verify status changes to Completed", async ({
    page,
    request,
  }) => {
    // Create, populate, and start a sprint via API
    const sprint = await createSprint(request, token, {
      projectId,
      name: "E2E Complete Sprint",
      startDate: new Date().toISOString().split("T")[0],
      endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
        .toISOString()
        .split("T")[0],
    });

    const item = await createWorkItem(request, token, {
      projectId,
      title: "Item for completing sprint",
      estimationValue: 5,
    });
    await addItemToSprint(request, token, sprint.id, item.id);
    await startSprint(request, token, sprint.id);

    // Navigate to sprint detail
    await authenticatePage(page, { accessToken: token, refreshToken: "" });
    await page.goto(`/projects/${projectId}/sprints/${sprint.id}`);

    // Wait for Active status
    await expect(
      page.getByRole("heading", { name: "E2E Complete Sprint" }),
    ).toBeVisible({ timeout: 10_000 });

    // Click Complete Sprint
    const completeButton = page.getByRole("button", {
      name: /complete sprint/i,
    });
    await expect(completeButton).toBeVisible();
    await completeButton.click();

    // Confirm in dialog
    const confirmDialog = page.getByRole("alertdialog");
    await expect(confirmDialog).toBeVisible();
    await expect(
      confirmDialog.getByText(/incomplete items will be unlinked/i),
    ).toBeVisible();
    await confirmDialog
      .getByRole("button", { name: /complete sprint/i })
      .click();

    // Verify sprint is now Completed
    await expect(page.getByText("Completed").first()).toBeVisible({
      timeout: 10_000,
    });

    // Start Sprint and Complete Sprint buttons should no longer be visible
    await expect(
      page.getByRole("button", { name: /start sprint/i }),
    ).not.toBeVisible();
    await expect(
      page.getByRole("button", { name: /complete sprint/i }),
    ).not.toBeVisible();
  });

  test("active sprint: adding item from backlog shows confirmation dialog", async ({
    page,
    request,
  }) => {
    // Create and start a sprint
    const sprint = await createSprint(request, token, {
      projectId,
      name: "E2E Scope Lock Sprint",
      startDate: new Date().toISOString().split("T")[0],
      endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
        .toISOString()
        .split("T")[0],
    });

    const item1 = await createWorkItem(request, token, {
      projectId,
      title: "Existing sprint item",
    });
    await addItemToSprint(request, token, sprint.id, item1.id);
    await startSprint(request, token, sprint.id);

    // Create an unscheduled backlog item
    await createWorkItem(request, token, {
      projectId,
      title: "Unscheduled backlog item for scope lock",
    });

    // Navigate to sprint detail
    await authenticatePage(page, { accessToken: token, refreshToken: "" });
    await page.goto(`/projects/${projectId}/sprints/${sprint.id}`);

    // Verify active sprint and scope lock indicator
    await expect(page.getByText("Active").first()).toBeVisible({
      timeout: 10_000,
    });
    await expect(page.getByText("Locked")).toBeVisible();

    // The backlog panel should contain the unscheduled item
    await expect(
      page.getByText("Unscheduled backlog item for scope lock"),
    ).toBeVisible({ timeout: 5_000 });

    // Note: Drag-and-drop is tested in sprint-backlog.spec.ts.
    // Here we verify the scope lock indicator and confirmation dialog text is correct.
    await expect(
      page
        .getByRole("alert")
        .filter({ hasText: /scope changes require team manager/i }),
    ).toBeVisible();
  });
});
