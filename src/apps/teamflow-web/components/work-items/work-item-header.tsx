"use client";

import { useState, useRef, useEffect } from "react";
import { Check, X } from "lucide-react";
import { TypeIcon } from "@/components/shared/type-icon";
import { StatusBadge } from "@/components/shared/status-badge";
import type { WorkItemDto } from "@/lib/api/types";

interface WorkItemHeaderProps {
  workItem: WorkItemDto;
  onTitleSave: (newTitle: string) => Promise<void>;
  isSaving?: boolean;
}

export function WorkItemHeader({
  workItem,
  onTitleSave,
  isSaving,
}: WorkItemHeaderProps) {
  const [editingTitle, setEditingTitle] = useState(false);
  const [draftTitle, setDraftTitle] = useState(workItem.title);
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (editingTitle) {
      inputRef.current?.focus();
      inputRef.current?.select();
    }
  }, [editingTitle]);

  // Keep draft in sync if parent updates the item
  useEffect(() => {
    if (!editingTitle) {
      setDraftTitle(workItem.title);
    }
  }, [workItem.title, editingTitle]);

  async function handleSaveTitle() {
    const trimmed = draftTitle.trim();
    if (!trimmed || trimmed === workItem.title) {
      setDraftTitle(workItem.title);
      setEditingTitle(false);
      return;
    }
    await onTitleSave(trimmed);
    setEditingTitle(false);
  }

  function handleKeyDown(e: React.KeyboardEvent) {
    if (e.key === "Enter") {
      e.preventDefault();
      void handleSaveTitle();
    }
    if (e.key === "Escape") {
      setDraftTitle(workItem.title);
      setEditingTitle(false);
    }
  }

  return (
    <div style={{ marginBottom: 20 }}>
      {/* Type + ID row */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 8,
          marginBottom: 10,
        }}
      >
        <TypeIcon type={workItem.type} size={20} />
        <span
          style={{
            fontFamily: "var(--tf-font-mono)",
            fontSize: 11,
            color: "var(--tf-text3)",
          }}
        >
          #{workItem.id.slice(0, 8)}
        </span>
        <span
          style={{
            fontSize: 11,
            color: "var(--tf-text3)",
            fontFamily: "var(--tf-font-body)",
          }}
        >
          {workItem.type === "UserStory" ? "User Story" : workItem.type}
        </span>
        <StatusBadge status={workItem.status} size="sm" />
      </div>

      {/* Title row */}
      {editingTitle ? (
        <div style={{ display: "flex", alignItems: "center", gap: 6 }}>
          <input
            ref={inputRef}
            value={draftTitle}
            onChange={(e) => setDraftTitle(e.target.value)}
            onKeyDown={handleKeyDown}
            disabled={isSaving}
            style={{
              flex: 1,
              fontFamily: "var(--tf-font-head)",
              fontSize: 22,
              fontWeight: 700,
              color: "var(--tf-text)",
              background: "var(--tf-bg4)",
              border: "1px solid var(--tf-accent)",
              borderRadius: 6,
              padding: "4px 10px",
              outline: "none",
            }}
          />
          <button
            onClick={handleSaveTitle}
            disabled={isSaving || !draftTitle.trim()}
            title="Save"
            style={{
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              width: 30,
              height: 30,
              borderRadius: 6,
              background: "var(--tf-accent-dim)",
              border: "1px solid var(--tf-accent)",
              cursor: "pointer",
              color: "var(--tf-accent)",
              flexShrink: 0,
            }}
          >
            <Check size={14} />
          </button>
          <button
            onClick={() => {
              setDraftTitle(workItem.title);
              setEditingTitle(false);
            }}
            title="Cancel"
            style={{
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              width: 30,
              height: 30,
              borderRadius: 6,
              background: "var(--tf-bg4)",
              border: "1px solid var(--tf-border)",
              cursor: "pointer",
              color: "var(--tf-text3)",
              flexShrink: 0,
            }}
          >
            <X size={14} />
          </button>
        </div>
      ) : (
        <h1
          onClick={() => setEditingTitle(true)}
          title="Click to edit title"
          style={{
            fontFamily: "var(--tf-font-head)",
            fontSize: 22,
            fontWeight: 700,
            color: "var(--tf-text)",
            lineHeight: 1.3,
            cursor: "text",
            padding: "4px 0",
            borderRadius: 4,
            transition: "background var(--tf-tr)",
            margin: 0,
          }}
          onMouseEnter={(e) => {
            (e.currentTarget as HTMLElement).style.background = "var(--tf-bg4)";
            (e.currentTarget as HTMLElement).style.padding = "4px 8px";
            (e.currentTarget as HTMLElement).style.marginLeft = "-8px";
          }}
          onMouseLeave={(e) => {
            (e.currentTarget as HTMLElement).style.background = "transparent";
            (e.currentTarget as HTMLElement).style.padding = "4px 0";
            (e.currentTarget as HTMLElement).style.marginLeft = "0";
          }}
        >
          {workItem.title}
        </h1>
      )}
    </div>
  );
}
