"use client";

import { create } from "zustand";
import { persist } from "zustand/middleware";

export type SystemRole = "User" | "SystemAdmin";

export interface AuthUser {
  id: string;
  email: string;
  name: string;
  systemRole: SystemRole;
}

interface AuthState {
  user: AuthUser | null;
  accessToken: string | null;
  refreshToken: string | null;
  expiresAt: string | null;
  isAuthenticated: boolean;

  setAuth: (params: {
    user: AuthUser;
    accessToken: string;
    refreshToken: string;
    expiresAt: string;
  }) => void;
  updateTokens: (params: {
    accessToken: string;
    refreshToken: string;
    expiresAt: string;
  }) => void;
  clearAuth: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      accessToken: null,
      refreshToken: null,
      expiresAt: null,
      isAuthenticated: false,

      setAuth: ({ user, accessToken, refreshToken, expiresAt }) =>
        set({
          user,
          accessToken,
          refreshToken,
          expiresAt,
          isAuthenticated: true,
        }),

      updateTokens: ({ accessToken, refreshToken, expiresAt }) =>
        set({ accessToken, refreshToken, expiresAt }),

      clearAuth: () =>
        set({
          user: null,
          accessToken: null,
          refreshToken: null,
          expiresAt: null,
          isAuthenticated: false,
        }),
    }),
    {
      name: "teamflow-auth",
      partialize: (state) => ({
        user: state.user,
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        expiresAt: state.expiresAt,
        isAuthenticated: state.isAuthenticated,
      }),
    },
  ),
);

/**
 * Parse JWT payload to extract user info.
 * Does NOT validate the token — that's the server's job.
 */
export function parseJwtUser(accessToken: string): AuthUser | null {
  try {
    const payload = accessToken.split(".")[1];
    if (!payload) return null;
    const decoded = JSON.parse(atob(payload));
    const rawRole = decoded.system_role ?? "User";
    const systemRole: SystemRole = rawRole === "SystemAdmin" ? "SystemAdmin" : "User";
    return {
      id: decoded.sub ?? "",
      email: decoded.email ?? "",
      name: decoded.name ?? "",
      systemRole,
    };
  } catch {
    return null;
  }
}
