import axios, {
  type AxiosError,
  type InternalAxiosRequestConfig,
} from "axios";
import type { ProblemDetails } from "./types";

// ---- API Error class ----

export class ApiError extends Error {
  public readonly status: number;
  public readonly problem: ProblemDetails;

  constructor(problem: ProblemDetails) {
    super(ApiError.formatMessage(problem));
    this.name = "ApiError";
    this.status = problem.status;
    this.problem = problem;
  }

  /** Extracts a user-friendly message from ProblemDetails, including field errors. */
  private static formatMessage(problem: ProblemDetails): string {
    // If there are field-level errors, show them
    if (problem.errors && Object.keys(problem.errors).length > 0) {
      const fieldMessages = Object.entries(problem.errors)
        .flatMap(([, msgs]) => msgs)
        .filter(Boolean);
      if (fieldMessages.length > 0) return fieldMessages.join(". ");
    }

    return problem.detail ?? problem.title ?? "An unexpected error occurred";
  }
}

// ---- Correlation ID helper ----

function generateCorrelationId(): string {
  return crypto.randomUUID();
}

// ---- Token helpers ----

function getAccessToken(): string | null {
  if (typeof window === "undefined") return null;
  try {
    const stored = localStorage.getItem("teamflow-auth");
    if (!stored) return null;
    const parsed = JSON.parse(stored);
    return parsed?.state?.accessToken ?? null;
  } catch {
    return null;
  }
}

// ---- Axios instance ----

export const apiClient = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000/api/v1",
  headers: {
    "Content-Type": "application/json",
  },
  timeout: 30_000,
});

// ---- Request interceptor: correlation ID + JWT ----

apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    config.headers["X-Correlation-Id"] = generateCorrelationId();

    const token = getAccessToken();
    if (token) {
      config.headers["Authorization"] = `Bearer ${token}`;
    }

    return config;
  },
  (error) => Promise.reject(error),
);

// ---- Response interceptor: silent refresh on 401, parse ProblemDetails ----

let isRefreshing = false;
let failedQueue: Array<{
  resolve: (token: string) => void;
  reject: (error: unknown) => void;
}> = [];

function processQueue(error: unknown, token: string | null) {
  for (const prom of failedQueue) {
    if (token) {
      prom.resolve(token);
    } else {
      prom.reject(error);
    }
  }
  failedQueue = [];
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & {
      _retry?: boolean;
    };

    // Silent refresh on 401 (skip for auth endpoints)
    if (
      error.response?.status === 401 &&
      originalRequest &&
      !originalRequest._retry &&
      !originalRequest.url?.includes("/auth/")
    ) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({
            resolve: (token: string) => {
              originalRequest.headers["Authorization"] = `Bearer ${token}`;
              resolve(apiClient(originalRequest));
            },
            reject,
          });
        });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        const storedRefreshToken = getRefreshToken();
        if (!storedRefreshToken) {
          throw new Error("No refresh token");
        }

        const { data } = await axios.post<{
          accessToken: string;
          refreshToken: string;
          expiresAt: string;
        }>(
          `${apiClient.defaults.baseURL}/auth/refresh`,
          { token: storedRefreshToken },
          { headers: { "Content-Type": "application/json" } },
        );

        // Update stored tokens
        updateStoredTokens(data.accessToken, data.refreshToken, data.expiresAt);

        originalRequest.headers["Authorization"] =
          `Bearer ${data.accessToken}`;
        processQueue(null, data.accessToken);

        return apiClient(originalRequest);
      } catch (refreshError) {
        processQueue(refreshError, null);
        clearStoredAuth();

        if (typeof window !== "undefined") {
          window.location.href = "/login";
        }

        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    // Parse ProblemDetails
    if (error.response) {
      const data = error.response.data as Partial<ProblemDetails>;

      const problem: ProblemDetails = {
        status: error.response.status,
        title: data?.title ?? error.message,
        detail: data?.detail,
        instance: data?.instance,
        correlationId: data?.correlationId,
        errors: (data as { errors?: Record<string, string[]> })?.errors,
      };

      return Promise.reject(new ApiError(problem));
    }

    if (error.request) {
      const problem: ProblemDetails = {
        status: 0,
        title: "Network Error",
        detail: "Unable to reach the server. Please check your connection.",
      };
      return Promise.reject(new ApiError(problem));
    }

    return Promise.reject(error);
  },
);

// ---- Storage helpers for refresh flow ----

function getRefreshToken(): string | null {
  if (typeof window === "undefined") return null;
  try {
    const stored = localStorage.getItem("teamflow-auth");
    if (!stored) return null;
    const parsed = JSON.parse(stored);
    return parsed?.state?.refreshToken ?? null;
  } catch {
    return null;
  }
}

function updateStoredTokens(
  accessToken: string,
  refreshToken: string,
  expiresAt: string,
) {
  if (typeof window === "undefined") return;
  try {
    const stored = localStorage.getItem("teamflow-auth");
    if (!stored) return;
    const parsed = JSON.parse(stored);
    if (parsed?.state) {
      parsed.state.accessToken = accessToken;
      parsed.state.refreshToken = refreshToken;
      parsed.state.expiresAt = expiresAt;
      localStorage.setItem("teamflow-auth", JSON.stringify(parsed));
    }
  } catch {
    // ignore
  }
}

function clearStoredAuth() {
  if (typeof window === "undefined") return;
  localStorage.removeItem("teamflow-auth");
}
