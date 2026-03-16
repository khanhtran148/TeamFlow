"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { AuthLayout } from "@/components/auth/auth-layout";
import { AuthForm } from "@/components/auth/auth-form";
import { AuthInput } from "@/components/auth/auth-input";
import { login } from "@/lib/api/auth";
import { useAuthStore, parseJwtUser } from "@/lib/stores/auth-store";

export default function LoginPage() {
  const router = useRouter();
  const setAuth = useAuthStore((s) => s.setAuth);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  async function handleLogin() {
    const response = await login({ email, password });
    const user = parseJwtUser(response.accessToken);
    if (!user) throw new Error("Failed to parse user from token");

    setAuth({
      user,
      accessToken: response.accessToken,
      refreshToken: response.refreshToken,
      expiresAt: response.expiresAt,
    });

    router.push(user.systemRole === "SystemAdmin" ? "/admin" : "/onboarding");
  }

  return (
    <AuthLayout title="Sign in" subtitle="Enter your credentials to continue">
      <AuthForm
        onSubmit={handleLogin}
        submitLabel="Sign in"
        footer={
          <p
            style={{
              textAlign: "center",
              fontSize: 13,
              color: "var(--tf-text3)",
              marginTop: 8,
            }}
          >
            Don&apos;t have an account?{" "}
            <Link
              href="/register"
              style={{ color: "var(--tf-accent)", textDecoration: "none" }}
            >
              Register
            </Link>
          </p>
        }
      >
        <AuthInput
          label="Email"
          type="email"
          placeholder="you@company.com"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          autoComplete="email"
          autoFocus
        />
        <AuthInput
          label="Password"
          type="password"
          placeholder="Enter your password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
          autoComplete="current-password"
        />
      </AuthForm>
    </AuthLayout>
  );
}
