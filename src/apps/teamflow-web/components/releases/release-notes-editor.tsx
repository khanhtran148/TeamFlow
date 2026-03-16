"use client";

import { useState } from "react";
import { FileText, Save } from "lucide-react";

interface ReleaseNotesEditorProps {
  initialNotes: string;
  isLocked: boolean;
  onSave: (notes: string) => Promise<void>;
  isPending: boolean;
}

export function ReleaseNotesEditor({
  initialNotes,
  isLocked,
  onSave,
  isPending,
}: ReleaseNotesEditorProps) {
  const [notes, setNotes] = useState(initialNotes);
  const [isDirty, setIsDirty] = useState(false);

  function handleChange(e: React.ChangeEvent<HTMLTextAreaElement>) {
    setNotes(e.target.value);
    setIsDirty(e.target.value !== initialNotes);
  }

  async function handleSave() {
    await onSave(notes);
    setIsDirty(false);
  }

  return (
    <div
      style={{
        background: "var(--tf-bg2)",
        border: "1px solid var(--tf-border)",
        borderRadius: 8,
        overflow: "hidden",
      }}
    >
      <div
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          padding: "10px 14px",
          borderBottom: "1px solid var(--tf-border)",
        }}
      >
        <span
          style={{
            display: "flex",
            alignItems: "center",
            gap: 6,
            fontFamily: "var(--tf-font-head)",
            fontWeight: 600,
            fontSize: 13,
            color: "var(--tf-text)",
          }}
        >
          <FileText size={14} />
          Release Notes
          {isLocked && (
            <span
              style={{
                padding: "1px 6px",
                borderRadius: 100,
                fontSize: 10,
                background: "var(--tf-bg4)",
                color: "var(--tf-text3)",
              }}
            >
              Locked
            </span>
          )}
        </span>

        {!isLocked && isDirty && (
          <button
            onClick={handleSave}
            disabled={isPending}
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 4,
              padding: "4px 12px",
              borderRadius: 5,
              fontSize: 11,
              fontWeight: 600,
              border: "none",
              background: "var(--tf-accent)",
              color: "var(--tf-bg)",
              cursor: isPending ? "not-allowed" : "pointer",
              fontFamily: "var(--tf-font-body)",
            }}
          >
            <Save size={10} />
            {isPending ? "Saving..." : "Save"}
          </button>
        )}
      </div>

      <textarea
        value={notes}
        onChange={handleChange}
        disabled={isLocked}
        placeholder={isLocked ? "No release notes." : "Add release notes..."}
        rows={6}
        style={{
          width: "100%",
          padding: "12px 14px",
          background: isLocked ? "var(--tf-bg3)" : "transparent",
          border: "none",
          outline: "none",
          fontSize: 13,
          color: isLocked ? "var(--tf-text3)" : "var(--tf-text)",
          fontFamily: "var(--tf-font-body)",
          resize: "vertical",
          minHeight: 100,
          cursor: isLocked ? "not-allowed" : "text",
        }}
      />
    </div>
  );
}
