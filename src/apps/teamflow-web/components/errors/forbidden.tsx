import { ShieldX } from "lucide-react";

export function ForbiddenPage() {
  return (
    <div
      style={{
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        minHeight: "50vh",
        gap: 12,
        color: "var(--tf-text3)",
      }}
    >
      <ShieldX size={48} style={{ color: "var(--tf-accent)" }} />
      <h2
        style={{
          fontSize: 20,
          fontWeight: 700,
          color: "var(--tf-text)",
          fontFamily: "var(--tf-font-head)",
          margin: 0,
        }}
      >
        Access Denied
      </h2>
      <p style={{ fontSize: 13, maxWidth: 400, textAlign: "center" }}>
        You don&apos;t have permission to access this resource.
        Contact your team manager or organization admin for access.
      </p>
      <a
        href="/projects"
        style={{
          marginTop: 8,
          padding: "8px 16px",
          borderRadius: 8,
          background: "var(--tf-bg3)",
          border: "1px solid var(--tf-border)",
          color: "var(--tf-text)",
          textDecoration: "none",
          fontSize: 13,
        }}
      >
        Back to Projects
      </a>
    </div>
  );
}
