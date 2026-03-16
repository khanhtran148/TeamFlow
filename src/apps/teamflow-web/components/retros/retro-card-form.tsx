"use client";

import { useState } from "react";
import { Plus } from "lucide-react";
import type { RetroCardCategory } from "@/lib/api/types";

interface RetroCardFormProps {
  category: RetroCardCategory;
  onSubmit: (content: string) => void;
  isPending: boolean;
}

export function RetroCardForm({
  category,
  onSubmit,
  isPending,
}: RetroCardFormProps) {
  const [content, setContent] = useState("");

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!content.trim() || isPending) return;
    onSubmit(content.trim());
    setContent("");
  }

  return (
    <form
      onSubmit={handleSubmit}
      style={{ display: "flex", gap: 6 }}
    >
      <input
        type="text"
        value={content}
        onChange={(e) => setContent(e.target.value)}
        placeholder={`Add a ${category === "WentWell" ? "positive" : category === "NeedsImprovement" ? "improvement" : "action"} item...`}
        style={{
          flex: 1,
          padding: "7px 10px",
          background: "var(--tf-bg4)",
          border: "1px solid var(--tf-border)",
          borderRadius: 6,
          fontSize: 13,
          color: "var(--tf-text)",
          fontFamily: "var(--tf-font-body)",
          outline: "none",
        }}
      />
      <button
        type="submit"
        disabled={!content.trim() || isPending}
        style={{
          display: "inline-flex",
          alignItems: "center",
          padding: "7px 10px",
          borderRadius: 6,
          border: "none",
          background:
            content.trim() && !isPending
              ? "var(--tf-accent)"
              : "var(--tf-bg3)",
          color:
            content.trim() && !isPending
              ? "var(--tf-bg)"
              : "var(--tf-text3)",
          cursor:
            content.trim() && !isPending ? "pointer" : "not-allowed",
          transition: "all var(--tf-tr)",
        }}
      >
        <Plus size={14} />
      </button>
    </form>
  );
}
