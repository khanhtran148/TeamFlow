"use client";

import { useState } from "react";
import { MoreHorizontal, Pencil, Trash2, Calendar, Package } from "lucide-react";
import { useRouter } from "next/navigation";
import type { ReleaseDto, ReleaseStatus, WorkItemStatus } from "@/lib/api/types";

interface ReleaseCardProps {
  release: ReleaseDto;
  projectId: string;
  onEdit: (release: ReleaseDto) => void;
  onDelete: (release: ReleaseDto) => void;
}

const STATUS_CONFIG: Record<
  ReleaseStatus,
  { label: string; bg: string; color: string; border: string }
> = {
  Unreleased: {
    label: "Unreleased",
    bg: "var(--tf-accent-dim2)",
    color: "var(--tf-accent)",
    border: "var(--tf-accent)",
  },
  Overdue: {
    label: "Overdue",
    bg: "var(--tf-red-dim)",
    color: "var(--tf-red)",
    border: "var(--tf-red-dim)",
  },
  Released: {
    label: "Released",
    bg: "var(--tf-bg4)",
    color: "var(--tf-text3)",
    border: "var(--tf-border)",
  },
};

function ReleaseStatusBadge({ status }: { status: ReleaseStatus }) {
  const config = STATUS_CONFIG[status];
  return (
    <span
      style={{
        display: "inline-flex",
        alignItems: "center",
        padding: "2px 8px",
        borderRadius: 100,
        fontSize: 10,
        fontWeight: 600,
        fontFamily: "var(--tf-font-mono)",
        background: config.bg,
        color: config.color,
        border: `1px solid ${config.border}`,
        whiteSpace: "nowrap",
      }}
    >
      {config.label}
    </span>
  );
}

function formatReleaseDate(dateStr: string | null): string {
  if (!dateStr) return "No date set";
  const d = new Date(dateStr);
  return d.toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" });
}

function computeProgress(release: ReleaseDto): { done: number; total: number } {
  const total = release.totalItems;
  const done = (release.itemCountsByStatus as Record<string, number>)["Done"] ?? 0;
  return { done, total };
}

function StatusBreakdown({
  counts,
}: {
  counts: Partial<Record<WorkItemStatus, number>>;
}) {
  const statusOrder: WorkItemStatus[] = ["ToDo", "InProgress", "InReview", "Done", "Rejected"];
  const entries = statusOrder
    .map((s) => ({ status: s, count: counts[s] ?? 0 }))
    .filter((e) => e.count > 0);

  if (entries.length === 0) return null;

  return (
    <div style={{ display: "flex", alignItems: "center", gap: 8, flexWrap: "wrap" }}>
      {entries.map(({ status, count }) => (
        <span
          key={status}
          style={{
            fontSize: 10,
            color: "var(--tf-text3)",
            fontFamily: "var(--tf-font-mono)",
          }}
        >
          {status === "ToDo"
            ? "To Do"
            : status === "InProgress"
              ? "In Progress"
              : status === "InReview"
                ? "In Review"
                : status === "NeedsClarification"
                  ? "Needs Clarification"
                  : status}
          : {count}
        </span>
      ))}
    </div>
  );
}

export function ReleaseCard({ release, projectId, onEdit, onDelete }: ReleaseCardProps) {
  const router = useRouter();
  const [menuOpen, setMenuOpen] = useState(false);
  const { done, total } = computeProgress(release);
  const progressPct = total > 0 ? Math.round((done / total) * 100) : 0;

  function handleCardClick() {
    router.push(`/projects/${projectId}/releases/${release.id}`);
  }

  function handleMenuToggle(e: React.MouseEvent) {
    e.stopPropagation();
    setMenuOpen((prev) => !prev);
  }

  function handleEdit(e: React.MouseEvent) {
    e.stopPropagation();
    setMenuOpen(false);
    onEdit(release);
  }

  function handleDelete(e: React.MouseEvent) {
    e.stopPropagation();
    setMenuOpen(false);
    onDelete(release);
  }

  return (
    <div
      onClick={handleCardClick}
      style={{
        background: "var(--tf-bg2)",
        border: "1px solid var(--tf-border)",
        borderRadius: "var(--tf-radius)",
        padding: "14px 16px",
        cursor: "pointer",
        position: "relative",
        transition: "border-color var(--tf-tr), background var(--tf-tr)",
        display: "flex",
        flexDirection: "column",
        gap: 10,
      }}
      onMouseEnter={(e) => {
        (e.currentTarget as HTMLDivElement).style.borderColor = "var(--tf-border2)";
        (e.currentTarget as HTMLDivElement).style.background = "var(--tf-bg3)";
      }}
      onMouseLeave={(e) => {
        (e.currentTarget as HTMLDivElement).style.borderColor = "var(--tf-border)";
        (e.currentTarget as HTMLDivElement).style.background = "var(--tf-bg2)";
      }}
    >
      {/* Header row */}
      <div style={{ display: "flex", alignItems: "flex-start", gap: 8 }}>
        {/* Icon */}
        <div
          style={{
            width: 28,
            height: 28,
            borderRadius: 6,
            background: "var(--tf-violet-dim)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            flexShrink: 0,
            marginTop: 1,
          }}
        >
          <Package size={13} color="var(--tf-violet)" />
        </div>

        {/* Name + status */}
        <div style={{ flex: 1, minWidth: 0 }}>
          <div style={{ display: "flex", alignItems: "center", gap: 8, flexWrap: "wrap" }}>
            <span
              style={{
                fontFamily: "var(--tf-font-head)",
                fontWeight: 600,
                fontSize: 14,
                color: "var(--tf-text)",
                lineHeight: 1.3,
              }}
            >
              {release.name}
            </span>
            <ReleaseStatusBadge status={release.status} />
          </div>

          {release.description && (
            <p
              style={{
                fontSize: 12,
                color: "var(--tf-text3)",
                marginTop: 3,
                lineHeight: 1.5,
                overflow: "hidden",
                display: "-webkit-box",
                WebkitLineClamp: 2,
                WebkitBoxOrient: "vertical",
              }}
            >
              {release.description}
            </p>
          )}
        </div>

        {/* Menu button */}
        <div style={{ position: "relative", flexShrink: 0 }}>
          <button
            onClick={handleMenuToggle}
            style={{
              width: 26,
              height: 26,
              borderRadius: 5,
              border: "1px solid var(--tf-border)",
              background: "transparent",
              color: "var(--tf-text3)",
              cursor: "pointer",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              transition: "all var(--tf-tr)",
            }}
            onMouseEnter={(e) => {
              (e.currentTarget as HTMLButtonElement).style.background = "var(--tf-bg4)";
              (e.currentTarget as HTMLButtonElement).style.color = "var(--tf-text)";
            }}
            onMouseLeave={(e) => {
              (e.currentTarget as HTMLButtonElement).style.background = "transparent";
              (e.currentTarget as HTMLButtonElement).style.color = "var(--tf-text3)";
            }}
          >
            <MoreHorizontal size={13} />
          </button>

          {menuOpen && (
            <>
              <div
                style={{ position: "fixed", inset: 0, zIndex: 9 }}
                onClick={(e) => {
                  e.stopPropagation();
                  setMenuOpen(false);
                }}
              />
              <div
                style={{
                  position: "absolute",
                  top: 30,
                  right: 0,
                  zIndex: 10,
                  background: "var(--tf-bg3)",
                  border: "1px solid var(--tf-border)",
                  borderRadius: "var(--tf-radius)",
                  boxShadow: "var(--tf-shadow)",
                  minWidth: 130,
                  overflow: "hidden",
                }}
              >
                <MenuItemButton icon={<Pencil size={12} />} label="Edit" onClick={handleEdit} />
                <MenuItemButton
                  icon={<Trash2 size={12} />}
                  label="Delete"
                  onClick={handleDelete}
                  destructive
                />
              </div>
            </>
          )}
        </div>
      </div>

      {/* Progress bar */}
      {total > 0 && (
        <div style={{ display: "flex", flexDirection: "column", gap: 5 }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
            <span style={{ fontSize: 10, color: "var(--tf-text3)", fontFamily: "var(--tf-font-mono)" }}>
              Progress
            </span>
            <span style={{ fontSize: 10, color: "var(--tf-text2)", fontFamily: "var(--tf-font-mono)" }}>
              {done}/{total} ({progressPct}%)
            </span>
          </div>
          <div
            style={{
              height: 4,
              borderRadius: 100,
              background: "var(--tf-bg4)",
              overflow: "hidden",
            }}
          >
            <div
              style={{
                height: "100%",
                width: `${progressPct}%`,
                borderRadius: 100,
                background:
                  release.status === "Overdue"
                    ? "var(--tf-red)"
                    : release.status === "Released"
                      ? "var(--tf-text3)"
                      : "var(--tf-accent)",
                transition: "width 0.3s ease",
              }}
            />
          </div>
        </div>
      )}

      {/* Footer row */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 12,
          paddingTop: 6,
          borderTop: "1px solid var(--tf-border)",
          flexWrap: "wrap",
        }}
      >
        <span
          style={{
            display: "inline-flex",
            alignItems: "center",
            gap: 4,
            fontSize: 11,
            color: "var(--tf-text3)",
            fontFamily: "var(--tf-font-mono)",
          }}
        >
          <Calendar size={11} />
          {formatReleaseDate(release.releaseDate)}
        </span>

        {total > 0 && (
          <StatusBreakdown counts={release.itemCountsByStatus} />
        )}

        {total === 0 && (
          <span style={{ fontSize: 11, color: "var(--tf-text3)", fontFamily: "var(--tf-font-mono)" }}>
            No items assigned
          </span>
        )}
      </div>
    </div>
  );
}

function MenuItemButton({
  icon,
  label,
  onClick,
  destructive,
}: {
  icon: React.ReactNode;
  label: string;
  onClick: (e: React.MouseEvent) => void;
  destructive?: boolean;
}) {
  return (
    <button
      onClick={onClick}
      style={{
        display: "flex",
        alignItems: "center",
        gap: 8,
        width: "100%",
        padding: "7px 12px",
        background: "transparent",
        border: "none",
        cursor: "pointer",
        fontSize: 12,
        color: destructive ? "var(--tf-red)" : "var(--tf-text2)",
        textAlign: "left",
        transition: "background var(--tf-tr)",
      }}
      onMouseEnter={(e) => {
        (e.currentTarget as HTMLButtonElement).style.background = "var(--tf-bg4)";
      }}
      onMouseLeave={(e) => {
        (e.currentTarget as HTMLButtonElement).style.background = "transparent";
      }}
    >
      {icon}
      {label}
    </button>
  );
}
