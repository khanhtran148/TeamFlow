"use client";

import { ChevronLeft, ChevronRight } from "lucide-react";

interface PaginationControlsProps {
  page: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  onPageChange: (page: number) => void;
  totalCount: number;
  pageSize: number;
}

export function PaginationControls({
  page,
  totalPages,
  hasNextPage,
  hasPreviousPage,
  onPageChange,
  totalCount,
  pageSize,
}: PaginationControlsProps) {
  const startItem = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const endItem = Math.min(page * pageSize, totalCount);

  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        padding: "12px 16px",
        borderTop: "1px solid var(--tf-border)",
      }}
    >
      <span
        style={{
          fontSize: 12,
          color: "var(--tf-text3)",
          fontFamily: "var(--tf-font-mono)",
        }}
      >
        {totalCount === 0
          ? "No results"
          : `${startItem}–${endItem} of ${totalCount}`}
      </span>

      <div style={{ display: "flex", alignItems: "center", gap: 4 }}>
        <button
          type="button"
          onClick={() => onPageChange(page - 1)}
          disabled={!hasPreviousPage}
          aria-label="Previous page"
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            width: 30,
            height: 30,
            borderRadius: 6,
            border: "1px solid var(--tf-border)",
            background: "transparent",
            color: hasPreviousPage ? "var(--tf-text2)" : "var(--tf-text3)",
            cursor: hasPreviousPage ? "pointer" : "not-allowed",
            opacity: hasPreviousPage ? 1 : 0.4,
            transition: "border-color 0.15s",
          }}
          onMouseEnter={(e) => {
            if (hasPreviousPage) {
              (e.currentTarget as HTMLButtonElement).style.borderColor =
                "var(--tf-border2)";
            }
          }}
          onMouseLeave={(e) => {
            (e.currentTarget as HTMLButtonElement).style.borderColor =
              "var(--tf-border)";
          }}
        >
          <ChevronLeft size={14} />
        </button>

        <span
          style={{
            fontSize: 12,
            color: "var(--tf-text2)",
            fontFamily: "var(--tf-font-mono)",
            padding: "0 8px",
            minWidth: 60,
            textAlign: "center",
          }}
        >
          {totalPages === 0 ? "0 / 0" : `${page} / ${totalPages}`}
        </span>

        <button
          type="button"
          onClick={() => onPageChange(page + 1)}
          disabled={!hasNextPage}
          aria-label="Next page"
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            width: 30,
            height: 30,
            borderRadius: 6,
            border: "1px solid var(--tf-border)",
            background: "transparent",
            color: hasNextPage ? "var(--tf-text2)" : "var(--tf-text3)",
            cursor: hasNextPage ? "pointer" : "not-allowed",
            opacity: hasNextPage ? 1 : 0.4,
            transition: "border-color 0.15s",
          }}
          onMouseEnter={(e) => {
            if (hasNextPage) {
              (e.currentTarget as HTMLButtonElement).style.borderColor =
                "var(--tf-border2)";
            }
          }}
          onMouseLeave={(e) => {
            (e.currentTarget as HTMLButtonElement).style.borderColor =
              "var(--tf-border)";
          }}
        >
          <ChevronRight size={14} />
        </button>
      </div>
    </div>
  );
}
