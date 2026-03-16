"use client";

import { useState, useEffect } from "react";
import { Loader2, Save } from "lucide-react";
import { Button } from "@/components/ui/button";
import type { WorkItemDto, Priority } from "@/lib/api/types";

const PRIORITY_OPTIONS: { value: Priority; label: string }[] = [
  { value: "Critical", label: "Critical" },
  { value: "High", label: "High" },
  { value: "Medium", label: "Medium" },
  { value: "Low", label: "Low" },
];

interface WorkItemFormProps {
  workItem: WorkItemDto;
  onSave: (data: {
    description?: string;
    acceptanceCriteria?: string;
    estimationValue?: number;
    priority?: Priority;
  }) => Promise<void>;
  isSaving?: boolean;
}

export function WorkItemForm({ workItem, onSave, isSaving }: WorkItemFormProps) {
  const [description, setDescription] = useState(workItem.description ?? "");
  const [acceptanceCriteria, setAcceptanceCriteria] = useState(
    workItem.acceptanceCriteria ?? "",
  );
  const [estimation, setEstimation] = useState(
    workItem.estimationValue != null ? String(workItem.estimationValue) : "",
  );
  const [priority, setPriority] = useState<Priority | "">(workItem.priority ?? "");
  const [isDirty, setIsDirty] = useState(false);

  // Reset form when workItem changes (e.g., navigation)
  useEffect(() => {
    setDescription(workItem.description ?? "");
    setAcceptanceCriteria(
      workItem.acceptanceCriteria ?? "",
    );
    setEstimation(
      workItem.estimationValue != null ? String(workItem.estimationValue) : "",
    );
    setPriority(workItem.priority ?? "");
    setIsDirty(false);
  }, [workItem.id, workItem.description, workItem.estimationValue, workItem.priority]);

  function markDirty() {
    setIsDirty(true);
  }

  async function handleSave() {
    const estimationNum = estimation ? parseFloat(estimation) : undefined;
    await onSave({
      description: description || undefined,
      acceptanceCriteria: acceptanceCriteria || undefined,
      estimationValue: !isNaN(estimationNum ?? NaN) ? estimationNum : undefined,
      priority: priority || undefined,
    });
    setIsDirty(false);
  }

  const labelStyle: React.CSSProperties = {
    display: "block",
    fontSize: 13,
    fontWeight: 600,
    color: "var(--tf-text2)",
    fontFamily: "var(--tf-font-body)",
    marginBottom: 5,
    textTransform: "uppercase",
    letterSpacing: "0.05em",
  };

  const textareaStyle: React.CSSProperties = {
    width: "100%",
    padding: "8px 10px",
    borderRadius: 6,
    border: "1px solid var(--tf-border)",
    background: "var(--tf-bg4)",
    color: "var(--tf-text)",
    fontSize: 13,
    fontFamily: "var(--tf-font-body)",
    outline: "none",
    resize: "vertical",
    lineHeight: 1.6,
    boxSizing: "border-box",
  };

  const inputStyle: React.CSSProperties = {
    width: "100%",
    padding: "7px 10px",
    borderRadius: 6,
    border: "1px solid var(--tf-border)",
    background: "var(--tf-bg4)",
    color: "var(--tf-text)",
    fontSize: 13,
    fontFamily: "var(--tf-font-body)",
    outline: "none",
    boxSizing: "border-box",
  };

  const selectStyle: React.CSSProperties = {
    ...inputStyle,
    appearance: "none",
  };

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 18 }}>
      {/* Description */}
      <div>
        <label style={labelStyle}>Description</label>
        <textarea
          value={description}
          onChange={(e) => {
            setDescription(e.target.value);
            markDirty();
          }}
          placeholder="Add a description…"
          rows={5}
          style={textareaStyle}
        />
      </div>

      {/* Acceptance criteria (UserStory + Epic) */}
      {(workItem.type === "UserStory" || workItem.type === "Epic") && (
        <div>
          <label style={labelStyle}>Acceptance Criteria</label>
          <textarea
            value={acceptanceCriteria}
            onChange={(e) => {
              setAcceptanceCriteria(e.target.value);
              markDirty();
            }}
            placeholder="Define the acceptance criteria…"
            rows={4}
            style={textareaStyle}
          />
        </div>
      )}

      {/* Estimation + Priority row */}
      <div style={{ display: "flex", gap: 12 }}>
        <div style={{ flex: 1 }}>
          <label style={labelStyle}>Estimation (points)</label>
          <input
            type="number"
            min="0"
            step="0.5"
            value={estimation}
            onChange={(e) => {
              setEstimation(e.target.value);
              markDirty();
            }}
            placeholder="—"
            style={inputStyle}
          />
        </div>
        <div style={{ flex: 1 }}>
          <label style={labelStyle}>Priority</label>
          <select
            value={priority}
            onChange={(e) => {
              setPriority(e.target.value as Priority | "");
              markDirty();
            }}
            style={selectStyle}
          >
            <option value="">— None —</option>
            {PRIORITY_OPTIONS.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Save button */}
      {isDirty && (
        <div style={{ display: "flex", justifyContent: "flex-end" }}>
          <Button
            onClick={handleSave}
            disabled={isSaving}
            style={{
              background: "var(--tf-accent)",
              color: "var(--tf-bg)",
              fontWeight: 600,
              fontSize: 13,
              gap: 6,
            }}
          >
            {isSaving ? (
              <>
                <Loader2 size={13} className="animate-spin" />
                Saving…
              </>
            ) : (
              <>
                <Save size={13} />
                Save Changes
              </>
            )}
          </Button>
        </div>
      )}
    </div>
  );
}
