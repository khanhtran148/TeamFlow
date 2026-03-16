"use client";

import { AlertTriangle, RefreshCw, WifiOff } from "lucide-react";
import type { ApiError } from "@/lib/api/client";

interface ErrorDisplayProps {
  /** The caught error — can be ApiError (with ProblemDetails) or a plain Error */
  error: Error | ApiError | unknown;
  /** Called when user clicks retry */
  onRetry?: () => void;
  /** Optional custom title */
  title?: string;
}

function isApiError(err: unknown): err is ApiError {
  return (
    err instanceof Error &&
    (err as ApiError).name === "ApiError" &&
    "status" in err &&
    "problem" in err
  );
}

function isNetworkError(err: unknown): boolean {
  if (err instanceof Error) {
    const msg = err.message.toLowerCase();
    return (
      msg.includes("network") ||
      msg.includes("failed to fetch") ||
      msg.includes("econnrefused") ||
      msg.includes("networkerror")
    );
  }
  return false;
}

function extractMessage(error: unknown): string {
  if (isApiError(error)) {
    // ProblemDetails detail field takes priority
    if (error.problem.detail) return error.problem.detail;
    if (error.problem.title) return error.problem.title;
    return error.message ?? "An error occurred.";
  }
  if (error instanceof Error) return error.message;
  return "An unexpected error occurred.";
}

export function ErrorDisplay({ error, onRetry, title }: ErrorDisplayProps) {
  const network = isNetworkError(error);
  const message = extractMessage(error);

  const statusCode = isApiError(error) ? error.status : undefined;
  // Note: status 0 = network error from the client, not a real HTTP status
  const displayStatusCode = statusCode && statusCode > 0 ? statusCode : undefined;

  const defaultTitle = network
    ? "Network error"
    : statusCode === 404
      ? "Not found"
      : statusCode === 403
        ? "Access denied"
        : title ?? "Something went wrong";

  const defaultDescription = network
    ? "Check your internet connection and that the API is running, then try again."
    : message;

  const Icon = network ? WifiOff : AlertTriangle;
  const iconColor = network ? "var(--tf-yellow)" : "var(--tf-red)";
  const iconBg = network ? "var(--tf-yellow-dim)" : "var(--tf-red-dim)";
  const iconBorder = network ? "var(--tf-yellow-dim)" : "var(--tf-red)";

  return (
    <div
      style={{
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        gap: 14,
        padding: "32px 20px",
        textAlign: "center",
      }}
    >
      <div
        style={{
          width: 40,
          height: 40,
          borderRadius: 10,
          background: iconBg,
          border: `1px solid ${iconBorder}`,
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
      >
        <Icon size={18} color={iconColor} />
      </div>

      <div>
        <p
          style={{
            fontFamily: "var(--tf-font-head)",
            fontWeight: 700,
            fontSize: 14,
            color: "var(--tf-text)",
            marginBottom: 6,
          }}
        >
          {defaultTitle}
        </p>
        <p
          style={{
            fontSize: 13,
            color: "var(--tf-text3)",
            lineHeight: 1.6,
            maxWidth: 320,
          }}
        >
          {defaultDescription}
        </p>
        {displayStatusCode && (
          <p
            style={{
              fontSize: 13,
              color: "var(--tf-text3)",
              fontFamily: "var(--tf-font-mono)",
              marginTop: 6,
            }}
          >
            HTTP {displayStatusCode}
          </p>
        )}
      </div>

      {onRetry && (
        <button
          onClick={onRetry}
          style={{
            display: "inline-flex",
            alignItems: "center",
            gap: 6,
            padding: "6px 14px",
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
          <RefreshCw size={12} />
          Retry
        </button>
      )}
    </div>
  );
}
