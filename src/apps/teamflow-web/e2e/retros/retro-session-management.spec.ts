import { test, expect } from "../fixtures";

const API_URL = process.env.API_URL ?? "http://localhost:5210/api/v1";

test.describe("Retro Session Management", () => {
  let projectId: string;
  let token: string;

  test.beforeAll(async ({ request }) => {
    const email = `e2e-retromgmt-${Date.now()}@teamflow.dev`;
    const regRes = await request.post(`${API_URL}/auth/register`, {
      data: { email, password: "Test@1234", name: "RetroMgmt E2E" },
    });
    const regBody = await regRes.json();
    token = regBody.accessToken;

    const orgRes = await request.post(`${API_URL}/organizations`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { name: `RetroMgmt Org ${Date.now()}` },
    });
    const org = await orgRes.json();

    const projRes = await request.post(`${API_URL}/projects`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { orgId: org.id, name: `RetroMgmt Project ${Date.now()}` },
    });
    const proj = await projRes.json();
    projectId = proj.id;
  });

  test("retros page shows the Retrospectives heading", async ({ page }) => {
    await page.goto(`/projects/${projectId}/retros`);
    await expect(page.getByText("Retrospectives")).toBeVisible({
      timeout: 10_000,
    });
  });

  test("New Retro button is visible", async ({ page }) => {
    await page.goto(`/projects/${projectId}/retros`);
    await expect(
      page.getByRole("button", { name: /new retro/i }),
    ).toBeVisible({ timeout: 10_000 });
  });

  test("clicking New Retro opens the create dialog", async ({ page }) => {
    await page.goto(`/projects/${projectId}/retros`);
    const newRetroBtn = page.getByRole("button", { name: /new retro/i });
    await expect(newRetroBtn).toBeVisible({ timeout: 10_000 });

    await newRetroBtn.click();

    // Dialog should show the "New Retrospective" heading
    await expect(page.getByText("New Retrospective")).toBeVisible({
      timeout: 5_000,
    });
    // And the session name input
    await expect(page.getByLabel(/session name/i)).toBeVisible({
      timeout: 5_000,
    });
    // And the two mode buttons
    await expect(page.getByRole("button", { name: "Public" })).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Anonymous" }),
    ).toBeVisible();
  });

  test("can create a retro session with a name and it appears in the list", async ({
    page,
  }) => {
    const sessionName = `Sprint ${Date.now()} Retro`;

    await page.goto(`/projects/${projectId}/retros`);
    const newRetroBtn = page.getByRole("button", { name: /new retro/i });
    await expect(newRetroBtn).toBeVisible({ timeout: 10_000 });

    await newRetroBtn.click();
    await expect(page.getByText("New Retrospective")).toBeVisible({
      timeout: 5_000,
    });

    // Fill in the session name
    await page.getByLabel(/session name/i).fill(sessionName);

    // Click Public mode to create
    await page.getByRole("button", { name: "Public" }).click();

    // Wait for the dialog to close and the session to appear in the list
    await expect(page.getByText(sessionName)).toBeVisible({
      timeout: 10_000,
    });
  });

  test("newly created session shows Draft status", async ({ page }) => {
    const sessionName = `Draft Check ${Date.now()}`;

    await page.goto(`/projects/${projectId}/retros`);
    const newRetroBtn = page.getByRole("button", { name: /new retro/i });
    await expect(newRetroBtn).toBeVisible({ timeout: 10_000 });

    await newRetroBtn.click();
    await page.getByLabel(/session name/i).fill(sessionName);
    await page.getByRole("button", { name: "Public" }).click();

    // Session should appear with Draft status badge
    await expect(page.getByText(sessionName)).toBeVisible({
      timeout: 10_000,
    });
    await expect(page.getByText("Draft").first()).toBeVisible({
      timeout: 5_000,
    });
  });

  test("empty state is shown when no sessions exist", async ({ page }) => {
    // Create a new project with no retro sessions for isolation
    const email = `e2e-retroempty-${Date.now()}@teamflow.dev`;
    const regRes = await page.request.post(`${API_URL}/auth/register`, {
      data: { email, password: "Test@1234", name: "RetroEmpty E2E" },
    });
    const regBody = await regRes.json();
    const freshToken = regBody.accessToken;

    const orgRes = await page.request.post(`${API_URL}/organizations`, {
      headers: { Authorization: `Bearer ${freshToken}` },
      data: { name: `RetroEmpty Org ${Date.now()}` },
    });
    const org = await orgRes.json();

    const projRes = await page.request.post(`${API_URL}/projects`, {
      headers: { Authorization: `Bearer ${freshToken}` },
      data: { orgId: org.id, name: `RetroEmpty Project ${Date.now()}` },
    });
    const proj = await projRes.json();

    // Inject auth tokens for the fresh user
    await page.goto("/login");
    await page.evaluate(
      ({ accessToken, refreshToken }) => {
        localStorage.setItem(
          "teamflow-auth",
          JSON.stringify({
            state: { accessToken, refreshToken, isAuthenticated: true },
            version: 0,
          }),
        );
      },
      {
        accessToken: freshToken,
        refreshToken: regBody.refreshToken,
      },
    );

    await page.goto(`/projects/${proj.id}/retros`);
    await expect(
      page.getByText(/no retrospective sessions yet/i),
    ).toBeVisible({ timeout: 10_000 });
  });
});
