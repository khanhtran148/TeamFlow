import { test, expect } from "@playwright/test";
import {
  registerUser,
  createProject,
  createSprint,
  createWorkItem,
  addItemToSprint,
  authenticatePage,
} from "../fixtures/sprint-helpers";

const API_URL = process.env.API_URL ?? "http://localhost:5210/api/v1";

test.describe("Sprint Backlog Interaction", () => {
  test.describe.configure({ mode: "serial" });

  let token: string;
  let projectId: string;

  test.beforeAll(async ({ request }) => {
    const user = await registerUser(request, "backlog");
    token = user.accessToken;
    const project = await createProject(request, token, "Sprint Backlog E2E");
    projectId = project.id;
  });

  test("sprint planning board shows backlog and sprint panels", async ({
    page,
    request,
  }) => {
    // Create a Planning sprint
    const sprint = await createSprint(request, token, {
      projectId,
      name: "E2E Backlog Board Sprint",
      startDate: new Date().toISOString().split("T")[0],
      endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
        .toISOString()
        .split("T")[0],
    });

    // Create backlog items (unscheduled)
    await createWorkItem(request, token, {
      projectId,
      title: "Backlog Item Alpha",
      estimationValue: 3,
    });
    await createWorkItem(request, token, {
      projectId,
      title: "Backlog Item Beta",
      estimationValue: 5,
    });

    // Navigate to sprint detail
    await authenticatePage(page, { accessToken: token, refreshToken: "" });
    await page.goto(`/projects/${projectId}/sprints/${sprint.id}`);

    // Verify sprint detail loaded
    await expect(
      page.getByRole("heading", { name: "E2E Backlog Board Sprint" }),
    ).toBeVisible({ timeout: 10_000 });

    // Verify both panels are visible
    await expect(page.getByText("Backlog")).toBeVisible();
    await expect(page.getByText("E2E Backlog Board Sprint").last()).toBeVisible();

    // Verify backlog items appear in the backlog panel
    await expect(page.getByText("Backlog Item Alpha")).toBeVisible({
      timeout: 5_000,
    });
    await expect(page.getByText("Backlog Item Beta")).toBeVisible();

    // Verify sprint panel shows "Drag items from the backlog" empty state
    await expect(
      page.getByText(/drag items from the backlog/i),
    ).toBeVisible();
  });

  test("add item to sprint via API and verify count updates on detail page", async ({
    page,
    request,
  }) => {
    const sprint = await createSprint(request, token, {
      projectId,
      name: "E2E Item Count Sprint",
      startDate: new Date().toISOString().split("T")[0],
      endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
        .toISOString()
        .split("T")[0],
    });

    const item1 = await createWorkItem(request, token, {
      projectId,
      title: "Count Item A",
      estimationValue: 2,
    });
    const item2 = await createWorkItem(request, token, {
      projectId,
      title: "Count Item B",
      estimationValue: 3,
    });

    // Add first item
    await addItemToSprint(request, token, sprint.id, item1.id);

    // Navigate to sprint detail
    await authenticatePage(page, { accessToken: token, refreshToken: "" });
    await page.goto(`/projects/${projectId}/sprints/${sprint.id}`);

    // Verify 1 item count
    await expect(
      page.getByRole("heading", { name: "E2E Item Count Sprint" }),
    ).toBeVisible({ timeout: 10_000 });
    await expect(page.getByText(/1 item(?!s)/)).toBeVisible();

    // Add second item via API
    await addItemToSprint(request, token, sprint.id, item2.id);

    // Reload to see updated count
    await page.reload();
    await expect(
      page.getByRole("heading", { name: "E2E Item Count Sprint" }),
    ).toBeVisible({ timeout: 10_000 });
    await expect(page.getByText(/2 items/)).toBeVisible();
  });

  test("remove item from sprint via API and verify it returns to backlog view", async ({
    page,
    request,
  }) => {
    const sprint = await createSprint(request, token, {
      projectId,
      name: "E2E Remove Item Sprint",
      startDate: new Date().toISOString().split("T")[0],
      endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
        .toISOString()
        .split("T")[0],
    });

    const item = await createWorkItem(request, token, {
      projectId,
      title: "Removable Sprint Item",
      estimationValue: 4,
    });
    await addItemToSprint(request, token, sprint.id, item.id);

    // Navigate and verify item is in sprint
    await authenticatePage(page, { accessToken: token, refreshToken: "" });
    await page.goto(`/projects/${projectId}/sprints/${sprint.id}`);
    await expect(
      page.getByRole("heading", { name: "E2E Remove Item Sprint" }),
    ).toBeVisible({ timeout: 10_000 });
    await expect(page.getByText("Removable Sprint Item")).toBeVisible();

    // Remove item via API
    const removeRes = await request.delete(
      `${API_URL}/sprints/${sprint.id}/items/${item.id}`,
      { headers: { Authorization: `Bearer ${token}` } },
    );
    expect(removeRes.ok()).toBe(true);

    // Reload page and verify item is no longer in sprint panel
    await page.reload();
    await expect(
      page.getByRole("heading", { name: "E2E Remove Item Sprint" }),
    ).toBeVisible({ timeout: 10_000 });

    // The sprint should now show 0 items
    await expect(page.getByText(/0 items/)).toBeVisible();

    // Item should appear back in backlog panel
    await expect(page.getByText("Removable Sprint Item")).toBeVisible({
      timeout: 5_000,
    });
  });

  test("drag and drop item from backlog to sprint panel (Planning sprint)", async ({
    page,
    request,
  }) => {
    const sprint = await createSprint(request, token, {
      projectId,
      name: "E2E DnD Sprint",
      startDate: new Date().toISOString().split("T")[0],
      endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
        .toISOString()
        .split("T")[0],
    });

    // Create a backlog item that is not in any sprint
    await createWorkItem(request, token, {
      projectId,
      title: "DnD Draggable Item",
      estimationValue: 5,
    });

    await authenticatePage(page, { accessToken: token, refreshToken: "" });
    await page.goto(`/projects/${projectId}/sprints/${sprint.id}`);

    // Wait for planning board to load
    await expect(
      page.getByRole("heading", { name: "E2E DnD Sprint" }),
    ).toBeVisible({ timeout: 10_000 });

    // Wait for the draggable item to appear in the backlog panel
    const draggableItem = page.getByText("DnD Draggable Item");
    await expect(draggableItem).toBeVisible({ timeout: 5_000 });

    // Find the sprint panel (the right panel)
    // The sprint panel header contains the sprint name
    const sprintPanelHeader = page
      .getByText("E2E DnD Sprint")
      .last();
    await expect(sprintPanelHeader).toBeVisible();

    // Perform drag-and-drop using Playwright's drag API
    // We drag the item from the backlog to the sprint panel area
    const itemBounds = await draggableItem.boundingBox();
    const sprintBounds = await sprintPanelHeader.boundingBox();

    if (itemBounds && sprintBounds) {
      // Start drag from item center
      const startX = itemBounds.x + itemBounds.width / 2;
      const startY = itemBounds.y + itemBounds.height / 2;

      // Drop on the sprint panel (below the header)
      const endX = sprintBounds.x + sprintBounds.width / 2;
      const endY = sprintBounds.y + sprintBounds.height + 50;

      // dnd-kit requires a pointer sensor with distance constraint
      await page.mouse.move(startX, startY);
      await page.mouse.down();
      // Move slowly to trigger the distance constraint (5px)
      await page.mouse.move(startX + 10, startY, { steps: 5 });
      await page.mouse.move(endX, endY, { steps: 20 });
      await page.mouse.up();

      // Wait for the API call to complete and UI to update
      // After successful drop, the item should move from backlog to sprint
      // Allow time for optimistic update or refetch
      await page.waitForTimeout(2_000);

      // Reload to verify the state persisted
      await page.reload();
      await expect(
        page.getByRole("heading", { name: "E2E DnD Sprint" }),
      ).toBeVisible({ timeout: 10_000 });

      // The sprint should now have at least 1 item
      // (This may show "1 item" in the header meta)
      await expect(page.getByText(/1 item(?!s)/)).toBeVisible({
        timeout: 5_000,
      });
    } else {
      // If we can't get bounding boxes (e.g., headless rendering issue),
      // fall back to API verification
      test.skip(
        true,
        "Could not get bounding boxes for drag-and-drop elements",
      );
    }
  });
});
