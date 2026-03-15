"use client";

import { useState } from "react";
import { useAddTeamMember } from "@/lib/hooks/use-teams";
import { toast } from "sonner";

const ROLES = [
  "Developer",
  "TeamManager",
  "TechnicalLeader",
  "ProductOwner",
  "Viewer",
];

interface AddMemberDialogProps {
  teamId: string;
  open: boolean;
  onClose: () => void;
}

export function AddMemberDialog({ teamId, open, onClose }: AddMemberDialogProps) {
  const [userId, setUserId] = useState("");
  const [role, setRole] = useState("Developer");
  const addMember = useAddTeamMember();

  if (!open) return null;

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    try {
      await addMember.mutateAsync({ teamId, userId, role });
      toast.success("Member added");
      setUserId("");
      setRole("Developer");
      onClose();
    } catch {
      toast.error("Failed to add member");
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
          width: 380,
          maxWidth: "90vw",
        }}
        onClick={(e) => e.stopPropagation()}
      >
        <h2
          style={{
            fontSize: 16,
            fontWeight: 700,
            color: "var(--tf-text)",
            fontFamily: "var(--tf-font-head)",
            margin: "0 0 16px",
          }}
        >
          Add Member
        </h2>
        <form onSubmit={handleSubmit} style={{ display: "flex", flexDirection: "column", gap: 12 }}>
          <input
            placeholder="User ID"
            value={userId}
            onChange={(e) => setUserId(e.target.value)}
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
          <select
            value={role}
            onChange={(e) => setRole(e.target.value)}
            style={{
              padding: "9px 12px",
              borderRadius: 8,
              border: "1px solid var(--tf-border)",
              background: "var(--tf-bg3)",
              color: "var(--tf-text)",
              fontSize: 14,
            }}
          >
            {ROLES.map((r) => (
              <option key={r} value={r}>
                {r}
              </option>
            ))}
          </select>
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
              disabled={addMember.isPending}
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
              {addMember.isPending ? "Adding..." : "Add"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
