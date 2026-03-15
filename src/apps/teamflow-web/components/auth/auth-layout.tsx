import type { ReactNode } from "react";

interface AuthLayoutProps {
  title: string;
  subtitle?: string;
  children: ReactNode;
}

export function AuthLayout({ title, subtitle, children }: AuthLayoutProps) {
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
        <div style={{ textAlign: "center", marginBottom: 28 }}>
          <div
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 8,
              marginBottom: 16,
            }}
          >
            <div
              style={{
                width: 32,
                height: 32,
                borderRadius: 7,
                background:
                  "linear-gradient(135deg, var(--tf-accent), var(--tf-blue))",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                fontSize: 14,
                fontWeight: 800,
                color: "#0a0a0b",
              }}
            >
              TF
            </div>
            <span
              style={{
                fontFamily: "var(--tf-font-head)",
                fontWeight: 800,
                fontSize: 20,
                color: "var(--tf-text)",
              }}
            >
              TeamFlow
            </span>
          </div>
          <h1
            style={{
              fontSize: 22,
              fontWeight: 700,
              color: "var(--tf-text)",
              fontFamily: "var(--tf-font-head)",
              margin: 0,
            }}
          >
            {title}
          </h1>
          {subtitle && (
            <p
              style={{
                fontSize: 13,
                color: "var(--tf-text3)",
                marginTop: 6,
              }}
            >
              {subtitle}
            </p>
          )}
        </div>
        <div style={{ display: "flex", justifyContent: "center" }}>
          {children}
        </div>
      </div>
    </div>
  );
}
