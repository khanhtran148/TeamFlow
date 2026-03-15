"use client";

import { useState } from "react";
import { useCreateTeam } from "@/lib/hooks/use-teams";
import { toast } from "sonner";

interface CreateTeamDialogProps {
  orgId: string;
  open: boolean;
  onClose: () => void;
}

export function CreateTeamDialog({ orgId, open, onClose }: CreateTeamDialogProps) {
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const createTeam = useCreateTeam();

  if (!open) return null;

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    try {
      await createTeam.mutateAsync({ orgId, name, description: description || undefined });
      toast.success("Team created");
      setName("");
      setDescription("");
      onClose();
    } catch {
      toast.error("Failed to create team");
    }
  }

  return (
    <div
      style={{
        position: "fixed",
        inset: 0,
        background: "rgba(0,0,0,0.5)",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        zIndex: 50,
      }}
      onClick={onClose}
    >
      <div
        style={{
          background: "var(--tf-bg2)",
          border: "1px solid var(--tf-border)",
          borderRadius: 12,
          padding: 24,
          width: 400,
          maxWidth: "90vw",
        }}
        onClick={(e) => e.stopPropagation()}
      >
        <h2
          style={{
            fontSize: 18,
            fontWeight: 700,
            color: "var(--tf-text)",
            fontFamily: "var(--tf-font-head)",
            margin: "0 0 16px",
          }}
        >
          Create Team
        </h2>
        <form onSubmit={handleSubmit} style={{ display: "flex", flexDirection: "column", gap: 12 }}>
          <input
            placeholder="Team name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            style={{
              padding: "9px 12px",
              borderRadius: 8,
              border: "1px solid var(--tf-border)",
              background: "var(--tf-bg3)",
              color: "var(--tf-text)",
              fontSize: 14,
            }}
          />
          <textarea
            placeholder="Description (optional)"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            rows={3}
            style={{
              padding: "9px 12px",
              borderRadius: 8,
              border: "1px solid var(--tf-border)",
              background: "var(--tf-bg3)",
              color: "var(--tf-text)",
              fontSize: 14,
              resize: "vertical",
            }}
          />
          <div style={{ display: "flex", gap: 8, justifyContent: "flex-end" }}>
            <button
              type="button"
              onClick={onClose}
              style={{
                padding: "8px 16px",
                borderRadius: 8,
                border: "1px solid var(--tf-border)",
                background: "transparent",
                color: "var(--tf-text2)",
                cursor: "pointer",
                fontSize: 13,
              }}
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={createTeam.isPending}
              style={{
                padding: "8px 16px",
                borderRadius: 8,
                border: "none",
                background: "linear-gradient(135deg, var(--tf-accent), var(--tf-blue))",
                color: "#0a0a0b",
                fontWeight: 600,
                cursor: "pointer",
                fontSize: 13,
              }}
            >
              {createTeam.isPending ? "Creating..." : "Create"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
