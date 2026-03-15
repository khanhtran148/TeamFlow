"use client";

import { useState, useEffect } from "react";
import { X } from "lucide-react";
import { toast } from "sonner";
import { useCreateSprint, useUpdateSprint } from "@/lib/hooks/use-sprints";
import type { SprintDto } from "@/lib/api/types";
import type { ApiError } from "@/lib/api/client";

interface SprintFormDialogProps {
  open: boolean;
  projectId: string;
  sprint?: SprintDto | null;
  onClose: () => void;
}

export function SprintFormDialog({ open, projectId, sprint, onClose }: SprintFormDialogProps) {
  const isEditing = !!sprint;

  const [name, setName] = useState("");
  const [goal, setGoal] = useState("");
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [nameError, setNameError] = useState("");
  const [dateError, setDateError] = useState("");

  const { mutate: createSprint, isPending: isCreating } = useCreateSprint(projectId);
  const { mutate: updateSprint, isPending: isUpdating } = useUpdateSprint(projectId);
  const isPending = isCreating || isUpdating;

  useEffect(() => {
    if (open && sprint) {
      setName(sprint.name);
      setGoal(sprint.goal ?? "");
      setStartDate(sprint.startDate ? sprint.startDate.split("T")[0] : "");
      setEndDate(sprint.endDate ? sprint.endDate.split("T")[0] : "");
    } else if (open && !sprint) {
      setName("");
      setGoal("");
      setStartDate("");
      setEndDate("");
    }
    setNameError("");
    setDateError("");
  }, [open, sprint]);

  function handleClose() {
    setName("");
    setGoal("");
    setStartDate("");
    setEndDate("");
    setNameError("");
    setDateError("");
    onClose();
  }

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const trimmedName = name.trim();

    if (!trimmedName) {
      setNameError("Sprint name is required.");
      return;
    }
    if (trimmedName.length > 100) {
      setNameError("Sprint name must be 100 characters or fewer.");
      return;
    }

    if (startDate && endDate && endDate <= startDate) {
      setDateError("End date must be after start date.");
      return;
    }

    setNameError("");
    setDateError("");

    const body = {
      name: trimmedName,
      goal: goal.trim() || undefined,
      startDate: startDate || undefined,
      endDate: endDate || undefined,
    };

    if (isEditing && sprint) {
      updateSprint(
        { id: sprint.id, data: body },
        {
          onSuccess: () => {
            toast.success("Sprint updated successfully.");
            handleClose();
          },
          onError: (err) => {
            const apiErr = err as ApiError;
            toast.error(apiErr.message ?? "Failed to update sprint.");
          },
        },
      );
    } else {
      createSprint(
        { ...body, projectId },
        {
          onSuccess: () => {
            toast.success("Sprint created successfully.");
            handleClose();
          },
          onError: (err) => {
            const apiErr = err as ApiError;
            toast.error(apiErr.message ?? "Failed to create sprint.");
          },
        },
      );
    }
  }

  if (!open) return null;

  return (
    <div
      style={{
        position: "fixed",
        inset: 0,
        zIndex: 50,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        background: "rgba(0,0,0,0.6)",
        backdropFilter: "blur(2px)",
      }}
      onClick={handleClose}
    >
      <div
        data-testid="sprint-form-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="sprint-form-title"
        onClick={(e) => e.stopPropagation()}
        style={{
          background: "var(--tf-bg2)",
          border: "1px solid var(--tf-border)",
          borderRadius: "var(--tf-radius)",
          boxShadow: "var(--tf-shadow)",
          width: "100%",
          maxWidth: 440,
          padding: 20,
        }}
      >
        {/* Header */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            marginBottom: 16,
          }}
        >
          <h2
            id="sprint-form-title"
            style={{
              fontFamily: "var(--tf-font-head)",
              fontWeight: 700,
              fontSize: 16,
              color: "var(--tf-text)",
            }}
          >
            {isEditing ? "Edit Sprint" : "New Sprint"}
          </h2>
          <button
            onClick={handleClose}
            aria-label="Close dialog"
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
            }}
          >
            <X size={13} />
          </button>
        </div>

        <form onSubmit={handleSubmit} style={{ display: "flex", flexDirection: "column", gap: 12 }}>
          {/* Name field */}
          <div style={{ display: "flex", flexDirection: "column", gap: 5 }}>
            <label
              htmlFor="sprint-name"
              style={{ fontSize: 12, fontWeight: 500, color: "var(--tf-text2)" }}
            >
              Name <span style={{ color: "var(--tf-red)" }}>*</span>
            </label>
            <input
              data-testid="sprint-name-input"
              id="sprint-name"
              type="text"
              value={name}
              onChange={(e) => {
                setName(e.target.value);
                if (nameError) setNameError("");
              }}
              placeholder="e.g. Sprint 1"
              autoFocus
              style={{
                padding: "7px 10px",
                borderRadius: 6,
                border: `1px solid ${nameError ? "var(--tf-red)" : "var(--tf-border)"}`,
                background: "var(--tf-bg3)",
                color: "var(--tf-text)",
                fontSize: 13,
                outline: "none",
                fontFamily: "var(--tf-font-body)",
                transition: "border-color var(--tf-tr)",
              }}
              onFocus={(e) => {
                (e.currentTarget as HTMLInputElement).style.borderColor = nameError
                  ? "var(--tf-red)"
                  : "var(--tf-accent)";
              }}
              onBlur={(e) => {
                (e.currentTarget as HTMLInputElement).style.borderColor = nameError
                  ? "var(--tf-red)"
                  : "var(--tf-border)";
              }}
            />
            {nameError && (
              <span style={{ fontSize: 11, color: "var(--tf-red)" }}>{nameError}</span>
            )}
          </div>

          {/* Goal field */}
          <div style={{ display: "flex", flexDirection: "column", gap: 5 }}>
            <label
              htmlFor="sprint-goal"
              style={{ fontSize: 12, fontWeight: 500, color: "var(--tf-text2)" }}
            >
              Goal
            </label>
            <textarea
              data-testid="sprint-goal-input"
              id="sprint-goal"
              value={goal}
              onChange={(e) => setGoal(e.target.value)}
              placeholder="What should the team accomplish?"
              rows={3}
              style={{
                padding: "7px 10px",
                borderRadius: 6,
                border: "1px solid var(--tf-border)",
                background: "var(--tf-bg3)",
                color: "var(--tf-text)",
                fontSize: 13,
                outline: "none",
                fontFamily: "var(--tf-font-body)",
                resize: "vertical",
                transition: "border-color var(--tf-tr)",
              }}
              onFocus={(e) => {
                (e.currentTarget as HTMLTextAreaElement).style.borderColor = "var(--tf-accent)";
              }}
              onBlur={(e) => {
                (e.currentTarget as HTMLTextAreaElement).style.borderColor = "var(--tf-border)";
              }}
            />
          </div>

          {/* Date fields */}
          <div style={{ display: "flex", gap: 12 }}>
            <div style={{ flex: 1, display: "flex", flexDirection: "column", gap: 5 }}>
              <label
                htmlFor="sprint-start-date"
                style={{ fontSize: 12, fontWeight: 500, color: "var(--tf-text2)" }}
              >
                Start Date
              </label>
              <input
                data-testid="sprint-start-date"
                id="sprint-start-date"
                type="date"
                value={startDate}
                onChange={(e) => {
                  setStartDate(e.target.value);
                  if (dateError) setDateError("");
                }}
                style={{
                  padding: "7px 10px",
                  borderRadius: 6,
                  border: `1px solid ${dateError ? "var(--tf-red)" : "var(--tf-border)"}`,
                  background: "var(--tf-bg3)",
                  color: "var(--tf-text)",
                  fontSize: 13,
                  outline: "none",
                  fontFamily: "var(--tf-font-body)",
                  transition: "border-color var(--tf-tr)",
                  colorScheme: "dark",
                }}
                onFocus={(e) => {
                  (e.currentTarget as HTMLInputElement).style.borderColor = "var(--tf-accent)";
                }}
                onBlur={(e) => {
                  (e.currentTarget as HTMLInputElement).style.borderColor = dateError
                    ? "var(--tf-red)"
                    : "var(--tf-border)";
                }}
              />
            </div>
            <div style={{ flex: 1, display: "flex", flexDirection: "column", gap: 5 }}>
              <label
                htmlFor="sprint-end-date"
                style={{ fontSize: 12, fontWeight: 500, color: "var(--tf-text2)" }}
              >
                End Date
              </label>
              <input
                data-testid="sprint-end-date"
                id="sprint-end-date"
                type="date"
                value={endDate}
                onChange={(e) => {
                  setEndDate(e.target.value);
                  if (dateError) setDateError("");
                }}
                style={{
                  padding: "7px 10px",
                  borderRadius: 6,
                  border: `1px solid ${dateError ? "var(--tf-red)" : "var(--tf-border)"}`,
                  background: "var(--tf-bg3)",
                  color: "var(--tf-text)",
                  fontSize: 13,
                  outline: "none",
                  fontFamily: "var(--tf-font-body)",
                  transition: "border-color var(--tf-tr)",
                  colorScheme: "dark",
                }}
                onFocus={(e) => {
                  (e.currentTarget as HTMLInputElement).style.borderColor = "var(--tf-accent)";
                }}
                onBlur={(e) => {
                  (e.currentTarget as HTMLInputElement).style.borderColor = dateError
                    ? "var(--tf-red)"
                    : "var(--tf-border)";
                }}
              />
            </div>
          </div>
          {dateError && (
            <span style={{ fontSize: 11, color: "var(--tf-red)" }}>{dateError}</span>
          )}

          {/* Actions */}
          <div style={{ display: "flex", justifyContent: "flex-end", gap: 8, marginTop: 4 }}>
            <button
              type="button"
              onClick={handleClose}
              disabled={isPending}
              style={{
                padding: "7px 14px",
                borderRadius: 6,
                border: "1px solid var(--tf-border)",
                background: "transparent",
                color: "var(--tf-text2)",
                fontSize: 12,
                fontWeight: 500,
                cursor: isPending ? "not-allowed" : "pointer",
                opacity: isPending ? 0.5 : 1,
              }}
            >
              Cancel
            </button>
            <button
              data-testid="sprint-submit-btn"
              type="submit"
              disabled={isPending}
              style={{
                padding: "7px 14px",
                borderRadius: 6,
                border: "1px solid var(--tf-accent)",
                background: "var(--tf-accent)",
                color: "var(--primary-foreground)",
                fontSize: 12,
                fontWeight: 600,
                cursor: isPending ? "not-allowed" : "pointer",
                opacity: isPending ? 0.7 : 1,
                fontFamily: "var(--tf-font-body)",
              }}
            >
              {isPending
                ? isEditing
                  ? "Saving..."
                  : "Creating..."
                : isEditing
                  ? "Save Changes"
                  : "Create Sprint"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
