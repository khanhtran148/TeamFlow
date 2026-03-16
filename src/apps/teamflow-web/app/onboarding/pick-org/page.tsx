"use client";

import { useAuthStore } from "@/lib/stores/auth-store";
import { useMyOrganizations } from "@/lib/hooks/use-organizations";
import { OrgPickerCard } from "@/components/onboarding/org-picker-card";
import { LoadingSkeleton } from "@/components/shared/loading-skeleton";
import { LogOut, Plus } from "lucide-react";
import Link from "next/link";

export default function PickOrgPage() {
  const user = useAuthStore((s) => s.user);
  const clearAuth = useAuthStore((s) => s.clearAuth);
  const { data: orgs, isLoading, isError } = useMyOrganizations();

  return (
    <div
      style={{
        minHeight: "100vh",
        background: "var(--tf-bg)",
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        padding: "40px 20px",
      }}
    >
      <div style={{ width: "100%", maxWidth: 640 }}>
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
            Select an Organization
          </h1>

          <p style={{ fontSize: 14, color: "var(--tf-text3)" }}>
            Hi {user?.name ?? "there"}! Choose which organization to open.
          </p>
        </div>

        {/* Org grid */}
        {isLoading ? (
          <LoadingSkeleton rows={4} />
        ) : isError ? (
          <div
            style={{
              textAlign: "center",
              fontSize: 13,
              color: "var(--tf-text3)",
              padding: 24,
            }}
          >
            Failed to load organizations. Please refresh.
          </div>
        ) : (
          <>
            <div
              style={{
                display: "grid",
                gridTemplateColumns: "repeat(auto-fill, minmax(200px, 1fr))",
                gap: 12,
              }}
            >
              {orgs?.map((org) => (
                <OrgPickerCard key={org.id} org={org} />
              ))}
            </div>

            {(!orgs || orgs.length === 0) && (
              <div
                style={{
                  textAlign: "center",
                  fontSize: 13,
                  color: "var(--tf-text3)",
                  padding: 24,
                }}
              >
                No organizations found.{" "}
                <Link
                  href="/onboarding/no-orgs"
                  style={{ color: "var(--tf-accent)", textDecoration: "none" }}
                >
                  Learn more
                </Link>
              </div>
            )}
          </>
        )}

        {/* Admin: create org */}
        {user?.systemRole === "SystemAdmin" && (
          <div style={{ textAlign: "center", marginTop: 24 }}>
            <Link
              href="/admin/organizations"
              style={{
                display: "inline-flex",
                alignItems: "center",
                gap: 6,
                fontSize: 13,
                color: "var(--tf-accent)",
                textDecoration: "none",
                padding: "7px 14px",
                borderRadius: 6,
                border: "1px solid var(--tf-accent)",
              }}
            >
              <Plus size={13} />
              Create Organization
            </Link>
          </div>
        )}

        {/* Sign out */}
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
