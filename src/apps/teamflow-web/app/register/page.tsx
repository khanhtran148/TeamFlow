"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { AuthLayout } from "@/components/auth/auth-layout";
import { AuthForm } from "@/components/auth/auth-form";
import { AuthInput } from "@/components/auth/auth-input";
import { register } from "@/lib/api/auth";
import { useAuthStore, parseJwtUser } from "@/lib/stores/auth-store";

export default function RegisterPage() {
  const router = useRouter();
  const setAuth = useAuthStore((s) => s.setAuth);
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  async function handleRegister() {
    if (password !== confirmPassword) {
      throw new Error("Passwords do not match");
    }

    const response = await register({ email, password, name });
    const user = parseJwtUser(response.accessToken);
    if (!user) throw new Error("Failed to parse user from token");

    setAuth({
      user,
      accessToken: response.accessToken,
      refreshToken: response.refreshToken,
      expiresAt: response.expiresAt,
    });

    router.push("/projects");
  }

  return (
    <AuthLayout
      title="Create account"
      subtitle="Join your team on TeamFlow"
    >
      <AuthForm
        onSubmit={handleRegister}
        submitLabel="Register"
        footer={
          <p
            style={{
              textAlign: "center",
              fontSize: 13,
              color: "var(--tf-text3)",
              marginTop: 8,
            }}
          >
            Already have an account?{" "}
            <Link
              href="/login"
              style={{ color: "var(--tf-accent)", textDecoration: "none" }}
            >
              Sign in
            </Link>
          </p>
        }
      >
        <AuthInput
          label="Name"
          type="text"
          placeholder="Your full name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
          autoComplete="name"
          autoFocus
        />
        <AuthInput
          label="Email"
          type="email"
          placeholder="you@company.com"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          autoComplete="email"
        />
        <AuthInput
          label="Password"
          type="password"
          placeholder="Min. 8 characters, 1 upper, 1 lower, 1 digit"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
          minLength={8}
          autoComplete="new-password"
        />
        <AuthInput
          label="Confirm Password"
          type="password"
          placeholder="Re-enter your password"
          value={confirmPassword}
          onChange={(e) => setConfirmPassword(e.target.value)}
          required
          autoComplete="new-password"
        />
      </AuthForm>
    </AuthLayout>
  );
}
