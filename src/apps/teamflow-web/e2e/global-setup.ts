import { test as setup, expect } from "@playwright/test";
import path from "node:path";
import fs from "node:fs";

const API_URL = process.env.API_URL ?? "http://localhost:5210/api/v1";
const AUTH_DIR = path.join(__dirname, "..", ".auth");
const AUTH_STATE_PATH = path.join(AUTH_DIR, "user.json");

/**
 * Global setup project: registers a shared test user, logs in via API,
 * and saves storageState so all "chromium" tests start authenticated.
 */
setup("authenticate", async ({ page }) => {
  // Ensure .auth directory exists
  if (!fs.existsSync(AUTH_DIR)) {
    fs.mkdirSync(AUTH_DIR, { recursive: true });
  }

  const email = `e2e-global-${Date.now()}@teamflow.dev`;
  const password = "Test@1234";
  const name = "E2E Global User";

  // Register a test user via API
  const registerResponse = await page.request.post(`${API_URL}/auth/register`, {
    data: { email, password, name },
  });
  expect(registerResponse.status()).toBe(201);
  const tokens = await registerResponse.json();

  // Navigate to the app and inject auth tokens into localStorage
  // so that storageState captures them for downstream tests.
  await page.goto("/login");
  await page.evaluate(
    ({ accessToken, refreshToken, userEmail, userName }) => {
      const payload = JSON.parse(atob(accessToken.split(".")[1]));
      localStorage.setItem(
        "teamflow-auth",
        JSON.stringify({
          state: {
            user: { id: payload.sub, email: userEmail, name: userName },
            accessToken,
            refreshToken,
            expiresAt: new Date(Date.now() + 3600000).toISOString(),
            isAuthenticated: true,
          },
          version: 0,
        }),
      );
    },
    {
      accessToken: tokens.accessToken,
      refreshToken: tokens.refreshToken,
      userEmail: email,
      userName: name,
    },
  );

  // Save browser storage state (cookies + localStorage) for downstream tests
  await page.context().storageState({ path: AUTH_STATE_PATH });
});
