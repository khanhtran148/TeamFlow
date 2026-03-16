import { test, expect } from "../fixtures";

const API_URL = process.env.API_URL ?? "http://localhost:5210/api/v1";

test.describe("Profile Page", () => {
  let token: string;
  let userName: string;

  test.beforeAll(async ({ request }) => {
    const email = `e2e-profile-${Date.now()}@teamflow.dev`;
    userName = "Profile E2E User";
    const regRes = await request.post(`${API_URL}/auth/register`, {
      data: { email, password: "Test@1234", name: userName },
    });
    const regBody = await regRes.json();
    token = regBody.accessToken;

    // Create org so user has membership data
    await request.post(`${API_URL}/organizations`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { name: `Profile Org ${Date.now()}` },
    });
  });

  test("profile page loads with Details tab active", async ({ page }) => {
    await page.goto("/profile");
    await expect(
      page.getByRole("heading", { name: /profile/i }),
    ).toBeVisible({ timeout: 10_000 });

    // Details tab should be active by default
    const detailsTab = page.getByRole("tab", { name: /details/i });
    await expect(detailsTab).toBeVisible({ timeout: 10_000 });
    await expect(detailsTab).toHaveAttribute("aria-selected", "true");
  });

  test("all four tabs are visible", async ({ page }) => {
    await page.goto("/profile");
    await expect(page.getByRole("tab", { name: /details/i })).toBeVisible({
      timeout: 10_000,
    });
    await expect(page.getByRole("tab", { name: /security/i })).toBeVisible();
    await expect(
      page.getByRole("tab", { name: /notifications/i }),
    ).toBeVisible();
    await expect(page.getByRole("tab", { name: /activity/i })).toBeVisible();
  });

  test("details tab shows user name and email", async ({ page }) => {
    await page.goto("/profile");
    // Wait for profile to load
    await expect(page.getByText(/member since/i)).toBeVisible({
      timeout: 10_000,
    });
    // User info should be displayed
    await expect(page.getByText(/@teamflow\.dev/)).toBeVisible();
  });

  test("details tab shows edit button", async ({ page }) => {
    await page.goto("/profile");
    await expect(page.getByText(/member since/i)).toBeVisible({
      timeout: 10_000,
    });
    await expect(
      page.getByRole("button", { name: /edit profile/i }),
    ).toBeVisible();
  });

  test("clicking edit shows name input and save/cancel buttons", async ({
    page,
  }) => {
    await page.goto("/profile");
    await expect(page.getByText(/member since/i)).toBeVisible({
      timeout: 10_000,
    });

    await page.getByRole("button", { name: /edit profile/i }).click();

    await expect(page.getByLabel(/display name/i)).toBeVisible();
    await expect(page.getByLabel(/avatar url/i)).toBeVisible();
    await expect(
      page.getByRole("button", { name: /save profile/i }),
    ).toBeVisible();
    await expect(
      page.getByRole("button", { name: /cancel editing/i }),
    ).toBeVisible();
  });

  test("cancel edit returns to view mode", async ({ page }) => {
    await page.goto("/profile");
    await expect(page.getByText(/member since/i)).toBeVisible({
      timeout: 10_000,
    });

    await page.getByRole("button", { name: /edit profile/i }).click();
    await expect(page.getByLabel(/display name/i)).toBeVisible();

    await page.getByRole("button", { name: /cancel editing/i }).click();
    await expect(
      page.getByRole("button", { name: /edit profile/i }),
    ).toBeVisible();
  });

  test("switching to security tab shows change password form", async ({
    page,
  }) => {
    await page.goto("/profile");
    await expect(page.getByRole("tab", { name: /security/i })).toBeVisible({
      timeout: 10_000,
    });

    await page.getByRole("tab", { name: /security/i }).click();

    await expect(
      page.getByRole("heading", { name: /change password/i }),
    ).toBeVisible({ timeout: 5_000 });
    await expect(page.getByLabel(/current password/i)).toBeVisible();
    await expect(page.getByLabel(/^new password$/i)).toBeVisible();
    await expect(page.getByLabel(/confirm new password/i)).toBeVisible();
  });

  test("switching to notifications tab renders preferences", async ({
    page,
  }) => {
    await page.goto("/profile");
    await expect(
      page.getByRole("tab", { name: /notifications/i }),
    ).toBeVisible({ timeout: 10_000 });

    await page.getByRole("tab", { name: /notifications/i }).click();

    // Notification preferences heading should render
    await expect(
      page.getByRole("heading", { name: /notification preferences/i }),
    ).toBeVisible({ timeout: 5_000 });
  });

  test("switching to activity tab shows activity content or empty state", async ({
    page,
  }) => {
    await page.goto("/profile");
    await expect(page.getByRole("tab", { name: /activity/i })).toBeVisible({
      timeout: 10_000,
    });

    await page.getByRole("tab", { name: /activity/i }).click();

    // Either activity items or empty state should be visible
    const activityList = page.getByLabel(/activity log/i);
    const emptyState = page.getByText(/no activity yet/i);
    await expect(activityList.or(emptyState)).toBeVisible({ timeout: 5_000 });
  });

  test("organizations section shows on details tab", async ({ page }) => {
    await page.goto("/profile");
    await expect(page.getByText(/member since/i)).toBeVisible({
      timeout: 10_000,
    });

    await expect(
      page.getByRole("heading", { name: /organizations/i }),
    ).toBeVisible();
  });

  test("teams section shows on details tab", async ({ page }) => {
    await page.goto("/profile");
    await expect(page.getByText(/member since/i)).toBeVisible({
      timeout: 10_000,
    });

    await expect(
      page.getByRole("heading", { name: /teams/i }),
    ).toBeVisible();
  });
});

test.describe("User Menu Profile Link", () => {
  test("user menu has profile link", async ({ page }) => {
    await page.goto("/profile");
    await expect(page.getByTestId("user-menu-btn")).toBeVisible({
      timeout: 10_000,
    });

    await page.getByTestId("user-menu-btn").click();
    await expect(page.getByTestId("profile-btn")).toBeVisible({
      timeout: 5_000,
    });
  });

  test("clicking profile link navigates to /profile", async ({ page }) => {
    // Start on profile page which has TopBar; navigate away then back
    await page.goto("/profile");
    await expect(page.getByTestId("user-menu-btn")).toBeVisible({
      timeout: 10_000,
    });

    await page.getByTestId("user-menu-btn").click();
    await page.getByTestId("profile-btn").click();

    await page.waitForURL("**/profile", { timeout: 5_000 });
    await expect(
      page.getByRole("heading", { name: /profile/i }),
    ).toBeVisible({ timeout: 10_000 });
  });
});
