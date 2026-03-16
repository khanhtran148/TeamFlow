"use client";

import { useState, useRef, useEffect } from "react";
import { useRouter } from "next/navigation";
import { LogOut, User } from "lucide-react";
import { useAuthStore } from "@/lib/stores/auth-store";
import { logout as logoutApi } from "@/lib/api/auth";
import { UserAvatar } from "@/components/shared/user-avatar";

export function UserMenu() {
  const router = useRouter();
  const user = useAuthStore((s) => s.user);
  const clearAuth = useAuthStore((s) => s.clearAuth);
  const [open, setOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  if (!user) return null;

  const initials = user.name
    .split(" ")
    .map((n) => n[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();

  async function handleLogout() {
    try {
      await logoutApi();
    } catch {
      // Logout even if API call fails
    }
    clearAuth();
    router.push("/login");
  }

  return (
    <div ref={menuRef} style={{ position: "relative" }}>
      <button
        data-testid="user-menu-btn"
        onClick={() => setOpen(!open)}
        style={{
          background: "none",
          border: "none",
          cursor: "pointer",
          padding: 0,
        }}
        aria-label="User menu"
      >
        <UserAvatar initials={initials} size="sm" />
      </button>

      {open && (
        <div
          data-testid="user-menu"
          style={{
            position: "absolute",
            right: 0,
            top: "calc(100% + 8px)",
            minWidth: 200,
            background: "var(--tf-bg2)",
            border: "1px solid var(--tf-border)",
            borderRadius: 10,
            padding: 6,
            zIndex: 50,
            boxShadow: "0 8px 24px rgba(0,0,0,0.2)",
          }}
        >
          <div
            style={{
              padding: "8px 12px",
              borderBottom: "1px solid var(--tf-border)",
              marginBottom: 4,
            }}
          >
            <div
              style={{
                fontSize: 13,
                fontWeight: 500,
                color: "var(--tf-text)",
              }}
            >
              {user.name}
            </div>
            <div
              style={{
                fontSize: 13,
                color: "var(--tf-text3)",
                marginTop: 2,
              }}
            >
              {user.email}
            </div>
          </div>

          <button
            data-testid="profile-btn"
            onClick={() => {
              setOpen(false);
              router.push("/profile");
            }}
            style={{
              display: "flex",
              alignItems: "center",
              gap: 8,
              width: "100%",
              padding: "8px 12px",
              borderRadius: 6,
              border: "none",
              background: "transparent",
              color: "var(--tf-text2)",
              fontSize: 13,
              cursor: "pointer",
              textAlign: "left",
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.background = "var(--tf-bg3)";
              e.currentTarget.style.color = "var(--tf-text)";
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.background = "transparent";
              e.currentTarget.style.color = "var(--tf-text2)";
            }}
          >
            <User size={14} />
            Profile
          </button>

          <button
            data-testid="logout-btn"
            onClick={handleLogout}
            style={{
              display: "flex",
              alignItems: "center",
              gap: 8,
              width: "100%",
              padding: "8px 12px",
              borderRadius: 6,
              border: "none",
              background: "transparent",
              color: "var(--tf-text2)",
              fontSize: 13,
              cursor: "pointer",
              textAlign: "left",
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.background = "var(--tf-bg3)";
              e.currentTarget.style.color = "#ef4444";
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.background = "transparent";
              e.currentTarget.style.color = "var(--tf-text2)";
            }}
          >
            <LogOut size={14} />
            Sign out
          </button>
        </div>
      )}
    </div>
  );
}
