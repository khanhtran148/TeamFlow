import { apiClient } from "./client";

// ---- Auth DTOs ----

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  mustChangePassword?: boolean;
}

export interface RegisterBody {
  email: string;
  password: string;
  name: string;
}

export interface LoginBody {
  email: string;
  password: string;
}

export interface ChangePasswordBody {
  currentPassword: string;
  newPassword: string;
}

// ---- Auth API functions ----

export async function register(body: RegisterBody): Promise<AuthResponse> {
  const { data } = await apiClient.post<AuthResponse>("/auth/register", body);
  return data;
}

export async function login(body: LoginBody): Promise<AuthResponse> {
  const { data } = await apiClient.post<AuthResponse>("/auth/login", body);
  return data;
}

export async function refreshToken(token: string): Promise<AuthResponse> {
  const { data } = await apiClient.post<AuthResponse>("/auth/refresh", {
    token,
  });
  return data;
}

export async function changePassword(body: ChangePasswordBody): Promise<void> {
  await apiClient.post("/auth/change-password", body);
}

export async function logout(): Promise<void> {
  await apiClient.post("/auth/logout");
}
