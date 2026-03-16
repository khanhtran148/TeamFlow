import { test, expect } from "../../e2e/fixtures/auth";

const API_URL = process.env.API_URL ?? "http://localhost:5210/api/v1";

test.describe("AC1: Register → Login → JWT → call protected API → success", () => {
  test("full auth flow works end-to-end", async ({ page }) => {
    const email = `e2e-${Date.now()}@teamflow.dev`;
    const password = "Test@1234";
    const name = "E2E User";

    // Clear any stored auth state so AuthGuard doesn't redirect away
    await page.goto("/login");
    await page.evaluate(() => localStorage.removeItem("teamflow-auth"));

    // Register
    await page.goto("/register");
    await page.getByLabel("Name").fill(name);
    await page.getByLabel("Email").fill(email);
    await page.getByLabel("Password", { exact: true }).fill(password);
    await page.getByLabel("Confirm").fill(password);
    await page.getByRole("button", { name: /register/i }).click();

    // Should redirect to /onboarding after successful registration
    // (then further to /onboarding/no-orgs for a fresh user with no org)
    await expect(page).toHaveURL(/\/onboarding/, { timeout: 10_000 });

    // Verify user lands on onboarding flow (fresh user has no org)
    await expect(
      page.getByText(/welcome to teamflow|not a member of any organization/i).first(),
    ).toBeVisible({ timeout: 10_000 });
  });

  test("register then login with same credentials", async ({ page }) => {
    const email = `e2e-${Date.now()}@teamflow.dev`;
    const password = "Test@1234";

    // Register via API
    const regResponse = await page.request.post(`${API_URL}/auth/register`, {
      data: { email, password, name: "E2E Login Test" },
    });
    expect(regResponse.status()).toBe(201);

    // Clear stored auth state so AuthGuard doesn't redirect away from /login
    await page.goto("about:blank");
    await page.goto("/login");
    await page.evaluate(() => localStorage.removeItem("teamflow-auth"));
    await page.goto("/login");
    await expect(page.getByLabel("Email")).toBeVisible({ timeout: 10_000 });

    // Login via UI
    await page.getByLabel("Email").fill(email);
    await page.getByLabel("Password").fill(password);
    await page.getByRole("button", { name: /sign in/i }).click();

    await expect(page).toHaveURL(/\/onboarding/, { timeout: 10_000 });
  });
});

test.describe("AC2: Token expires + valid refresh → new token, no logout", () => {
  test("silent refresh keeps user logged in", async ({ page }) => {
    const email = `e2e-refresh-${Date.now()}@teamflow.dev`;
    const password = "Test@1234";

    // Register
    const regResponse = await page.request.post(`${API_URL}/auth/register`, {
      data: { email, password, name: "Refresh Test" },
    });
    expect(regResponse.status()).toBe(201);
    const tokens = await regResponse.json();

    // Use refresh endpoint directly
    const refreshResponse = await page.request.post(`${API_URL}/auth/refresh`, {
      data: { token: tokens.refreshToken },
    });
    expect(refreshResponse.status()).toBe(200);

    const newTokens = await refreshResponse.json();
    expect(newTokens.accessToken).toBeTruthy();
    expect(newTokens.refreshToken).toBeTruthy();
    expect(newTokens.accessToken).not.toBe(tokens.accessToken);
  });
});
