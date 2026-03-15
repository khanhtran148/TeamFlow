"use client";

import { useState } from "react";
import { toast } from "sonner";
import { ChevronDown, X, Loader2 } from "lucide-react";
import { useAssignWorkItem, useUnassignWorkItem } from "@/lib/hooks/use-work-items";
import { UserAvatar } from "@/components/shared/user-avatar";

// Phase 1: seed users (no real user API yet)
const SEED_USERS = [
  { id: "00000000-0000-0000-0000-000000000001", name: "Alice Johnson" },
  { id: "00000000-0000-0000-0000-000000000002", name: "Bob Smith" },
  { id: "00000000-0000-0000-0000-000000000003", name: "Carol White" },
  { id: "00000000-0000-0000-0000-000000000004", name: "Dave Brown" },
  { id: "00000000-0000-0000-0000-000000000005", name: "Eve Davis" },
];

interface AssigneePickerProps {
  workItemId: string;
  projectId: string;
  assigneeId: string | null;
  assigneeName: string | null;
}

export function AssigneePicker({
  workItemId,
  projectId,
  assigneeId,
  assigneeName,
}: AssigneePickerProps) {
  const [open, setOpen] = useState(false);

  const assignMutation = useAssignWorkItem(projectId);
  const unassignMutation = useUnassignWorkItem(projectId);

  const isPending = assignMutation.isPending || unassignMutation.isPending;

  async function handleAssign(userId: string, userName: string) {
    setOpen(false);
    try {
      await assignMutation.mutateAsync({
        id: workItemId,
        data: { assigneeId: userId },
      });
      toast.success(`Assigned to ${userName}`);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to assign";
      toast.error(message);
    }
  }

  async function handleUnassign(e: React.MouseEvent) {
    e.stopPropagation();
    setOpen(false);
    try {
      await unassignMutation.mutateAsync(workItemId);
      toast.success("Assignee removed");
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to unassign";
      toast.error(message);
    }
  }

  const initials = assigneeName
    ? assigneeName
        .split(" ")
        .map((n) => n[0])
        .join("")
        .slice(0, 2)
    : "";

  return (
    <div style={{ position: "relative", display: "inline-block" }}>
      <button
        onClick={() => setOpen((v) => !v)}
        disabled={isPending}
        style={{
          display: "flex",
          alignItems: "center",
          gap: 6,
          background: "var(--tf-bg4)",
          border: "1px solid var(--tf-border)",
          borderRadius: 6,
          padding: "5px 10px",
          cursor: "pointer",
          color: "var(--tf-text)",
          fontSize: 12,
          fontFamily: "var(--tf-font-body)",
          minWidth: 140,
        }}
      >
        {isPending ? (
          <Loader2 size={12} className="animate-spin" style={{ color: "var(--tf-text3)" }} />
        ) : assigneeName ? (
          <UserAvatar initials={initials} name={assigneeName} size="xs" />
        ) : (
          <span
            style={{
              width: 18,
              height: 18,
              borderRadius: "50%",
              border: "1px dashed var(--tf-border2)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              fontSize: 10,
              color: "var(--tf-text3)",
              flexShrink: 0,
            }}
          >
            ?
          </span>
        )}
        <span
          style={{
            flex: 1,
            color: assigneeName ? "var(--tf-text)" : "var(--tf-text3)",
            overflow: "hidden",
            textOverflow: "ellipsis",
            whiteSpace: "nowrap",
          }}
        >
          {assigneeName ?? "Unassigned"}
        </span>
        {assigneeId ? (
          <X
            size={11}
            style={{ color: "var(--tf-text3)", flexShrink: 0 }}
            onClick={handleUnassign}
          />
        ) : (
          <ChevronDown size={11} style={{ color: "var(--tf-text3)", flexShrink: 0 }} />
        )}
      </button>

      {open && (
        <>
          <div
            style={{ position: "fixed", inset: 0, zIndex: 40 }}
            onClick={() => setOpen(false)}
          />
          <div
            style={{
              position: "absolute",
              top: "calc(100% + 4px)",
              left: 0,
              zIndex: 50,
              background: "var(--tf-bg3)",
              border: "1px solid var(--tf-border)",
              borderRadius: 8,
              padding: "4px 0",
              minWidth: 180,
              boxShadow: "var(--tf-shadow)",
            }}
          >
            {assigneeId && (
              <button
                onClick={handleUnassign}
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: 8,
                  width: "100%",
                  padding: "7px 12px",
                  background: "none",
                  border: "none",
                  cursor: "pointer",
                  color: "var(--tf-red)",
                  fontSize: 12,
                  fontFamily: "var(--tf-font-body)",
                  borderBottom: "1px solid var(--tf-border)",
                  marginBottom: 4,
                }}
              >
                <X size={12} />
                Unassign
              </button>
            )}
            {SEED_USERS.map((user) => {
              const userInitials = user.name
                .split(" ")
                .map((n) => n[0])
                .join("")
                .slice(0, 2);
              const isSelected = user.id === assigneeId;
              return (
                <button
                  key={user.id}
                  onClick={() => handleAssign(user.id, user.name)}
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: 8,
                    width: "100%",
                    padding: "7px 12px",
                    background: isSelected ? "var(--tf-bg4)" : "none",
                    border: "none",
                    cursor: "pointer",
                    color: "var(--tf-text)",
                    fontSize: 12,
                    fontFamily: "var(--tf-font-body)",
                  }}
                >
                  <UserAvatar initials={userInitials} name={user.name} size="xs" />
                  <span style={{ flex: 1, textAlign: "left" }}>{user.name}</span>
                  {isSelected && (
                    <span style={{ fontSize: 10, color: "var(--tf-accent)" }}>✓</span>
                  )}
                </button>
              );
            })}
          </div>
        </>
      )}
    </div>
  );
}
