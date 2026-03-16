"use client";

import { useState } from "react";
import { MoreHorizontal, Pencil, Trash2, Calendar, Zap } from "lucide-react";
import { useRouter } from "next/navigation";
import { SprintStatusBadge } from "./sprint-status-badge";
import type { SprintDto } from "@/lib/api/types";

interface SprintCardProps {
  sprint: SprintDto;
  projectId: string;
  onEdit: (sprint: SprintDto) => void;
  onDelete: (sprint: SprintDto) => void;
}

function formatSprintDate(dateStr: string | null): string {
  if (!dateStr) return "No date set";
  const d = new Date(dateStr);
  return d.toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" });
}

export function SprintCard({ sprint, projectId, onEdit, onDelete }: SprintCardProps) {
  const router = useRouter();
  const [menuOpen, setMenuOpen] = useState(false);

  const progressPct =
    sprint.totalPoints > 0
      ? Math.round((sprint.completedPoints / sprint.totalPoints) * 100)
      : 0;

  function handleCardClick() {
    router.push(`/projects/${projectId}/sprints/${sprint.id}`);
  }

  function handleMenuToggle(e: React.MouseEvent) {
    e.stopPropagation();
    setMenuOpen((prev) => !prev);
  }

  function handleEdit(e: React.MouseEvent) {
    e.stopPropagation();
    setMenuOpen(false);
    onEdit(sprint);
  }

  function handleDelete(e: React.MouseEvent) {
    e.stopPropagation();
    setMenuOpen(false);
    onDelete(sprint);
  }

  return (
    <div
      data-testid={`sprint-card-${sprint.id}`}
      onClick={handleCardClick}
      role="article"
      tabIndex={0}
      aria-label={`Sprint: ${sprint.name}`}
      onKeyDown={(e) => {
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault();
          handleCardClick();
        }
      }}
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
        <div
          style={{
            width: 28,
            height: 28,
            borderRadius: 6,
            background: "var(--tf-blue-dim)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            flexShrink: 0,
            marginTop: 1,
          }}
        >
          <Zap size={13} color="var(--tf-blue)" />
        </div>

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
              {sprint.name}
            </span>
            <SprintStatusBadge status={sprint.status} />
          </div>

          {sprint.goal && (
            <p
              style={{
                fontSize: 13,
                color: "var(--tf-text3)",
                marginTop: 3,
                lineHeight: 1.5,
                overflow: "hidden",
                display: "-webkit-box",
                WebkitLineClamp: 2,
                WebkitBoxOrient: "vertical",
              }}
            >
              {sprint.goal}
            </p>
          )}
        </div>

        {/* Menu button */}
        <div style={{ position: "relative", flexShrink: 0 }}>
          <button
            onClick={handleMenuToggle}
            aria-label="Sprint actions"
            aria-expanded={menuOpen}
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
                role="menu"
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
                <button
                  role="menuitem"
                  onClick={handleEdit}
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: 8,
                    width: "100%",
                    padding: "7px 12px",
                    background: "transparent",
                    border: "none",
                    cursor: "pointer",
                    fontSize: 13,
                    color: "var(--tf-text2)",
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
                  <Pencil size={12} />
                  Edit
                </button>
                <button
                  role="menuitem"
                  onClick={handleDelete}
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: 8,
                    width: "100%",
                    padding: "7px 12px",
                    background: "transparent",
                    border: "none",
                    cursor: "pointer",
                    fontSize: 13,
                    color: "var(--tf-red)",
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
                  <Trash2 size={12} />
                  Delete
                </button>
              </div>
            </>
          )}
        </div>
      </div>

      {/* Progress bar */}
      {sprint.totalPoints > 0 && (
        <div style={{ display: "flex", flexDirection: "column", gap: 5 }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
            <span style={{ fontSize: 13, color: "var(--tf-text3)", fontFamily: "var(--tf-font-mono)" }}>
              Progress
            </span>
            <span style={{ fontSize: 13, color: "var(--tf-text2)", fontFamily: "var(--tf-font-mono)" }}>
              {sprint.completedPoints}/{sprint.totalPoints} pts ({progressPct}%)
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
                  sprint.status === "Completed"
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
            fontSize: 13,
            color: "var(--tf-text3)",
            fontFamily: "var(--tf-font-mono)",
          }}
        >
          <Calendar size={11} />
          {formatSprintDate(sprint.startDate)}
          {sprint.endDate && ` - ${formatSprintDate(sprint.endDate)}`}
        </span>

        <span style={{ fontSize: 13, color: "var(--tf-text3)", fontFamily: "var(--tf-font-mono)" }}>
          {sprint.itemCount} item{sprint.itemCount !== 1 ? "s" : ""}
        </span>

        {sprint.capacityUtilization !== null && (
          <span style={{ fontSize: 13, color: "var(--tf-text3)", fontFamily: "var(--tf-font-mono)" }}>
            {Math.round(sprint.capacityUtilization * 100)}% capacity
          </span>
        )}
      </div>
    </div>
  );
}
