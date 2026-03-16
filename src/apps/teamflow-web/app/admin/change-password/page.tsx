"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { Shield, Lock } from "lucide-react";
import { changePassword } from "@/lib/api/auth";
import { useAuthStore } from "@/lib/stores/auth-store";
import { AuthInput } from "@/components/auth/auth-input";
import { ApiError } from "@/lib/api/client";

export default function ChangePasswordPage() {
  const router = useRouter();
  const mustChangePassword = useAuthStore((s) => s.mustChangePassword);
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const clearMustChangePassword = useAuthStore((s) => s.clearMustChangePassword);
  const clearAuth = useAuthStore((s) => s.clearAuth);

  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!isAuthenticated) {
      router.replace("/login");
    }
  }, [isAuthenticated, router]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);

    if (newPassword !== confirmPassword) {
      setError("New passwords do not match.");
      return;
    }

    if (newPassword.length < 8) {
      setError("New password must be at least 8 characters.");
      return;
    }

    setLoading(true);
    try {
      await changePassword({ currentPassword, newPassword });
      clearMustChangePassword();
      router.push("/admin");
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.problem.detail ?? err.problem.title);
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("An unexpected error occurred.");
      }
    } finally {
      setLoading(false);
    }
  }

  function handleDismiss() {
    clearAuth();
    router.push("/login");
  }

  if (!isAuthenticated) {
    return null;
  }

  return (
    <div
      style={{
        minHeight: "100vh",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        background: "var(--tf-bg)",
        padding: 24,
      }}
    >
      <div
        style={{
          width: "100%",
          maxWidth: 440,
          padding: 32,
          borderRadius: 12,
          background: "var(--tf-bg2)",
          border: "1px solid var(--tf-border)",
        }}
      >
        {/* Header */}
        <div style={{ textAlign: "center", marginBottom: 28 }}>
          <div
            style={{
              display: "inline-flex",
              alignItems: "center",
              justifyContent: "center",
              width: 48,
              height: 48,
              borderRadius: 12,
              background: "var(--tf-accent-dim)",
              marginBottom: 16,
            }}
          >
            <Shield size={24} color="var(--tf-accent)" />
          </div>
          <h1
            style={{
              fontSize: 22,
              fontWeight: 700,
              color: "var(--tf-text)",
              fontFamily: "var(--tf-font-head)",
              margin: 0,
              marginBottom: 6,
            }}
          >
            Change Your Password
          </h1>
          {mustChangePassword && (
            <p
              style={{
                fontSize: 13,
                color: "var(--tf-orange)",
                margin: 0,
                fontFamily: "var(--tf-font-body)",
              }}
            >
              You must change your password before continuing.
            </p>
          )}
          {!mustChangePassword && (
            <p
              style={{
                fontSize: 13,
                color: "var(--tf-text3)",
                margin: 0,
                fontFamily: "var(--tf-font-body)",
              }}
            >
              Enter your current password and choose a new one.
            </p>
          )}
        </div>

        {/* Form */}
        <form
          onSubmit={handleSubmit}
          style={{ display: "flex", flexDirection: "column", gap: 16 }}
        >
          <AuthInput
            label="Current Password"
            type="password"
            placeholder="Your current password"
            value={currentPassword}
            onChange={(e) => setCurrentPassword(e.target.value)}
            required
            autoComplete="current-password"
            autoFocus
          />
          <AuthInput
            label="New Password"
            type="password"
            placeholder="At least 8 characters"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            required
            autoComplete="new-password"
          />
          <AuthInput
            label="Confirm New Password"
            type="password"
            placeholder="Repeat new password"
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
            required
            autoComplete="new-password"
          />

          {error && (
            <div
              role="alert"
              style={{
                padding: "10px 14px",
                borderRadius: 8,
                background: "rgba(248, 113, 113, 0.1)",
                border: "1px solid rgba(248, 113, 113, 0.3)",
                color: "var(--tf-red)",
                fontSize: 13,
                fontFamily: "var(--tf-font-body)",
              }}
            >
              {error}
            </div>
          )}

          <button
            type="submit"
            disabled={loading}
            style={{
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              gap: 8,
              padding: "10px 0",
              borderRadius: 8,
              border: "none",
              background:
                "linear-gradient(135deg, var(--tf-accent), var(--tf-blue))",
              color: "#0a0a0b",
              fontWeight: 600,
              fontSize: 14,
              cursor: loading ? "not-allowed" : "pointer",
              opacity: loading ? 0.7 : 1,
              transition: "opacity 0.2s",
              fontFamily: "var(--tf-font-body)",
              minHeight: 44,
            }}
          >
            <Lock size={14} />
            {loading ? "Changing password..." : "Change Password"}
          </button>

          <button
            type="button"
            onClick={handleDismiss}
            style={{
              padding: "8px 0",
              borderRadius: 8,
              border: "1px solid var(--tf-border)",
              background: "transparent",
              color: "var(--tf-text3)",
              fontWeight: 500,
              fontSize: 13,
              cursor: "pointer",
              transition: "border-color 0.2s, color 0.2s",
              fontFamily: "var(--tf-font-body)",
              minHeight: 44,
            }}
            onMouseEnter={(e) => {
              (e.currentTarget as HTMLButtonElement).style.borderColor =
                "var(--tf-border2)";
              (e.currentTarget as HTMLButtonElement).style.color =
                "var(--tf-text2)";
            }}
            onMouseLeave={(e) => {
              (e.currentTarget as HTMLButtonElement).style.borderColor =
                "var(--tf-border)";
              (e.currentTarget as HTMLButtonElement).style.color =
                "var(--tf-text3)";
            }}
          >
            Dismiss (log out)
          </button>
        </form>
      </div>
    </div>
  );
}
