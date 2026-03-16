"use client";

import { X } from "lucide-react";
import {
  useKanbanFilterStore,
  type KanbanSwimlane,
} from "@/lib/stores/kanban-filter-store";
import type { WorkItemType, Priority } from "@/lib/api/types";

const TYPE_OPTIONS: { value: WorkItemType; label: string; color: string }[] = [
  { value: "Epic", label: "Epic", color: "var(--tf-orange)" },
  { value: "UserStory", label: "Story", color: "var(--tf-blue)" },
  { value: "Task", label: "Task", color: "var(--tf-accent)" },
  { value: "Bug", label: "Bug", color: "var(--tf-red)" },
  { value: "Spike", label: "Spike", color: "var(--tf-violet)" },
];

const PRIORITY_OPTIONS: { value: Priority; label: string; color: string }[] = [
  { value: "Critical", label: "Critical", color: "var(--tf-red)" },
  { value: "High", label: "High", color: "var(--tf-orange)" },
  { value: "Medium", label: "Medium", color: "var(--tf-yellow)" },
  { value: "Low", label: "Low", color: "var(--tf-text3)" },
];

const SWIMLANE_OPTIONS: { value: KanbanSwimlane; label: string }[] = [
  { value: "none", label: "None" },
  { value: "assignee", label: "By Assignee" },
  { value: "epic", label: "By Epic" },
];

interface FilterChipProps {
  label: string;
  active: boolean;
  color?: string;
  onClick: () => void;
}

function FilterChip({ label, active, color, onClick }: FilterChipProps) {
  return (
    <button
      onClick={onClick}
      style={{
        display: "inline-flex",
        alignItems: "center",
        gap: 4,
        padding: "3px 10px",
        borderRadius: 100,
        fontSize: 13,
        fontWeight: 500,
        fontFamily: "var(--tf-font-body)",
        border: `1px solid ${active ? (color ?? "var(--tf-accent)") : "var(--tf-border)"}`,
        background: active ? "var(--tf-accent-dim2)" : "transparent",
        color: active ? (color ?? "var(--tf-accent)") : "var(--tf-text2)",
        cursor: "pointer",
        transition: "all var(--tf-tr)",
        whiteSpace: "nowrap",
      }}
    >
      {active && color && (
        <span
          style={{
            width: 6,
            height: 6,
            borderRadius: "50%",
            background: color,
            flexShrink: 0,
          }}
        />
      )}
      {label}
    </button>
  );
}

export function KanbanToolbar() {
  const {
    filters,
    setType,
    setPriority,
    setSwimlane,
    resetFilters,
  } = useKanbanFilterStore();

  const hasActiveFilters =
    !!filters.type || !!filters.priority || !!filters.assigneeId || !!filters.releaseId;

  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        gap: 8,
        padding: "10px 20px",
        borderBottom: "1px solid var(--tf-border)",
        background: "var(--tf-bg2)",
        flexWrap: "wrap",
      }}
    >
      {/* Type filter chips */}
      {TYPE_OPTIONS.map((opt) => (
        <FilterChip
          key={opt.value}
          label={opt.label}
          active={filters.type === opt.value}
          color={opt.color}
          onClick={() => setType(filters.type === opt.value ? "" : opt.value)}
        />
      ))}

      {/* Separator */}
      <div
        style={{
          width: 1,
          height: 20,
          background: "var(--tf-border)",
          flexShrink: 0,
        }}
      />

      {/* Priority filter chips */}
      {PRIORITY_OPTIONS.map((opt) => (
        <FilterChip
          key={opt.value}
          label={opt.label}
          active={filters.priority === opt.value}
          color={opt.color}
          onClick={() =>
            setPriority(filters.priority === opt.value ? "" : opt.value)
          }
        />
      ))}

      {/* Clear filters */}
      {hasActiveFilters && (
        <button
          onClick={resetFilters}
          style={{
            display: "inline-flex",
            alignItems: "center",
            gap: 3,
            padding: "3px 8px",
            borderRadius: 100,
            fontSize: 13,
            fontWeight: 500,
            border: "none",
            background: "none",
            color: "var(--tf-text3)",
            cursor: "pointer",
          }}
        >
          <X size={10} />
          Clear
        </button>
      )}

      {/* Spacer */}
      <div style={{ flex: 1 }} />

      {/* Swimlane toggle */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 6,
          fontSize: 13,
          color: "var(--tf-text2)",
          fontFamily: "var(--tf-font-body)",
        }}
      >
        <span style={{ color: "var(--tf-text3)" }}>Swimlane:</span>
        <div
          style={{
            display: "flex",
            borderRadius: 6,
            overflow: "hidden",
            border: "1px solid var(--tf-border)",
          }}
        >
          {SWIMLANE_OPTIONS.map((opt, i) => (
            <button
              key={opt.value}
              onClick={() => setSwimlane(opt.value)}
              style={{
                padding: "4px 10px",
                fontSize: 13,
                fontWeight: 500,
                fontFamily: "var(--tf-font-body)",
                background:
                  filters.swimlane === opt.value
                    ? "var(--tf-accent-dim2)"
                    : "transparent",
                border: "none",
                borderLeft: i > 0 ? "1px solid var(--tf-border)" : "none",
                color:
                  filters.swimlane === opt.value
                    ? "var(--tf-accent)"
                    : "var(--tf-text2)",
                cursor: "pointer",
                transition: "all var(--tf-tr)",
                whiteSpace: "nowrap",
              }}
            >
              {opt.label}
            </button>
          ))}
        </div>
      </div>
    </div>
  );
}
