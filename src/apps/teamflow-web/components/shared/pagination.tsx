"use client";

import { ChevronLeft, ChevronRight } from "lucide-react";

interface PaginationProps {
  page: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
}

export function Pagination({ page, pageSize, totalCount, onPageChange }: PaginationProps) {
  const totalPages = Math.ceil(totalCount / pageSize);
  if (totalPages <= 1) return null;

  const canPrev = page > 1;
  const canNext = page < totalPages;

  const from = (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, totalCount);

  return (
    <div
      data-testid="pagination"
      style={{
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        padding: "10px 16px",
        borderTop: "1px solid var(--tf-border)",
        background: "var(--tf-bg2)",
      }}
    >
      <span
        style={{
          fontSize: 13,
          color: "var(--tf-text3)",
          fontFamily: "var(--tf-font-mono)",
        }}
      >
        {from}–{to} of {totalCount}
      </span>

      <div style={{ display: "flex", gap: 4 }}>
        <NavButton
          data-testid="page-prev"
          onClick={() => onPageChange(page - 1)}
          disabled={!canPrev}
          aria-label="Previous page"
        >
          <ChevronLeft size={13} />
        </NavButton>

        {buildPageRange(page, totalPages).map((p, i) =>
          p === "..." ? (
            <span
              key={`dots-${i}`}
              style={{
                padding: "3px 6px",
                fontSize: 13,
                color: "var(--tf-text3)",
                fontFamily: "var(--tf-font-mono)",
              }}
            >
              …
            </span>
          ) : (
            <NavButton
              key={p}
              onClick={() => onPageChange(p as number)}
              active={p === page}
            >
              {p}
            </NavButton>
          )
        )}

        <NavButton
          data-testid="page-next"
          onClick={() => onPageChange(page + 1)}
          disabled={!canNext}
          aria-label="Next page"
        >
          <ChevronRight size={13} />
        </NavButton>
      </div>
    </div>
  );
}

function NavButton({
  children,
  onClick,
  disabled,
  active,
  "aria-label": ariaLabel,
  "data-testid": testId,
}: {
  children: React.ReactNode;
  onClick: () => void;
  disabled?: boolean;
  active?: boolean;
  "aria-label"?: string;
  "data-testid"?: string;
}) {
  return (
    <button
      data-testid={testId}
      onClick={onClick}
      disabled={disabled}
      aria-label={ariaLabel}
      style={{
        minWidth: 26,
        height: 26,
        padding: "0 6px",
        borderRadius: 5,
        border: `1px solid ${active ? "var(--tf-accent)" : "var(--tf-border)"}`,
        background: active ? "var(--tf-accent-dim2)" : "var(--tf-bg3)",
        color: active ? "var(--tf-accent)" : disabled ? "var(--tf-text3)" : "var(--tf-text2)",
        fontSize: 13,
        fontFamily: "var(--tf-font-mono)",
        cursor: disabled ? "not-allowed" : "pointer",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        transition: "all var(--tf-tr)",
        opacity: disabled ? 0.5 : 1,
      }}
    >
      {children}
    </button>
  );
}

function buildPageRange(current: number, total: number): (number | "...")[] {
  if (total <= 7) {
    return Array.from({ length: total }, (_, i) => i + 1);
  }

  const result: (number | "...")[] = [];
  result.push(1);

  if (current > 3) result.push("...");

  const start = Math.max(2, current - 1);
  const end = Math.min(total - 1, current + 1);
  for (let i = start; i <= end; i++) result.push(i);

  if (current < total - 2) result.push("...");

  result.push(total);
  return result;
}
