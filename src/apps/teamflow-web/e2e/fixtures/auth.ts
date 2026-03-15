import { test as base, expect, type Page } from "@playwright/test";
import fs from "node:fs";
import path from "node:path";

const API_URL = process.env.API_URL ?? "http://localhost:5210/api/v1";
const AUTH_STATE_PATH = path.join(__dirname, "..", "..", ".auth", "user.json");

export interface TestUser {
  email: string;
  password: string;
  name: string;
  accessToken?: string;
  refreshToken?: string;
}

/**
 * Check if storageState from global setup exists.
 * When present, tests already start with an authenticated context.
 */
function hasGlobalStorageState(): boolean {
  return fs.existsSync(AUTH_STATE_PATH);
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
 * storageState-aware: if global setup has run, tests begin authenticated.
 */
export const test = base.extend<{
  apiUrl: string;
  testUser: TestUser;
  hasStorageState: boolean;
  registerTestUser: (user: TestUser) => Promise<TestUser>;
  loginTestUser: (
    email: string,
    password: string
  ) => Promise<{ accessToken: string; refreshToken: string }>;
}>({
  apiUrl: API_URL,

  hasStorageState: [
    async ({}, use) => {
      await use(hasGlobalStorageState());
    },
    { scope: "test" },
  ],

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
