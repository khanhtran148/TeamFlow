"use client";

import { useState, type FormEvent, type ReactNode } from "react";
import { ApiError } from "@/lib/api/client";

interface AuthFormProps {
  onSubmit: () => Promise<void>;
  children: ReactNode;
  submitLabel: string;
  footer?: ReactNode;
}

export function AuthForm({
  onSubmit,
  children,
  submitLabel,
  footer,
}: AuthFormProps) {
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      await onSubmit();
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.problem.detail ?? err.problem.title);
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("An unexpected error occurred");
      }
    } finally {
      setLoading(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} style={{ width: "100%", maxWidth: 400 }}>
      <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
        {children}

        {error && (
          <div
            role="alert"
            style={{
              padding: "10px 14px",
              borderRadius: 8,
              background: "rgba(239, 68, 68, 0.1)",
              border: "1px solid rgba(239, 68, 68, 0.3)",
              color: "#ef4444",
              fontSize: 13,
            }}
          >
            {error}
          </div>
        )}

        <button
          type="submit"
          disabled={loading}
          style={{
            padding: "10px 0",
            borderRadius: 8,
            border: "none",
            background:
              "linear-gradient(135deg, var(--tf-accent), var(--tf-blue))",
            color: "#0a0a0b",
            fontWeight: 600,
            fontSize: 14,
            cursor: loading ? "not-allowed" : "pointer",
            opacity: loading ? 0.7 : 1,
            transition: "opacity 0.2s",
          }}
        >
          {loading ? "Please wait..." : submitLabel}
        </button>

        {footer}
      </div>
    </form>
  );
}
