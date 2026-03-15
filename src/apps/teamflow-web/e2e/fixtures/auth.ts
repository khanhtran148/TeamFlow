import { test as base, expect, type Page } from "@playwright/test";

const API_URL = process.env.API_URL ?? "http://localhost:5210/api/v1";

export interface TestUser {
  email: string;
  password: string;
  name: string;
  accessToken?: string;
  refreshToken?: string;
}

/**
 * Register a new user via the API and return tokens.
 */
async function registerUser(
  page: Page,
  user: TestUser
): Promise<TestUser & { accessToken: string; refreshToken: string }> {
  const response = await page.request.post(`${API_URL}/auth/register`, {
    data: {
      email: user.email,
      password: user.password,
      name: user.name,
    },
  });

  expect(response.status()).toBe(201);
  const body = await response.json();

  return {
    ...user,
    accessToken: body.accessToken,
    refreshToken: body.refreshToken,
  };
}

/**
 * Login an existing user via the API and return tokens.
 */
async function loginUser(
  page: Page,
  email: string,
  password: string
): Promise<{ accessToken: string; refreshToken: string }> {
  const response = await page.request.post(`${API_URL}/auth/login`, {
    data: { email, password },
  });

  expect(response.status()).toBe(200);
  return response.json();
}

/**
 * Extended test fixture providing authenticated users and API helpers.
 */
export const test = base.extend<{
  apiUrl: string;
  testUser: TestUser;
  registerTestUser: (user: TestUser) => Promise<TestUser>;
  loginTestUser: (
    email: string,
    password: string
  ) => Promise<{ accessToken: string; refreshToken: string }>;
}>({
  apiUrl: API_URL,

  testUser: {
    email: `e2e-${Date.now()}@teamflow.dev`,
    password: "Test@1234",
    name: "E2E Test User",
  },

  registerTestUser: async ({ page }, use) => {
    await use((user) => registerUser(page, user));
  },

  loginTestUser: async ({ page }, use) => {
    await use((email, password) => loginUser(page, email, password));
  },
});

export { expect } from "@playwright/test";
