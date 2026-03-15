import { expect, type APIRequestContext } from "@playwright/test";

const API_URL = process.env.API_URL ?? "http://localhost:5210/api/v1";

/**
 * Shared helpers for sprint E2E tests.
 * All helpers operate via API calls to set up test state.
 */

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
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

/** Register a fresh user and return auth tokens. */
export async function registerUser(
  request: APIRequestContext,
  suffix?: string,
): Promise<AuthTokens & { userId: string }> {
  const email = `e2e-sprint-${Date.now()}-${suffix ?? Math.random().toString(36).slice(2)}@teamflow.dev`;
  const response = await request.post(`${API_URL}/auth/register`, {
    data: { email, password: "Test@1234", name: `E2E Sprint ${suffix ?? "User"}` },
  });
  expect(response.status()).toBe(201);
  const body = await response.json();
  const payload = JSON.parse(atob(body.accessToken.split(".")[1]));
  return {
    accessToken: body.accessToken,
    refreshToken: body.refreshToken,
    userId: payload.sub,
  };
}

/** Create a project via API. Returns project ID and org ID. */
export async function createProject(
  request: APIRequestContext,
  token: string,
  name?: string,
): Promise<SeededProject> {
  // First get org ID from user's accessible orgs/projects
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
      // Create an org
      const createOrgRes = await request.post(`${API_URL}/organizations`, {
        headers: { Authorization: `Bearer ${token}` },
        data: { name: `E2E Org ${Date.now()}` },
      });
      expect(createOrgRes.status()).toBe(201);
      const orgData = await createOrgRes.json();
      orgId = orgData.id;
    }
  } else {
    // Create an org
    const createOrgRes = await request.post(`${API_URL}/organizations`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { name: `E2E Org ${Date.now()}` },
    });
    expect(createOrgRes.status()).toBe(201);
    const orgData = await createOrgRes.json();
    orgId = orgData.id;
  }

  const projectName = name ?? `E2E Sprint Project ${Date.now()}`;
  const response = await request.post(`${API_URL}/projects`, {
    headers: { Authorization: `Bearer ${token}` },
    data: { orgId, name: projectName },
  });
  expect(response.status()).toBe(201);
  const project = await response.json();
  return { id: project.id, orgId };
}

/** Create a sprint via API. */
export async function createSprint(
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
  expect(response.status()).toBe(201);
  const sprint = await response.json();
  return {
    id: sprint.id,
    projectId: sprint.projectId,
    name: sprint.name,
    status: sprint.status,
  };
}

/** Create a work item via API. */
export async function createWorkItem(
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
  expect(response.status()).toBe(201);
  const item = await response.json();

  // Set estimation value if provided
  if (data.estimationValue !== undefined) {
    const updateRes = await request.put(`${API_URL}/workitems/${item.id}`, {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        title: data.title,
        estimationValue: data.estimationValue,
      },
    });
    expect(updateRes.ok()).toBe(true);
  }

  return { id: item.id, projectId: item.projectId, title: item.title };
}

/** Add a work item to a sprint via API. */
export async function addItemToSprint(
  request: APIRequestContext,
  token: string,
  sprintId: string,
  workItemId: string,
): Promise<void> {
  const response = await request.post(
    `${API_URL}/sprints/${sprintId}/items/${workItemId}`,
    { headers: { Authorization: `Bearer ${token}` } },
  );
  expect(response.ok()).toBe(true);
}

/** Start a sprint via API. */
export async function startSprint(
  request: APIRequestContext,
  token: string,
  sprintId: string,
): Promise<void> {
  const response = await request.post(`${API_URL}/sprints/${sprintId}/start`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  expect(response.ok()).toBe(true);
}

/** Complete a sprint via API. */
export async function completeSprint(
  request: APIRequestContext,
  token: string,
  sprintId: string,
): Promise<void> {
  const response = await request.post(`${API_URL}/sprints/${sprintId}/complete`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  expect(response.ok()).toBe(true);
}

/** Create a release via API. */
export async function createRelease(
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
  expect(response.status()).toBe(201);
  const release = await response.json();
  return { id: release.id, name: release.name, status: release.status };
}

/**
 * Authenticate a page by setting auth tokens in localStorage.
 * Must be called before navigating to authenticated pages.
 */
export async function authenticatePage(
  page: import("@playwright/test").Page,
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
