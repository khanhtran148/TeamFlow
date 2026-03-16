"use client";

import { useState } from "react";
import { toast } from "sonner";
import { ChevronDown, X, Loader2 } from "lucide-react";
import { useQuery } from "@tanstack/react-query";
import { useAssignWorkItem, useUnassignWorkItem } from "@/lib/hooks/use-work-items";
import { UserAvatar, formatAssignedAt } from "@/components/shared/user-avatar";
import { apiClient } from "@/lib/api/client";

interface ProjectMember {
  id: string;
  memberId: string;
  memberName: string;
  memberType: string;
  role: string;
}

function useProjectMembers(projectId: string) {
  return useQuery({
    queryKey: ["project-members", projectId],
    queryFn: async () => {
      const { data } = await apiClient.get<ProjectMember[]>(
        `/projects/${projectId}/memberships`,
      );
      return data
        .filter((m) => m.memberType === "User")
        .map((m) => ({ id: m.memberId, name: m.memberName }));
    },
    staleTime: 60_000,
  });
}

interface AssigneePickerProps {
  workItemId: string;
  projectId: string;
  assigneeId: string | null;
  assigneeName: string | null;
  assignedAt?: string | null;
}

export function AssigneePicker({
  workItemId,
  projectId,
  assigneeId,
  assigneeName,
  assignedAt,
}: AssigneePickerProps) {
  const [open, setOpen] = useState(false);

  const { data: members = [], isLoading: membersLoading } = useProjectMembers(projectId);
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
          fontSize: 13,
          fontFamily: "var(--tf-font-body)",
          minWidth: 140,
        }}
      >
        {isPending ? (
          <Loader2 size={12} className="animate-spin" style={{ color: "var(--tf-text3)" }} />
        ) : assigneeName ? (
          <UserAvatar
            initials={initials}
            name={assigneeName}
            subtitle={formatAssignedAt(assignedAt ?? null)}
            size="xs"
          />
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
              fontSize: 13,
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
                  fontSize: 13,
                  fontFamily: "var(--tf-font-body)",
                  borderBottom: "1px solid var(--tf-border)",
                  marginBottom: 4,
                }}
              >
                <X size={12} />
                Unassign
              </button>
            )}
            {membersLoading ? (
              <div style={{ padding: "12px", textAlign: "center", color: "var(--tf-text3)", fontSize: 12 }}>
                <Loader2 size={14} className="animate-spin" style={{ display: "inline-block" }} />
              </div>
            ) : members.length === 0 ? (
              <div style={{ padding: "8px 12px", color: "var(--tf-text3)", fontSize: 12 }}>
                No members found
              </div>
            ) : (
              members.map((user) => {
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
                      fontSize: 13,
                      fontFamily: "var(--tf-font-body)",
                    }}
                  >
                    <UserAvatar initials={userInitials} name={user.name} size="xs" />
                    <span style={{ flex: 1, textAlign: "left" }}>{user.name}</span>
                    {isSelected && (
                      <span style={{ fontSize: 13, color: "var(--tf-accent)" }}>✓</span>
                    )}
                  </button>
                );
              })
            )}
          </div>
        </>
      )}
    </div>
  );
}
