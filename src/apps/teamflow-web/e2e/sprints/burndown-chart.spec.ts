import { test, expect } from "@playwright/test";
import {
  registerUser,
  createProject,
  createSprint,
  createWorkItem,
  addItemToSprint,
  startSprint,
  authenticatePage,
} from "../fixtures/sprint-helpers";

const API_URL = process.env.API_URL ?? "http://localhost:5210/api/v1";

test.describe("Burndown Chart", () => {
  let token: string;
  let projectId: string;

  test.beforeAll(async ({ request }) => {
    const user = await registerUser(request, "burndown");
    token = user.accessToken;
    const project = await createProject(
      request,
      token,
      "Burndown Chart E2E",
    );
    projectId = project.id;
  });

  test("burndown chart section is visible on active sprint detail page", async ({
    page,
    request,
  }) => {
    // Create and start a sprint
    const sprint = await createSprint(request, token, {
      projectId,
      name: "E2E Burndown Sprint",
      startDate: new Date().toISOString().split("T")[0],
      endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
        .toISOString()
        .split("T")[0],
    });

    const item = await createWorkItem(request, token, {
      projectId,
      title: "Burndown Test Item",
      estimationValue: 8,
    });
    await addItemToSprint(request, token, sprint.id, item.id);
    await startSprint(request, token, sprint.id);

    // Navigate to sprint detail
    await authenticatePage(page, { accessToken: token, refreshToken: "" });
    await page.goto(`/projects/${projectId}/sprints/${sprint.id}`);

    // Verify the sprint is active
    await expect(
      page.getByRole("heading", { name: "E2E Burndown Sprint" }),
    ).toBeVisible({ timeout: 10_000 });
    await expect(page.getByText("Active").first()).toBeVisible();

    // Verify burndown section renders (the "BURNDOWN" label or chart container)
    // The BurndownChart component shows "Burndown" text as a label
    // If there is data, it shows the chart; if not, it shows empty state message
    const burndownLabel = page.getByText("Burndown", { exact: true });
    const emptyMessage = page.getByText(
      /no burndown data available yet/i,
    );

    // Either the chart with "Burndown" label or the empty state should be visible
    const hasBurndownLabel = await burndownLabel.isVisible().catch(() => false);
    const hasEmptyMessage = await emptyMessage.isVisible().catch(() => false);

    expect(hasBurndownLabel || hasEmptyMessage).toBe(true);
  });

  test("burndown chart renders Ideal and Actual lines when data exists", async ({
    page,
    request,
  }) => {
    // Create and start a sprint with burndown data
    const sprint = await createSprint(request, token, {
      projectId,
      name: "E2E Burndown Data Sprint",
      startDate: new Date().toISOString().split("T")[0],
      endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
        .toISOString()
        .split("T")[0],
    });

    const item = await createWorkItem(request, token, {
      projectId,
      title: "Burndown Data Item",
      estimationValue: 10,
    });
    await addItemToSprint(request, token, sprint.id, item.id);
    await startSprint(request, token, sprint.id);

    // Check if burndown data exists via API
    const burndownRes = await request.get(
      `${API_URL}/sprints/${sprint.id}/burndown`,
      { headers: { Authorization: `Bearer ${token}` } },
    );
    expect(burndownRes.ok()).toBe(true);
    const burndownData = await burndownRes.json();

    // Navigate to sprint detail
    await authenticatePage(page, { accessToken: token, refreshToken: "" });
    await page.goto(`/projects/${projectId}/sprints/${sprint.id}`);

    await expect(
      page.getByRole("heading", { name: "E2E Burndown Data Sprint" }),
    ).toBeVisible({ timeout: 10_000 });

    if (
      burndownData.idealLine.length > 0 ||
      burndownData.actualLine.length > 0
    ) {
      // When burndown data exists, the Recharts chart renders with legend items
      // "Ideal" and "Actual" lines are shown in the Legend component
      await expect(page.getByText("Ideal")).toBeVisible({ timeout: 5_000 });
      await expect(page.getByText("Actual")).toBeVisible();

      // Verify the Recharts SVG container is present
      // Recharts renders inside a <svg> element within the ResponsiveContainer
      const svgElement = page.locator(".recharts-responsive-container svg");
      await expect(svgElement).toBeVisible();
    } else {
      // If no burndown data yet (sprint just started, BurndownSnapshotJob hasn't run),
      // verify the empty state message
      await expect(
        page.getByText(/no burndown data available yet/i),
      ).toBeVisible();
    }
  });

  test("burndown chart is NOT visible on Planning sprint", async ({
    page,
    request,
  }) => {
    // Create a sprint that stays in Planning status
    const sprint = await createSprint(request, token, {
      projectId,
      name: "E2E No Burndown Sprint",
      startDate: new Date().toISOString().split("T")[0],
      endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
        .toISOString()
        .split("T")[0],
    });

    await authenticatePage(page, { accessToken: token, refreshToken: "" });
    await page.goto(`/projects/${projectId}/sprints/${sprint.id}`);

    await expect(
      page.getByRole("heading", { name: "E2E No Burndown Sprint" }),
    ).toBeVisible({ timeout: 10_000 });

    // Planning sprint should show the planning board, NOT the burndown chart
    await expect(page.getByText("Planning").first()).toBeVisible();

    // The burndown label or recharts should NOT be present
    // (Burndown chart only shows for Active or Completed sprints)
    const burndownLabel = page.getByText("Burndown", { exact: true });
    await expect(burndownLabel).not.toBeVisible();
  });

  test("burndown API endpoint returns correct shape", async ({ request }) => {
    // Create and start a sprint
    const sprint = await createSprint(request, token, {
      projectId,
      name: "E2E Burndown API Sprint",
      startDate: new Date().toISOString().split("T")[0],
      endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
        .toISOString()
        .split("T")[0],
    });

    const item = await createWorkItem(request, token, {
      projectId,
      title: "Burndown API Item",
      estimationValue: 5,
    });
    await addItemToSprint(request, token, sprint.id, item.id);
    await startSprint(request, token, sprint.id);

    // Call burndown endpoint
    const response = await request.get(
      `${API_URL}/sprints/${sprint.id}/burndown`,
      { headers: { Authorization: `Bearer ${token}` } },
    );
    expect(response.ok()).toBe(true);

    const data = await response.json();

    // Verify response shape matches BurndownDto contract
    expect(data).toHaveProperty("sprintId");
    expect(data).toHaveProperty("idealLine");
    expect(data).toHaveProperty("actualLine");
    expect(data.sprintId).toBe(sprint.id);
    expect(Array.isArray(data.idealLine)).toBe(true);
    expect(Array.isArray(data.actualLine)).toBe(true);

    // Each ideal line point should have date and points
    for (const point of data.idealLine) {
      expect(point).toHaveProperty("date");
      expect(point).toHaveProperty("points");
    }

    // Each actual line point should have date, remainingPoints, completedPoints, addedPoints
    for (const point of data.actualLine) {
      expect(point).toHaveProperty("date");
      expect(point).toHaveProperty("remainingPoints");
      expect(point).toHaveProperty("completedPoints");
      expect(point).toHaveProperty("addedPoints");
    }
  });
});
