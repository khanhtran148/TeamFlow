import type { ReactNode } from "react";

interface BreadcrumbSegment {
  label: string;
  href?: string;
  bold?: boolean;
}

interface BreadcrumbProps {
  segments?: BreadcrumbSegment[];
  children?: ReactNode;
}

export function Breadcrumb({ segments, children }: BreadcrumbProps) {
  if (children) {
    return (
      <nav
        aria-label="breadcrumb"
        style={{
          display: "flex",
          alignItems: "center",
          gap: 5,
          fontSize: 12,
          color: "var(--tf-text2)",
        }}
      >
        {children}
      </nav>
    );
  }

  if (!segments || segments.length === 0) return null;

  return (
    <nav
      aria-label="breadcrumb"
      style={{
        display: "flex",
        alignItems: "center",
        gap: 5,
        fontSize: 12,
        color: "var(--tf-text2)",
      }}
    >
      {segments.map((seg, i) => (
        <span key={i} style={{ display: "flex", alignItems: "center", gap: 5 }}>
          {i > 0 && (
            <span style={{ color: "var(--tf-text3)" }}>›</span>
          )}
          {seg.href ? (
            <a
              href={seg.href}
              style={{
                color: seg.bold ? "var(--tf-text)" : "var(--tf-text2)",
                fontWeight: seg.bold ? 500 : undefined,
                textDecoration: "none",
              }}
            >
              {seg.label}
            </a>
          ) : (
            <span
              style={{
                color: seg.bold ? "var(--tf-text)" : "var(--tf-text3)",
                fontWeight: seg.bold ? 500 : undefined,
              }}
            >
              {seg.label}
            </span>
          )}
        </span>
      ))}
    </nav>
  );
}

export function BreadcrumbSeparator() {
  return (
    <span style={{ color: "var(--tf-text3)" }}>›</span>
  );
}
