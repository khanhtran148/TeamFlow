"use client";

import { useState } from "react";
import { X } from "lucide-react";
import { toast } from "sonner";
import { useCreateProject } from "@/lib/hooks/use-projects";
import type { ApiError } from "@/lib/api/client";

interface CreateProjectDialogProps {
  open: boolean;
  onClose: () => void;
}

const DEFAULT_ORG_ID = "00000000-0000-0000-0000-000000000001";

export function CreateProjectDialog({ open, onClose }: CreateProjectDialogProps) {
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [nameError, setNameError] = useState("");

  const { mutate: createProject, isPending } = useCreateProject();

  function handleClose() {
    setName("");
    setDescription("");
    setNameError("");
    onClose();
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const trimmedName = name.trim();

    if (!trimmedName) {
      setNameError("Project name is required.");
      return;
    }
    if (trimmedName.length > 100) {
      setNameError("Project name must be 100 characters or fewer.");
      return;
    }

    setNameError("");
    createProject(
      {
        orgId: DEFAULT_ORG_ID,
        name: trimmedName,
        description: description.trim() || undefined,
      },
      {
        onSuccess: () => {
          toast.success("Project created successfully.");
          handleClose();
        },
        onError: (err) => {
          const apiErr = err as ApiError;
          toast.error(apiErr.message ?? "Failed to create project.");
        },
      },
    );
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
        role="dialog"
        aria-modal="true"
        aria-labelledby="create-project-title"
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
            id="create-project-title"
            style={{
              fontFamily: "var(--tf-font-head)",
              fontWeight: 700,
              fontSize: 16,
              color: "var(--tf-text)",
            }}
          >
            New Project
          </h2>
          <button
            onClick={handleClose}
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
              htmlFor="project-name"
              style={{ fontSize: 13, fontWeight: 500, color: "var(--tf-text2)" }}
            >
              Name <span style={{ color: "var(--tf-red)" }}>*</span>
            </label>
            <input
              id="project-name"
              type="text"
              value={name}
              onChange={(e) => {
                setName(e.target.value);
                if (nameError) setNameError("");
              }}
              placeholder="e.g. TeamFlow API"
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
              <span style={{ fontSize: 13, color: "var(--tf-red)" }}>{nameError}</span>
            )}
          </div>

          {/* Description field */}
          <div style={{ display: "flex", flexDirection: "column", gap: 5 }}>
            <label
              htmlFor="project-description"
              style={{ fontSize: 13, fontWeight: 500, color: "var(--tf-text2)" }}
            >
              Description
            </label>
            <textarea
              id="project-description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Short project description..."
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

          {/* Actions */}
          <div
            style={{
              display: "flex",
              justifyContent: "flex-end",
              gap: 8,
              marginTop: 4,
            }}
          >
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
                fontSize: 13,
                fontWeight: 500,
                cursor: isPending ? "not-allowed" : "pointer",
                opacity: isPending ? 0.5 : 1,
              }}
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isPending}
              style={{
                padding: "7px 14px",
                borderRadius: 6,
                border: "1px solid var(--tf-accent)",
                background: "var(--tf-accent)",
                color: "var(--primary-foreground)",
                fontSize: 13,
                fontWeight: 600,
                cursor: isPending ? "not-allowed" : "pointer",
                opacity: isPending ? 0.7 : 1,
                fontFamily: "var(--tf-font-body)",
              }}
            >
              {isPending ? "Creating..." : "Create Project"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
