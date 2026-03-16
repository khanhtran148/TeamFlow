"use client";

import { useState } from "react";
import { useActivityLog } from "@/lib/hooks/use-profile";
import { ChevronLeft, ChevronRight } from "lucide-react";

function formatRelativeTime(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime();
  const minutes = Math.floor(diff / 60_000);
  if (minutes < 1) return "just now";
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  if (days < 30) return `${days}d ago`;
  return new Date(iso).toLocaleDateString("en-AU", {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}

function actionBadgeColor(actionType: string): { bg: string; color: string } {
  const lower = actionType.toLowerCase();
  if (lower.includes("created")) return { bg: "rgba(34,197,94,0.12)", color: "#22c55e" };
  if (lower.includes("deleted")) return { bg: "rgba(239,68,68,0.1)", color: "#ef4444" };
  if (lower.includes("status")) return { bg: "rgba(99,102,241,0.12)", color: "var(--tf-accent)" };
  if (lower.includes("assign")) return { bg: "rgba(245,158,11,0.12)", color: "#f59e0b" };
  return { bg: "rgba(99,102,241,0.08)", color: "var(--tf-text2)" };
}

export function ProfileActivity() {
  const [page, setPage] = useState(1);
  const pageSize = 20;
  const { data, isLoading, isError } = useActivityLog(page, pageSize);

  if (isLoading) {
    return (
      <div style={{ color: "var(--tf-text3)", fontSize: 13, padding: "24px 0" }}>
        Loading activity...
      </div>
    );
  }

  if (isError) {
    return (
      <div
        role="alert"
        style={{
          color: "#ef4444",
          fontSize: 13,
          padding: "12px 16px",
          background: "rgba(239,68,68,0.08)",
          border: "1px solid rgba(239,68,68,0.2)",
          borderRadius: 8,
        }}
      >
        Failed to load activity log. Please try again.
      </div>
    );
  }

  if (!data || data.items.length === 0) {
    return (
      <div
        style={{
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          padding: "48px 24px",
          color: "var(--tf-text3)",
          gap: 8,
        }}
      >
        <span style={{ fontSize: 32 }} aria-hidden="true">
          📋
        </span>
        <p style={{ fontSize: 14, margin: 0, fontWeight: 500, color: "var(--tf-text2)" }}>
          No activity yet
        </p>
        <p style={{ fontSize: 13, margin: 0 }}>
          Actions you take on work items will appear here.
        </p>
      </div>
    );
  }

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
      <ul
        aria-label="Activity log"
        style={{
          listStyle: "none",
          padding: 0,
          margin: 0,
          display: "flex",
          flexDirection: "column",
          gap: 8,
        }}
      >
        {data.items.map((item) => {
          const badge = actionBadgeColor(item.actionType);
          return (
            <li
              key={item.id}
              style={{
                display: "flex",
                alignItems: "flex-start",
                gap: 12,
                padding: "12px 14px",
                background: "var(--tf-bg2)",
                border: "1px solid var(--tf-border)",
                borderRadius: 8,
                flexWrap: "wrap",
              }}
            >
              {/* Action badge */}
              <span
                aria-label={`Action: ${item.actionType}`}
                style={{
                  fontSize: 11,
                  fontWeight: 600,
                  padding: "3px 8px",
                  borderRadius: 99,
                  background: badge.bg,
                  color: badge.color,
                  flexShrink: 0,
                  marginTop: 1,
                  letterSpacing: "0.03em",
                  textTransform: "capitalize",
                  whiteSpace: "nowrap",
                }}
              >
                {item.actionType}
              </span>

              {/* Content */}
              <div style={{ flex: 1, minWidth: 0 }}>
                <div
                  style={{
                    fontSize: 13,
                    fontWeight: 600,
                    color: "var(--tf-text)",
                    marginBottom: item.fieldName ? 4 : 0,
                    overflow: "hidden",
                    textOverflow: "ellipsis",
                    whiteSpace: "nowrap",
                  }}
                  title={item.workItemTitle}
                >
                  {item.workItemTitle}
                </div>
                {item.fieldName && (
                  <div style={{ fontSize: 12, color: "var(--tf-text3)" }}>
                    <span style={{ fontWeight: 500 }}>{item.fieldName}</span>
                    {item.oldValue !== null && item.newValue !== null && (
                      <>
                        {": "}
                        <span
                          style={{
                            background: "rgba(239,68,68,0.08)",
                            color: "#ef4444",
                            padding: "0 4px",
                            borderRadius: 3,
                          }}
                        >
                          {item.oldValue}
                        </span>
                        <span style={{ margin: "0 4px" }}>&#8594;</span>
                        <span
                          style={{
                            background: "rgba(34,197,94,0.08)",
                            color: "#22c55e",
                            padding: "0 4px",
                            borderRadius: 3,
                          }}
                        >
                          {item.newValue}
                        </span>
                      </>
                    )}
                  </div>
                )}
              </div>

              {/* Timestamp */}
              <time
                dateTime={item.createdAt}
                title={new Date(item.createdAt).toLocaleString()}
                style={{
                  fontSize: 11,
                  color: "var(--tf-text3)",
                  flexShrink: 0,
                  whiteSpace: "nowrap",
                }}
              >
                {formatRelativeTime(item.createdAt)}
              </time>
            </li>
          );
        })}
      </ul>

      {/* Pagination */}
      {data.totalPages > 1 && (
        <nav
          aria-label="Activity log pagination"
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            paddingTop: 8,
          }}
        >
          <button
            onClick={() => setPage((p) => p - 1)}
            disabled={!data.hasPreviousPage}
            aria-label="Previous page"
            style={{
              display: "flex",
              alignItems: "center",
              gap: 4,
              padding: "7px 14px",
              borderRadius: 6,
              border: "1px solid var(--tf-border)",
              background: "transparent",
              color: data.hasPreviousPage ? "var(--tf-text2)" : "var(--tf-text3)",
              fontSize: 13,
              cursor: data.hasPreviousPage ? "pointer" : "not-allowed",
              opacity: data.hasPreviousPage ? 1 : 0.4,
              minHeight: 44,
            }}
          >
            <ChevronLeft size={14} />
            Previous
          </button>

          <span style={{ fontSize: 12, color: "var(--tf-text3)" }}>
            Page {data.page} of {data.totalPages}
          </span>

          <button
            onClick={() => setPage((p) => p + 1)}
            disabled={!data.hasNextPage}
            aria-label="Next page"
            style={{
              display: "flex",
              alignItems: "center",
              gap: 4,
              padding: "7px 14px",
              borderRadius: 6,
              border: "1px solid var(--tf-border)",
              background: "transparent",
              color: data.hasNextPage ? "var(--tf-text2)" : "var(--tf-text3)",
              fontSize: 13,
              cursor: data.hasNextPage ? "pointer" : "not-allowed",
              opacity: data.hasNextPage ? 1 : 0.4,
              minHeight: 44,
            }}
          >
            Next
            <ChevronRight size={14} />
          </button>
        </nav>
      )}
    </div>
  );
}
