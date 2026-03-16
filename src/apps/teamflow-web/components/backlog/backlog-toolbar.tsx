"use client";

import { Search, X, LayoutList, AlignJustify, Plus } from "lucide-react";
import { useBacklogFilterStore } from "@/lib/stores/backlog-filter-store";
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

interface BacklogToolbarProps {
  onNewItem: () => void;
}

export function BacklogToolbar({ onNewItem }: BacklogToolbarProps) {
  const {
    filters,
    setSearch,
    setType,
    setPriority,
    setBlockedOnly,
    setReadyOnly,
    setViewMode,
    resetFilters,
  } = useBacklogFilterStore();

  const hasActiveFilters =
    !!filters.type ||
    !!filters.priority ||
    !!filters.assigneeId ||
    !!filters.releaseId ||
    filters.blockedOnly ||
    filters.readyOnly;

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
      {/* Search */}
      <div
        className="tf-search-focus"
        style={{
          display: "flex",
          alignItems: "center",
          gap: 6,
          background: "var(--tf-bg4)",
          border: "1px solid var(--tf-border)",
          borderRadius: 6,
          padding: "4px 10px",
          minWidth: 200,
          transition: "border-color var(--tf-tr), box-shadow var(--tf-tr)",
        }}
      >
        <Search size={13} style={{ color: "var(--tf-text3)", flexShrink: 0 }} />
        <input
          type="text"
          placeholder="Search backlog…"
          value={filters.search}
          onChange={(e) => setSearch(e.target.value)}
          style={{
            background: "transparent",
            border: "none",
            outline: "none",
            fontSize: 13,
            color: "var(--tf-text)",
            fontFamily: "var(--tf-font-body)",
            width: "100%",
          }}
        />
        {filters.search && (
          <button
            onClick={() => setSearch("")}
            style={{
              background: "none",
              border: "none",
              cursor: "pointer",
              padding: 0,
              color: "var(--tf-text3)",
              display: "flex",
              alignItems: "center",
            }}
          >
            <X size={12} />
          </button>
        )}
      </div>

      {/* Separator */}
      <div
        style={{
          width: 1,
          height: 20,
          background: "var(--tf-border)",
          flexShrink: 0,
        }}
      />

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

      {/* Blocked only chip */}
      <FilterChip
        label="Blocked"
        active={filters.blockedOnly}
        color="var(--tf-red)"
        onClick={() => setBlockedOnly(!filters.blockedOnly)}
      />

      {/* Ready only chip */}
      <FilterChip
        label="Ready"
        active={filters.readyOnly}
        color="var(--tf-accent)"
        onClick={() => setReadyOnly(!filters.readyOnly)}
      />

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

      {/* View mode toggle */}
      <div
        style={{
          display: "flex",
          borderRadius: 6,
          overflow: "hidden",
          border: "1px solid var(--tf-border)",
        }}
      >
        <button
          onClick={() => setViewMode("grouped")}
          title="Grouped by Epic"
          style={{
            padding: "4px 8px",
            background:
              filters.viewMode === "grouped"
                ? "var(--tf-accent-dim2)"
                : "transparent",
            border: "none",
            color:
              filters.viewMode === "grouped"
                ? "var(--tf-accent)"
                : "var(--tf-text3)",
            cursor: "pointer",
            display: "flex",
            alignItems: "center",
          }}
        >
          <LayoutList size={14} />
        </button>
        <button
          onClick={() => setViewMode("flat")}
          title="Flat list"
          style={{
            padding: "4px 8px",
            background:
              filters.viewMode === "flat"
                ? "var(--tf-accent-dim2)"
                : "transparent",
            border: "none",
            borderLeft: "1px solid var(--tf-border)",
            color:
              filters.viewMode === "flat"
                ? "var(--tf-accent)"
                : "var(--tf-text3)",
            cursor: "pointer",
            display: "flex",
            alignItems: "center",
          }}
        >
          <AlignJustify size={14} />
        </button>
      </div>

      {/* New item */}
      <button
        onClick={onNewItem}
        style={{
          display: "inline-flex",
          alignItems: "center",
          gap: 6,
          padding: "5px 14px",
          borderRadius: 6,
          fontSize: 13,
          fontWeight: 600,
          border: "none",
          background: "var(--tf-accent)",
          color: "var(--tf-bg)",
          cursor: "pointer",
          fontFamily: "var(--tf-font-body)",
          transition: "opacity var(--tf-tr)",
        }}
      >
        <Plus size={13} />
        New Item
      </button>
    </div>
  );
}
