"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

interface ProjectNavProps {
  projectId: string;
  orgSlug?: string;
}

function makeNav(orgSlug?: string) {
  const base = orgSlug ? `/org/${orgSlug}/projects` : `/projects`;
  return [
    { label: "Backlog", href: (id: string) => `${base}/${id}/backlog` },
    { label: "Board", href: (id: string) => `${base}/${id}/board` },
    { label: "Sprints", href: (id: string) => `${base}/${id}/sprints` },
    { label: "Releases", href: (id: string) => `${base}/${id}/releases` },
    { label: "Retros", href: (id: string) => `${base}/${id}/retros` },
  ];
}

export function ProjectNav({ projectId, orgSlug }: ProjectNavProps) {
  const NAV_TABS = makeNav(orgSlug);
  const pathname = usePathname();

  return (
    <nav
      style={{
        display: "flex",
        alignItems: "center",
        gap: 4,
        padding: "0 20px",
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
              padding: "12px 16px",
              fontSize: 13,
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
