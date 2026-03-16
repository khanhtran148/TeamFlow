"use client";

import Link from "next/link";
import { Building2, LogOut } from "lucide-react";
import { useAuthStore } from "@/lib/stores/auth-store";
import { PendingInvitations } from "@/components/onboarding/pending-invitations";

export default function NoOrgsPage() {
  const user = useAuthStore((s) => s.user);
  const clearAuth = useAuthStore((s) => s.clearAuth);

  return (
    <div
      style={{
        minHeight: "100vh",
        background: "var(--tf-bg)",
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        padding: "24px 20px",
      }}
    >
      <div style={{ width: "100%", maxWidth: 480 }}>
        {/* Header */}
        <div style={{ textAlign: "center", marginBottom: 32 }}>
          <div
            style={{
              width: 56,
              height: 56,
              borderRadius: 14,
              background: "linear-gradient(135deg, var(--tf-accent), var(--tf-blue))",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              margin: "0 auto 16px",
              fontSize: 20,
              fontWeight: 800,
              color: "#0a0a0b",
            }}
          >
            TF
          </div>

          <h1
            style={{
              fontFamily: "var(--tf-font-head)",
              fontWeight: 700,
              fontSize: 24,
              color: "var(--tf-text)",
              marginBottom: 8,
            }}
          >
            Welcome to TeamFlow
          </h1>

          <p style={{ fontSize: 14, color: "var(--tf-text3)", lineHeight: 1.5 }}>
            Hi {user?.name ?? "there"}! You are not a member of any organization yet.
          </p>
        </div>

        {/* Pending invitations section */}
        <PendingInvitations />

        {/* No invitations info */}
        <div
          style={{
            marginTop: 20,
            background: "var(--tf-bg2)",
            border: "1px solid var(--tf-border)",
            borderRadius: "var(--tf-radius)",
            padding: 20,
            display: "flex",
            flexDirection: "column",
            gap: 12,
          }}
        >
          <div style={{ display: "flex", alignItems: "flex-start", gap: 12 }}>
            <Building2 size={20} color="var(--tf-text3)" style={{ flexShrink: 0, marginTop: 2 }} />
            <div>
              <div
                style={{ fontSize: 14, fontWeight: 600, color: "var(--tf-text)", marginBottom: 4 }}
              >
                Need an organization?
              </div>
              <p style={{ fontSize: 13, color: "var(--tf-text3)", lineHeight: 1.6, margin: 0 }}>
                Ask your system administrator to create an organization and invite you, or check your
                email for an invitation link.
              </p>
            </div>
          </div>
        </div>

        {/* Sign out link */}
        <div style={{ textAlign: "center", marginTop: 24 }}>
          <button
            onClick={() => {
              clearAuth();
              window.location.href = "/login";
            }}
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 6,
              fontSize: 13,
              color: "var(--tf-text3)",
              background: "none",
              border: "none",
              cursor: "pointer",
              fontFamily: "var(--tf-font-body)",
            }}
          >
            <LogOut size={13} />
            Sign out
          </button>
        </div>
      </div>
    </div>
  );
}
