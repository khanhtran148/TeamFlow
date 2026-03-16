import type { ReactNode } from "react";
import Link from "next/link";
import { ThemeToggle } from "./theme-toggle";
import { UserMenu } from "./user-menu";
import { NotificationBell } from "./notification-bell";

interface TopBarProps {
  breadcrumb?: ReactNode;
  actions?: ReactNode;
}

export function TopBar({ breadcrumb, actions }: TopBarProps) {
  return (
    <header
      data-testid="top-bar"
      style={{
        display: "flex",
        alignItems: "center",
        gap: 10,
        padding: "0 16px",
        height: 50,
        background: "var(--tf-bg2)",
        borderBottom: "1px solid var(--tf-border)",
        flexShrink: 0,
      }}
    >
      {/* Logo */}
      <Link
        data-testid="nav-projects"
        href="/projects"
        style={{
          display: "flex",
          alignItems: "center",
          gap: 7,
          fontFamily: "var(--tf-font-head)",
          fontWeight: 800,
          fontSize: 16,
          color: "var(--tf-text)",
          textDecoration: "none",
          marginRight: 6,
          flexShrink: 0,
        }}
      >
        <LogoIcon />
        TeamFlow
      </Link>

      {/* Breadcrumb slot */}
      {breadcrumb && (
        <div style={{ display: "flex", alignItems: "center", gap: 5 }}>
          {breadcrumb}
        </div>
      )}

      {/* Right section */}
      <div
        style={{
          marginLeft: "auto",
          display: "flex",
          alignItems: "center",
          gap: 7,
        }}
      >
        {actions}
        <NotificationBell />
        <ThemeToggle />
        <UserMenu />
      </div>
    </header>
  );
}

function LogoIcon() {
  return (
    <div
      style={{
        width: 24,
        height: 24,
        borderRadius: 5,
        background: "linear-gradient(135deg, var(--tf-accent), var(--tf-blue))",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        fontSize: 11,
        fontWeight: 800,
        color: "#0a0a0b",
        flexShrink: 0,
      }}
    >
      TF
    </div>
  );
}
