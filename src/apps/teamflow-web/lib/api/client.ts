import axios, { type AxiosError, type InternalAxiosRequestConfig } from "axios";
import type { ProblemDetails } from "./types";

// ---- API Error class ----

export class ApiError extends Error {
  public readonly status: number;
  public readonly problem: ProblemDetails;

  constructor(problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? "An unexpected error occurred");
    this.name = "ApiError";
    this.status = problem.status;
    this.problem = problem;
  }
}

// ---- Correlation ID helper ----

function generateCorrelationId(): string {
  return crypto.randomUUID();
}

// ---- Axios instance ----

export const apiClient = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000/api/v1",
  headers: {
    "Content-Type": "application/json",
  },
  timeout: 30_000,
});

// ---- Request interceptor: correlation ID + JWT skeleton ----

apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    // Always attach a correlation ID
    config.headers["X-Correlation-Id"] = generateCorrelationId();

    // JWT interceptor skeleton — Phase 1: no-op
    // Phase 2: read token from cookie/localStorage and attach as Authorization header
    // const token = getAccessToken();
    // if (token) config.headers["Authorization"] = `Bearer ${token}`;

    return config;
  },
  (error) => Promise.reject(error),
);

// ---- Response interceptor: parse ProblemDetails errors ----

apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
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
