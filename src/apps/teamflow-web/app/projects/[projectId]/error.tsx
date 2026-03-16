"use client";

import { useEffect } from "react";
import { AlertTriangle, RefreshCw } from "lucide-react";
import Link from "next/link";

interface ErrorPageProps {
  error: Error & { digest?: string };
  reset: () => void;
}

export default function ProjectError({ error, reset }: ErrorPageProps) {
  useEffect(() => {
    // Log to error reporting in production
    console.error("[ProjectError]", error);
  }, [error]);

  return (
    <div
      style={{
        flex: 1,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        padding: "40px 20px",
      }}
    >
      <div
        style={{
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          gap: 16,
          maxWidth: 400,
          textAlign: "center",
        }}
      >
        <div
          style={{
            width: 48,
            height: 48,
            borderRadius: 12,
            background: "var(--tf-red-dim)",
            border: "1px solid var(--tf-red)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
          }}
        >
          <AlertTriangle size={22} color="var(--tf-red)" />
        </div>

        <div>
          <h2
            style={{
              fontFamily: "var(--tf-font-head)",
              fontWeight: 700,
              fontSize: 18,
              color: "var(--tf-text)",
              marginBottom: 8,
            }}
          >
            Something went wrong
          </h2>
          <p
            style={{
              fontSize: 13,
              color: "var(--tf-text3)",
              lineHeight: 1.6,
            }}
          >
            {error.message && error.message !== "An error occurred in the Server Components render."
              ? error.message
              : "An unexpected error occurred while loading this page."}
          </p>
        </div>

        <div style={{ display: "flex", gap: 10, flexWrap: "wrap", justifyContent: "center" }}>
          <button
            onClick={reset}
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 6,
              padding: "7px 14px",
              borderRadius: 6,
              border: "1px solid var(--tf-border)",
              background: "var(--tf-bg3)",
              color: "var(--tf-text2)",
              fontSize: 13,
              fontWeight: 500,
              cursor: "pointer",
              fontFamily: "var(--tf-font-body)",
              transition: "all var(--tf-tr)",
            }}
            onMouseEnter={(e) => {
              (e.currentTarget as HTMLButtonElement).style.background = "var(--tf-bg4)";
              (e.currentTarget as HTMLButtonElement).style.color = "var(--tf-text)";
            }}
            onMouseLeave={(e) => {
              (e.currentTarget as HTMLButtonElement).style.background = "var(--tf-bg3)";
              (e.currentTarget as HTMLButtonElement).style.color = "var(--tf-text2)";
            }}
          >
            <RefreshCw size={13} />
            Try again
          </button>

          <Link
            href="/projects"
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 6,
              padding: "7px 14px",
              borderRadius: 6,
              border: "1px solid var(--tf-accent)",
              background: "var(--tf-accent-dim)",
              color: "var(--tf-accent)",
              fontSize: 13,
              fontWeight: 500,
              textDecoration: "none",
              fontFamily: "var(--tf-font-body)",
              transition: "all var(--tf-tr)",
            }}
          >
            Back to Projects
          </Link>
        </div>

        {error.digest && (
          <p
            style={{
              fontSize: 13,
              color: "var(--tf-text3)",
              fontFamily: "var(--tf-font-mono)",
            }}
          >
            Error ID: {error.digest}
          </p>
        )}
      </div>
    </div>
  );
}
