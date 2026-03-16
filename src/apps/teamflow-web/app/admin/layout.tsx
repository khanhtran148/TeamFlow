"use client";

import type { ReactNode } from "react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { AdminGuard } from "@/components/admin/admin-guard";
import { Shield, LogOut } from "lucide-react";
import { useAuthStore } from "@/lib/stores/auth-store";
import { logout } from "@/lib/api/auth";

const NAV_LINKS = [
  { href: "/admin", label: "Dashboard" },
  { href: "/admin/organizations", label: "Organizations" },
  { href: "/admin/users", label: "Users" },
];

export default function AdminLayout({ children }: { children: ReactNode }) {
  return (
    <AdminGuard>
      <AdminLayoutInner>{children}</AdminLayoutInner>
    </AdminGuard>
  );
}

function AdminLayoutInner({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const clearAuth = useAuthStore((s) => s.clearAuth);

  async function handleLogout() {
    try {
      await logout();
    } catch {
      // Ignore errors — clear auth regardless
    } finally {
      clearAuth();
      router.push("/login");
    }
  }

  return (
    <div
      style={{
        display: "flex",
        height: "100vh",
        overflow: "hidden",
        background: "var(--tf-bg)",
      }}
    >
      {/* Sidebar */}
      <aside
        style={{
          width: 220,
          borderRight: "1px solid var(--tf-border)",
          background: "var(--tf-bg2)",
          display: "flex",
          flexDirection: "column",
          padding: "20px 0",
          flexShrink: 0,
        }}
      >
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 8,
            padding: "0 20px 20px",
            borderBottom: "1px solid var(--tf-border)",
            marginBottom: 12,
          }}
        >
          <Shield size={16} color="var(--tf-accent)" />
          <span
            style={{
              fontFamily: "var(--tf-font-head)",
              fontWeight: 700,
              fontSize: 14,
              color: "var(--tf-text)",
            }}
          >
            Admin Panel
          </span>
        </div>

        <nav
          style={{
            display: "flex",
            flexDirection: "column",
            gap: 2,
            padding: "0 12px",
          }}
        >
          {NAV_LINKS.map((link) => {
            const isActive = pathname === link.href;
            return (
              <Link
                key={link.href}
                href={link.href}
                style={{
                  padding: "7px 10px",
                  borderRadius: 6,
                  fontSize: 13,
                  fontFamily: "var(--tf-font-body)",
                  fontWeight: isActive ? 600 : 400,
                  color: isActive ? "var(--tf-accent)" : "var(--tf-text2)",
                  background: isActive
                    ? "var(--tf-accent-dim)"
                    : "transparent",
                  textDecoration: "none",
                  transition: "all var(--tf-tr)",
                }}
              >
                {link.label}
              </Link>
            );
          })}
        </nav>

        <div
          style={{
            marginTop: "auto",
            padding: "12px 20px",
            display: "flex",
            flexDirection: "column",
            gap: 8,
          }}
        >
          <Link
            href="/onboarding"
            style={{
              fontSize: 12,
              color: "var(--tf-text3)",
              textDecoration: "none",
              fontFamily: "var(--tf-font-mono)",
            }}
          >
            Back to App
          </Link>

          <button
            type="button"
            onClick={handleLogout}
            aria-label="Log out of admin panel"
            style={{
              display: "flex",
              alignItems: "center",
              gap: 6,
              padding: "6px 8px",
              borderRadius: 6,
              border: "none",
              background: "transparent",
              color: "var(--tf-text3)",
              fontSize: 12,
              fontFamily: "var(--tf-font-body)",
              cursor: "pointer",
              transition: "color 0.15s",
              textAlign: "left",
              width: "100%",
            }}
            onMouseEnter={(e) => {
              (e.currentTarget as HTMLButtonElement).style.color =
                "var(--tf-red)";
            }}
            onMouseLeave={(e) => {
              (e.currentTarget as HTMLButtonElement).style.color =
                "var(--tf-text3)";
            }}
          >
            <LogOut size={12} />
            Log out
          </button>
        </div>
      </aside>

      {/* Main content */}
      <main style={{ flex: 1, overflow: "auto", padding: "24px 28px" }}>
        {children}
      </main>
    </div>
  );
}
