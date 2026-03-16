"use client";

import { useState } from "react";
import { Plus, Link as LinkIcon } from "lucide-react";

interface RetroActionItemFormProps {
  onSubmit: (data: {
    title: string;
    description?: string;
    assigneeId?: string;
    dueDate?: string;
    linkToBacklog?: boolean;
  }) => void;
  isPending: boolean;
}

export function RetroActionItemForm({
  onSubmit,
  isPending,
}: RetroActionItemFormProps) {
  const [title, setTitle] = useState("");
  const [linkToBacklog, setLinkToBacklog] = useState(false);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!title.trim() || isPending) return;
    onSubmit({
      title: title.trim(),
      linkToBacklog,
    });
    setTitle("");
    setLinkToBacklog(false);
  }

  return (
    <form
      onSubmit={handleSubmit}
      style={{ display: "flex", flexDirection: "column", gap: 8 }}
    >
      <div style={{ display: "flex", gap: 6 }}>
        <input
          type="text"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          placeholder="Action item title..."
          style={{
            flex: 1,
            padding: "7px 10px",
            background: "var(--tf-bg4)",
            border: "1px solid var(--tf-border)",
            borderRadius: 6,
            fontSize: 12,
            color: "var(--tf-text)",
            fontFamily: "var(--tf-font-body)",
            outline: "none",
          }}
        />
        <button
          type="submit"
          disabled={!title.trim() || isPending}
          style={{
            display: "inline-flex",
            alignItems: "center",
            gap: 4,
            padding: "7px 12px",
            borderRadius: 6,
            border: "none",
            fontSize: 12,
            fontWeight: 600,
            background:
              title.trim() && !isPending
                ? "var(--tf-accent)"
                : "var(--tf-bg3)",
            color:
              title.trim() && !isPending
                ? "var(--tf-bg)"
                : "var(--tf-text3)",
            cursor:
              title.trim() && !isPending ? "pointer" : "not-allowed",
            fontFamily: "var(--tf-font-body)",
          }}
        >
          <Plus size={12} />
          Add
        </button>
      </div>
      <label
        style={{
          display: "inline-flex",
          alignItems: "center",
          gap: 6,
          fontSize: 11,
          color: "var(--tf-text3)",
          cursor: "pointer",
        }}
      >
        <input
          type="checkbox"
          checked={linkToBacklog}
          onChange={(e) => setLinkToBacklog(e.target.checked)}
          style={{ accentColor: "var(--tf-accent)" }}
        />
        <LinkIcon size={10} />
        Create linked Task in backlog
      </label>
    </form>
  );
}
