"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

interface ProjectNavProps {
  projectId: string;
}

const NAV_TABS = [
  { label: "Backlog", href: (id: string) => `/projects/${id}/backlog` },
  { label: "Board", href: (id: string) => `/projects/${id}/board` },
  { label: "Sprints", href: (id: string) => `/projects/${id}/sprints` },
  { label: "Releases", href: (id: string) => `/projects/${id}/releases` },
];

export function ProjectNav({ projectId }: ProjectNavProps) {
  const pathname = usePathname();

  return (
    <nav
      style={{
        display: "flex",
        alignItems: "center",
        gap: 2,
        padding: "0 16px",
        background: "var(--tf-bg2)",
        borderBottom: "1px solid var(--tf-border)",
        flexShrink: 0,
      }}
    >
      {NAV_TABS.map((tab) => {
        const href = tab.href(projectId);
        const isActive = pathname.startsWith(href);

        return (
          <Link
            key={tab.label}
            href={href}
            style={{
              display: "flex",
              alignItems: "center",
              padding: "10px 12px",
              fontSize: 12,
              fontWeight: isActive ? 600 : 400,
              color: isActive ? "var(--tf-accent)" : "var(--tf-text3)",
              textDecoration: "none",
              borderBottom: isActive
                ? "2px solid var(--tf-accent)"
                : "2px solid transparent",
              transition: "color var(--tf-tr), border-color var(--tf-tr)",
              whiteSpace: "nowrap",
              marginBottom: -1,
            }}
            onMouseEnter={(e) => {
              if (!isActive) {
                (e.currentTarget as HTMLAnchorElement).style.color = "var(--tf-text2)";
              }
            }}
            onMouseLeave={(e) => {
              if (!isActive) {
                (e.currentTarget as HTMLAnchorElement).style.color = "var(--tf-text3)";
              }
            }}
          >
            {tab.label}
          </Link>
        );
      })}
    </nav>
  );
}
