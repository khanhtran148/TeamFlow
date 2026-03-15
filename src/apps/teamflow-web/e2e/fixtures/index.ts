import { test as base, expect } from "@playwright/test";
import fs from "node:fs";
import path from "node:path";
import type { APIRequestContext, Page } from "@playwright/test";

const API_URL = process.env.API_URL ?? "http://localhost:5210/api/v1";
const AUTH_STATE_PATH = path.join(__dirname, "..", "..", ".auth", "user.json");

// ---- Interfaces ----

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
}

export interface TestUser {
  email: string;
  password: string;
  name: string;
  accessToken: string;
  refreshToken: string;
  userId: string;
}

export interface SeededProject {
  id: string;
  orgId: string;
}

export interface SeededSprint {
  id: string;
  projectId: string;
  name: string;
  status: string;
}

export interface SeededWorkItem {
  id: string;
  projectId: string;
  title: string;
}

// ---- Sprint helper functions ----

/** Register a fresh user and return auth tokens. */
async function registerUserViaApi(
  request: APIRequestContext,
  suffix?: string,
): Promise<TestUser> {
  const email = `e2e-${Date.now()}-${suffix ?? Math.random().toString(36).slice(2)}@teamflow.dev`;
  const password = "Test@1234";
  const name = `E2E ${suffix ?? "User"}`;
  const response = await request.post(`${API_URL}/auth/register`, {
    data: { email, password, name },
  });
  if (response.status() !== 201) {
    throw new Error(`Registration failed: ${response.status()} ${await response.text()}`);
  }
  const body = await response.json();
  const payload = JSON.parse(atob(body.accessToken.split(".")[1]));
  return {
    email,
    password,
    name,
    accessToken: body.accessToken,
    refreshToken: body.refreshToken,
    userId: payload.sub,
  };
}

/** Create a project via API. */
async function createProjectViaApi(
  request: APIRequestContext,
  token: string,
  name?: string,
): Promise<SeededProject> {
  const orgResponse = await request.get(`${API_URL}/organizations`, {
    headers: { Authorization: `Bearer ${token}` },
  });

  let orgId: string;
  if (orgResponse.ok()) {
    const orgs = await orgResponse.json();
    const items = Array.isArray(orgs) ? orgs : orgs.items ?? [];
    if (items.length > 0) {
      orgId = items[0].id;
    } else {
      const createOrgRes = await request.post(`${API_URL}/organizations`, {
        headers: { Authorization: `Bearer ${token}` },
        data: { name: `E2E Org ${Date.now()}` },
      });
      const orgData = await createOrgRes.json();
      orgId = orgData.id;
    }
  } else {
    const createOrgRes = await request.post(`${API_URL}/organizations`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { name: `E2E Org ${Date.now()}` },
    });
    const orgData = await createOrgRes.json();
    orgId = orgData.id;
  }

  const projectName = name ?? `E2E Project ${Date.now()}`;
  const response = await request.post(`${API_URL}/projects`, {
    headers: { Authorization: `Bearer ${token}` },
    data: { orgId, name: projectName },
  });
  const project = await response.json();
  return { id: project.id, orgId };
}

/** Create a sprint via API. */
async function createSprintViaApi(
  request: APIRequestContext,
  token: string,
  data: {
    projectId: string;
    name: string;
    goal?: string;
    startDate?: string;
    endDate?: string;
  },
): Promise<SeededSprint> {
  const response = await request.post(`${API_URL}/sprints`, {
    headers: { Authorization: `Bearer ${token}` },
    data,
  });
  const sprint = await response.json();
  return {
    id: sprint.id,
    projectId: sprint.projectId,
    name: sprint.name,
    status: sprint.status,
  };
}

/** Create a work item via API. */
async function createWorkItemViaApi(
  request: APIRequestContext,
  token: string,
  data: {
    projectId: string;
    title: string;
    type?: string;
    priority?: string;
    estimationValue?: number;
  },
): Promise<SeededWorkItem> {
  const response = await request.post(`${API_URL}/workitems`, {
    headers: { Authorization: `Bearer ${token}` },
    data: {
      projectId: data.projectId,
      type: data.type ?? "Task",
      title: data.title,
      priority: data.priority ?? "Medium",
    },
  });
  const item = await response.json();

  if (data.estimationValue !== undefined) {
    await request.put(`${API_URL}/workitems/${item.id}`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { title: data.title, estimationValue: data.estimationValue },
    });
  }

  return { id: item.id, projectId: item.projectId, title: item.title };
}

/** Add a work item to a sprint via API. */
async function addItemToSprintViaApi(
  request: APIRequestContext,
  token: string,
  sprintId: string,
  workItemId: string,
): Promise<void> {
  await request.post(`${API_URL}/sprints/${sprintId}/items/${workItemId}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
}

/** Start a sprint via API. */
async function startSprintViaApi(
  request: APIRequestContext,
  token: string,
  sprintId: string,
): Promise<void> {
  await request.post(`${API_URL}/sprints/${sprintId}/start`, {
    headers: { Authorization: `Bearer ${token}` },
  });
}

/** Complete a sprint via API. */
async function completeSprintViaApi(
  request: APIRequestContext,
  token: string,
  sprintId: string,
): Promise<void> {
  await request.post(`${API_URL}/sprints/${sprintId}/complete`, {
    headers: { Authorization: `Bearer ${token}` },
  });
}

/** Create a release via API. */
async function createReleaseViaApi(
  request: APIRequestContext,
  token: string,
  data: {
    projectId: string;
    name: string;
    description?: string;
    releaseDate?: string;
  },
): Promise<{ id: string; name: string; status: string }> {
  const response = await request.post(`${API_URL}/releases`, {
    headers: { Authorization: `Bearer ${token}` },
    data,
  });
  const release = await response.json();
  return { id: release.id, name: release.name, status: release.status };
}

/**
 * Authenticate a page by setting auth tokens in localStorage.
 * If storageState exists from the global setup, this is a fallback
 * for tests that need a different user context.
 */
async function authenticatePageWithTokens(
  page: Page,
  tokens: AuthTokens,
): Promise<void> {
  await page.goto("/login");
  await page.evaluate(
    ({ accessToken, refreshToken }) => {
      localStorage.setItem(
        "teamflow-auth",
        JSON.stringify({
          state: {
            accessToken,
            refreshToken,
            isAuthenticated: true,
          },
          version: 0,
        }),
      );
    },
    { accessToken: tokens.accessToken, refreshToken: tokens.refreshToken },
  );
}

/** Best-effort cleanup: delete a project via API. */
async function deleteProjectViaApi(
  request: APIRequestContext,
  token: string,
  projectId: string,
): Promise<void> {
  try {
    await request.delete(`${API_URL}/projects/${projectId}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
  } catch {
    // Best-effort cleanup — do not fail tests
  }
}

// ---- Fixture types ----

interface SprintHelpers {
  registerUser: (suffix?: string) => Promise<TestUser>;
  createProject: (token: string, name?: string) => Promise<SeededProject>;
  createSprint: (
    token: string,
    data: {
      projectId: string;
      name: string;
      goal?: string;
      startDate?: string;
      endDate?: string;
    },
  ) => Promise<SeededSprint>;
  createWorkItem: (
    token: string,
    data: {
      projectId: string;
      title: string;
      type?: string;
      priority?: string;
      estimationValue?: number;
    },
  ) => Promise<SeededWorkItem>;
  addItemToSprint: (
    token: string,
    sprintId: string,
    workItemId: string,
  ) => Promise<void>;
  startSprint: (token: string, sprintId: string) => Promise<void>;
  completeSprint: (token: string, sprintId: string) => Promise<void>;
  createRelease: (
    token: string,
    data: {
      projectId: string;
      name: string;
      description?: string;
      releaseDate?: string;
    },
  ) => Promise<{ id: string; name: string; status: string }>;
  authenticatePage: (page: Page, tokens: AuthTokens) => Promise<void>;
  deleteProject: (token: string, projectId: string) => Promise<void>;
}

interface AuthFixtures {
  apiUrl: string;
  hasStorageState: boolean;
}

// ---- Unified test fixture ----

export const test = base.extend<AuthFixtures & { sprintHelpers: SprintHelpers }>({
  apiUrl: API_URL,

  hasStorageState: [
    async ({}, use) => {
      const exists = fs.existsSync(AUTH_STATE_PATH);
      await use(exists);
    },
    { scope: "test" },
  ],

  sprintHelpers: async ({ request }, use) => {
    await use({
      registerUser: (suffix?) => registerUserViaApi(request, suffix),
      createProject: (token, name?) => createProjectViaApi(request, token, name),
      createSprint: (token, data) => createSprintViaApi(request, token, data),
      createWorkItem: (token, data) => createWorkItemViaApi(request, token, data),
      addItemToSprint: (token, sprintId, workItemId) =>
        addItemToSprintViaApi(request, token, sprintId, workItemId),
      startSprint: (token, sprintId) => startSprintViaApi(request, token, sprintId),
      completeSprint: (token, sprintId) => completeSprintViaApi(request, token, sprintId),
      createRelease: (token, data) => createReleaseViaApi(request, token, data),
      authenticatePage: (page, tokens) => authenticatePageWithTokens(page, tokens),
      deleteProject: (token, projectId) => deleteProjectViaApi(request, token, projectId),
    });
  },
});

export { expect } from "@playwright/test";

// Re-export legacy functions for backward compatibility during migration
export {
  registerUserViaApi as registerUser,
  createProjectViaApi as createProject,
  createSprintViaApi as createSprint,
  createWorkItemViaApi as createWorkItem,
  addItemToSprintViaApi as addItemToSprint,
  startSprintViaApi as startSprint,
  completeSprintViaApi as completeSprint,
  createReleaseViaApi as createRelease,
  authenticatePageWithTokens as authenticatePage,
  deleteProjectViaApi as deleteProject,
};
