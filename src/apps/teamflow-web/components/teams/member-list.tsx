"use client";

import { Trash2 } from "lucide-react";
import type { TeamMemberDto } from "@/lib/api/teams";
import { useRemoveTeamMember, useChangeTeamMemberRole } from "@/lib/hooks/use-teams";
import { UserAvatar } from "@/components/shared/user-avatar";
import { toast } from "sonner";

const ROLES = [
  "OrgAdmin",
  "ProductOwner",
  "TechnicalLeader",
  "TeamManager",
  "Developer",
  "Viewer",
];

interface MemberListProps {
  teamId: string;
  members: TeamMemberDto[];
}

export function MemberList({ teamId, members }: MemberListProps) {
  const removeMember = useRemoveTeamMember();
  const changeRole = useChangeTeamMemberRole();

  async function handleRemove(userId: string, userName: string) {
    if (!confirm(`Remove ${userName} from this team?`)) return;
    try {
      await removeMember.mutateAsync({ teamId, userId });
      toast.success(`${userName} removed`);
    } catch {
      toast.error("Failed to remove member");
    }
  }

  async function handleRoleChange(userId: string, newRole: string) {
    try {
      await changeRole.mutateAsync({ teamId, userId, newRole });
      toast.success("Role updated");
    } catch {
      toast.error("Failed to change role");
    }
  }

  if (members.length === 0) {
    return (
      <div style={{ color: "var(--tf-text3)", fontSize: 13, padding: 16 }}>
        No members yet. Add someone to get started.
      </div>
    );
  }

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 1 }}>
      {members.map((member) => {
        const initials = member.userName
          .split(" ")
          .map((n) => n[0])
          .join("")
          .slice(0, 2)
          .toUpperCase();

        return (
          <div
            key={member.id}
            style={{
              display: "flex",
              alignItems: "center",
              gap: 12,
              padding: "10px 0",
              borderBottom: "1px solid var(--tf-border)",
            }}
          >
            <UserAvatar initials={initials} size="sm" />
            <div style={{ flex: 1 }}>
              <div style={{ fontSize: 13, fontWeight: 500, color: "var(--tf-text)" }}>
                {member.userName}
              </div>
              <div style={{ fontSize: 13, color: "var(--tf-text3)" }}>
                {member.userEmail}
              </div>
            </div>
            <select
              value={member.role}
              onChange={(e) => handleRoleChange(member.userId, e.target.value)}
              style={{
                padding: "4px 8px",
                borderRadius: 6,
                border: "1px solid var(--tf-border)",
                background: "var(--tf-bg3)",
                color: "var(--tf-text2)",
                fontSize: 13,
              }}
            >
              {ROLES.map((role) => (
                <option key={role} value={role}>
                  {role}
                </option>
              ))}
            </select>
            <button
              onClick={() => handleRemove(member.userId, member.userName)}
              style={{
                padding: 6,
                borderRadius: 6,
                border: "none",
                background: "transparent",
                color: "var(--tf-text3)",
                cursor: "pointer",
              }}
              onMouseEnter={(e) => {
                e.currentTarget.style.color = "#ef4444";
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.color = "var(--tf-text3)";
              }}
              aria-label={`Remove ${member.userName}`}
            >
              <Trash2 size={14} />
            </button>
          </div>
        );
      })}
    </div>
  );
}
