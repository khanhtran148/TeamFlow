import { test, expect } from "../fixtures";
import type { Page } from "@playwright/test";

const API_URL = process.env.API_URL ?? "http://localhost:5210/api/v1";

/**
 * Permission-based UI tests for sprint management.
 *
 * Verifies that sprint management buttons (New Sprint, Start Sprint,
 * Complete Sprint, Edit, Capacity, Delete) are shown/hidden based
 * on the user's role in the project.
 *
 * Roles tested:
 * - Viewer: no sprint management buttons visible
 * - Developer: same restrictions as Viewer for sprint management
 * - TeamManager: all sprint management buttons visible
 */
test.describe("Sprint Permission-Based UI", () => {
  let ownerToken: string;
  let projectId: string;
  let orgId: string;
  let planningSprintId: string;
  let activeSprintId: string;

  // Role-specific users
  let viewerToken: string;
  let viewerUserId: string;
  let developerToken: string;
  let developerUserId: string;
  let teamManagerToken: string;
  let teamManagerUserId: string;

  test.beforeAll(async ({ request }) => {
    // Register owner (OrgAdmin) who creates the project
    const ownerEmail = `e2e-${Date.now()}-perm-owner@teamflow.dev`;
    const ownerRes = await request.post(`${API_URL}/auth/register`, {
      data: { email: ownerEmail, password: "Test@1234", name: "E2E Owner" },
    });
    expect(ownerRes.status()).toBe(201);
    const ownerBody = await ownerRes.json();
    ownerToken = ownerBody.accessToken;

    // Create org + project
    const orgRes = await request.get(`${API_URL}/organizations`, {
      headers: { Authorization: `Bearer ${ownerToken}` },
    });
    if (orgRes.ok()) {
      const orgs = await orgRes.json();
      const items = Array.isArray(orgs) ? orgs : orgs.items ?? [];
      if (items.length > 0) {
        orgId = items[0].id;
      } else {
        const createOrgRes = await request.post(`${API_URL}/organizations`, {
          headers: { Authorization: `Bearer ${ownerToken}` },
          data: { name: `E2E Perm Org ${Date.now()}` },
        });
        orgId = (await createOrgRes.json()).id;
      }
    } else {
      const createOrgRes = await request.post(`${API_URL}/organizations`, {
        headers: { Authorization: `Bearer ${ownerToken}` },
        data: { name: `E2E Perm Org ${Date.now()}` },
      });
      orgId = (await createOrgRes.json()).id;
    }

    const projRes = await request.post(`${API_URL}/projects`, {
      headers: { Authorization: `Bearer ${ownerToken}` },
      data: { orgId, name: "Permission Test Project" },
    });
    const project = await projRes.json();
    projectId = project.id;

    // Create a Planning sprint
    const planningSprintRes = await request.post(`${API_URL}/sprints`, {
      headers: { Authorization: `Bearer ${ownerToken}` },
      data: {
        projectId,
        name: "Perm Planning Sprint",
        startDate: new Date().toISOString().split("T")[0],
        endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
          .toISOString()
          .split("T")[0],
      },
    });
    const planningSprint = await planningSprintRes.json();
    planningSprintId = planningSprint.id;

    // Add an item so start button can be active
    const itemRes = await request.post(`${API_URL}/workitems`, {
      headers: { Authorization: `Bearer ${ownerToken}` },
      data: {
        projectId,
        type: "Task",
        title: "Perm Test Item 1",
        priority: "Medium",
      },
    });
    const item1 = await itemRes.json();
    await request.post(`${API_URL}/sprints/${planningSprintId}/items/${item1.id}`, {
      headers: { Authorization: `Bearer ${ownerToken}` },
    });

    // Create an Active sprint
    const activeSprintRes = await request.post(`${API_URL}/sprints`, {
      headers: { Authorization: `Bearer ${ownerToken}` },
      data: {
        projectId,
        name: "Perm Active Sprint",
        startDate: new Date().toISOString().split("T")[0],
        endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000)
          .toISOString()
          .split("T")[0],
      },
    });
    const activeSprint = await activeSprintRes.json();
    activeSprintId = activeSprint.id;

    const item2Res = await request.post(`${API_URL}/workitems`, {
      headers: { Authorization: `Bearer ${ownerToken}` },
      data: {
        projectId,
        type: "Task",
        title: "Perm Test Item 2",
        priority: "Medium",
      },
    });
    const item2 = await item2Res.json();
    await request.post(`${API_URL}/sprints/${activeSprintId}/items/${item2.id}`, {
      headers: { Authorization: `Bearer ${ownerToken}` },
    });
    await request.post(`${API_URL}/sprints/${activeSprintId}/start`, {
      headers: { Authorization: `Bearer ${ownerToken}` },
    });

    // Register role-specific users and add them to the project
    // Viewer
    const viewerEmail = `e2e-${Date.now()}-viewer@teamflow.dev`;
    const viewerRes = await request.post(`${API_URL}/auth/register`, {
      data: { email: viewerEmail, password: "Test@1234", name: "E2E Viewer" },
    });
    expect(viewerRes.status()).toBe(201);
    const viewerBody = await viewerRes.json();
    viewerToken = viewerBody.accessToken;
    const viewerPayload = JSON.parse(atob(viewerBody.accessToken.split(".")[1]));
    viewerUserId = viewerPayload.sub;

    // Developer
    const devEmail = `e2e-${Date.now()}-developer@teamflow.dev`;
    const devRes = await request.post(`${API_URL}/auth/register`, {
      data: { email: devEmail, password: "Test@1234", name: "E2E Developer" },
    });
    expect(devRes.status()).toBe(201);
    const devBody = await devRes.json();
    developerToken = devBody.accessToken;
    const devPayload = JSON.parse(atob(devBody.accessToken.split(".")[1]));
    developerUserId = devPayload.sub;

    // TeamManager
    const tmEmail = `e2e-${Date.now()}-teammanager@teamflow.dev`;
    const tmRes = await request.post(`${API_URL}/auth/register`, {
      data: { email: tmEmail, password: "Test@1234", name: "E2E TeamManager" },
    });
    expect(tmRes.status()).toBe(201);
    const tmBody = await tmRes.json();
    teamManagerToken = tmBody.accessToken;
    const tmPayload = JSON.parse(atob(tmBody.accessToken.split(".")[1]));
    teamManagerUserId = tmPayload.sub;

    // Add members to project with specific roles
    await request.post(`${API_URL}/projects/${projectId}/memberships`, {
      headers: { Authorization: `Bearer ${ownerToken}` },
      data: { memberId: viewerUserId, memberType: "User", role: "Viewer" },
    });

    await request.post(`${API_URL}/projects/${projectId}/memberships`, {
      headers: { Authorization: `Bearer ${ownerToken}` },
      data: { memberId: developerUserId, memberType: "User", role: "Developer" },
    });

    await request.post(`${API_URL}/projects/${projectId}/memberships`, {
      headers: { Authorization: `Bearer ${ownerToken}` },
      data: { memberId: teamManagerUserId, memberType: "User", role: "TeamManager" },
    });
  });

  test.afterAll(async ({ request }) => {
    if (ownerToken && projectId) {
      try {
        await request.delete(`${API_URL}/projects/${projectId}`, {
          headers: { Authorization: `Bearer ${ownerToken}` },
        });
      } catch {
        // Best-effort cleanup
      }
    }
  });

  async function authenticateAs(
    page: Page,
    token: string,
  ): Promise<void> {
    await page.goto("/login");
    await page.evaluate(
      ({ accessToken }: { accessToken: string }) => {
        localStorage.setItem(
          "teamflow-auth",
          JSON.stringify({
            state: {
              accessToken,
              refreshToken: "",
              isAuthenticated: true,
            },
            version: 0,
          }),
        );
      },
      { accessToken: token },
    );
  }

  // ---- Viewer Role Tests ----

  test.describe("Viewer role", () => {
    test("sprint list: 'New Sprint' button is hidden", async ({ page }) => {
      await authenticateAs(page, viewerToken);
      await page.goto(`/projects/${projectId}/sprints`);

      await expect(page.getByRole("heading", { name: "Sprints" })).toBeVisible({
        timeout: 10_000,
      });

      // "New Sprint" button should NOT be visible for Viewer
      await expect(
        page.getByRole("button", { name: /new sprint/i }),
      ).not.toBeVisible();
    });

    test("planning sprint detail: start/edit/delete buttons are hidden", async ({ page }) => {
      await authenticateAs(page, viewerToken);
      await page.goto(`/projects/${projectId}/sprints/${planningSprintId}`);

      await expect(
        page.getByRole("heading", { name: "Perm Planning Sprint" }),
      ).toBeVisible({ timeout: 10_000 });

      // Sprint management buttons should NOT be visible
      await expect(
        page.getByRole("button", { name: /start sprint/i }),
      ).not.toBeVisible();
      await expect(
        page.getByRole("button", { name: /edit sprint/i }),
      ).not.toBeVisible();
      await expect(
        page.getByRole("button", { name: /capacity/i }),
      ).not.toBeVisible();
      await expect(
        page.getByRole("button", { name: /delete/i }),
      ).not.toBeVisible();
    });

    test("active sprint detail: complete button is hidden", async ({ page }) => {
      await authenticateAs(page, viewerToken);
      await page.goto(`/projects/${projectId}/sprints/${activeSprintId}`);

      await expect(
        page.getByRole("heading", { name: "Perm Active Sprint" }),
      ).toBeVisible({ timeout: 10_000 });

      await expect(
        page.getByRole("button", { name: /complete sprint/i }),
      ).not.toBeVisible();
    });
  });

  // ---- Developer Role Tests ----

  test.describe("Developer role", () => {
    test("sprint list: 'New Sprint' button is hidden", async ({ page }) => {
      await authenticateAs(page, developerToken);
      await page.goto(`/projects/${projectId}/sprints`);

      await expect(page.getByRole("heading", { name: "Sprints" })).toBeVisible({
        timeout: 10_000,
      });

      // Developer should also NOT see the New Sprint button
      // (same restrictions as Viewer for sprint management)
      await expect(
        page.getByRole("button", { name: /new sprint/i }),
      ).not.toBeVisible();
    });

    test("planning sprint detail: start/edit/delete buttons are hidden", async ({ page }) => {
      await authenticateAs(page, developerToken);
      await page.goto(`/projects/${projectId}/sprints/${planningSprintId}`);

      await expect(
        page.getByRole("heading", { name: "Perm Planning Sprint" }),
      ).toBeVisible({ timeout: 10_000 });

      await expect(
        page.getByRole("button", { name: /start sprint/i }),
      ).not.toBeVisible();
      await expect(
        page.getByRole("button", { name: /edit sprint/i }),
      ).not.toBeVisible();
      await expect(
        page.getByRole("button", { name: /capacity/i }),
      ).not.toBeVisible();
      await expect(
        page.getByRole("button", { name: /delete/i }),
      ).not.toBeVisible();
    });

    test("active sprint detail: complete button is hidden", async ({ page }) => {
      await authenticateAs(page, developerToken);
      await page.goto(`/projects/${projectId}/sprints/${activeSprintId}`);

      await expect(
        page.getByRole("heading", { name: "Perm Active Sprint" }),
      ).toBeVisible({ timeout: 10_000 });

      await expect(
        page.getByRole("button", { name: /complete sprint/i }),
      ).not.toBeVisible();
    });
  });

  // ---- TeamManager Role Tests ----

  test.describe("TeamManager role", () => {
    test("sprint list: 'New Sprint' button is visible", async ({ page }) => {
      await authenticateAs(page, teamManagerToken);
      await page.goto(`/projects/${projectId}/sprints`);

      await expect(page.getByRole("heading", { name: "Sprints" })).toBeVisible({
        timeout: 10_000,
      });

      // TeamManager should see the New Sprint button
      await expect(
        page.getByRole("button", { name: /new sprint/i }),
      ).toBeVisible();
    });

    test("planning sprint detail: all management buttons are visible", async ({ page }) => {
      await authenticateAs(page, teamManagerToken);
      await page.goto(`/projects/${projectId}/sprints/${planningSprintId}`);

      await expect(
        page.getByRole("heading", { name: "Perm Planning Sprint" }),
      ).toBeVisible({ timeout: 10_000 });

      // All sprint management buttons should be visible
      await expect(
        page.getByRole("button", { name: /start sprint/i }),
      ).toBeVisible();
      await expect(
        page.getByRole("button", { name: /edit sprint/i }),
      ).toBeVisible();
      await expect(
        page.getByRole("button", { name: /capacity/i }),
      ).toBeVisible();
      await expect(
        page.getByRole("button", { name: /delete/i }),
      ).toBeVisible();
    });

    test("active sprint detail: complete button is visible", async ({ page }) => {
      await authenticateAs(page, teamManagerToken);
      await page.goto(`/projects/${projectId}/sprints/${activeSprintId}`);

      await expect(
        page.getByRole("heading", { name: "Perm Active Sprint" }),
      ).toBeVisible({ timeout: 10_000 });

      // Complete sprint button should be visible for TeamManager
      await expect(
        page.getByRole("button", { name: /complete sprint/i }),
      ).toBeVisible();
    });
  });
});
