"use client";

import { useState } from "react";
import { toast } from "sonner";
import { UserMinus, ShieldCheck } from "lucide-react";
import { ChangeRoleDialog } from "./change-role-dialog";
import { RemoveMemberDialog } from "./remove-member-dialog";
import { useChangeOrgMemberRole, useRemoveOrgMember } from "@/lib/hooks/use-org-members";
import type { OrgRole, OrganizationMemberDto } from "@/lib/api/types";
import type { ApiError } from "@/lib/api/client";

const ROLE_BADGE_STYLES: Record<
  OrgRole,
  { background: string; color: string }
> = {
  Owner: { background: "var(--tf-accent-dim)", color: "var(--tf-accent)" },
  Admin: { background: "var(--tf-yellow-dim)", color: "var(--tf-yellow)" },
  Member: { background: "var(--tf-bg3)", color: "var(--tf-text3)" },
};

interface MemberListProps {
  orgId: string;
  members: OrganizationMemberDto[];
  currentUserId: string;
  currentUserRole: OrgRole | undefined;
}

export function MemberList({
  orgId,
  members,
  currentUserId,
  currentUserRole,
}: MemberListProps) {
  const [changeRoleTarget, setChangeRoleTarget] =
    useState<OrganizationMemberDto | null>(null);
  const [removeTarget, setRemoveTarget] =
    useState<OrganizationMemberDto | null>(null);

  const { mutate: changeRole, isPending: isChangingRole } =
    useChangeOrgMemberRole(orgId);
  const { mutate: removeMember, isPending: isRemoving } =
    useRemoveOrgMember(orgId);

  const canManage =
    currentUserRole === "Owner" || currentUserRole === "Admin";

  function handleChangeRoleConfirm(newRole: OrgRole) {
    if (!changeRoleTarget) return;
    changeRole(
      { userId: changeRoleTarget.userId, role: newRole },
      {
        onSuccess: () => {
          toast.success(`${changeRoleTarget.userName}'s role updated to ${newRole}.`);
          setChangeRoleTarget(null);
        },
        onError: (err) => {
          const apiErr = err as ApiError;
          toast.error(apiErr.message ?? "Failed to change role.");
          setChangeRoleTarget(null);
        },
      },
    );
  }

  function handleRemoveConfirm() {
    if (!removeTarget) return;
    removeMember(removeTarget.userId, {
      onSuccess: () => {
        toast.success(`${removeTarget.userName} has been removed.`);
        setRemoveTarget(null);
      },
      onError: (err) => {
        const apiErr = err as ApiError;
        toast.error(apiErr.message ?? "Failed to remove member.");
        setRemoveTarget(null);
      },
    });
  }

  return (
    <>
      <div
        style={{
          background: "var(--tf-bg2)",
          border: "1px solid var(--tf-border)",
          borderRadius: "var(--tf-radius)",
          overflow: "hidden",
        }}
      >
        {/* Header row */}
        <div
          style={{
            display: "grid",
            gridTemplateColumns: "1fr 200px 140px 120px",
            gap: 12,
            padding: "10px 16px",
            borderBottom: "1px solid var(--tf-border)",
            background: "var(--tf-bg3)",
          }}
        >
          {["Member", "Email", "Role", ""].map((h) => (
            <span
              key={h}
              style={{
                fontSize: 11,
                fontWeight: 600,
                color: "var(--tf-text3)",
                textTransform: "uppercase",
                letterSpacing: "0.05em",
                fontFamily: "var(--tf-font-mono)",
              }}
            >
              {h}
            </span>
          ))}
        </div>

        {/* Member rows */}
        {members.map((member) => {
          const isCurrentUser = member.userId === currentUserId;
          const badgeStyle = ROLE_BADGE_STYLES[member.role];

          return (
            <div
              key={member.userId}
              style={{
                display: "grid",
                gridTemplateColumns: "1fr 200px 140px 120px",
                gap: 12,
                padding: "12px 16px",
                borderBottom: "1px solid var(--tf-border)",
                alignItems: "center",
                background: isCurrentUser ? "var(--tf-accent-dim2)" : undefined,
                transition: "background var(--tf-tr)",
              }}
            >
              {/* Name */}
              <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                <div
                  style={{
                    width: 30,
                    height: 30,
                    borderRadius: "50%",
                    background: "var(--tf-accent-dim)",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    fontSize: 12,
                    fontWeight: 700,
                    color: "var(--tf-accent)",
                    flexShrink: 0,
                    fontFamily: "var(--tf-font-mono)",
                  }}
                >
                  {member.userName.charAt(0).toUpperCase()}
                </div>
                <div>
                  <div
                    style={{
                      fontSize: 13,
                      fontWeight: 600,
                      color: "var(--tf-text)",
                      fontFamily: "var(--tf-font-body)",
                    }}
                  >
                    {member.userName}
                    {isCurrentUser && (
                      <span
                        style={{
                          marginLeft: 6,
                          fontSize: 11,
                          fontWeight: 500,
                          color: "var(--tf-accent)",
                          fontFamily: "var(--tf-font-mono)",
                        }}
                      >
                        (you)
                      </span>
                    )}
                  </div>
                </div>
              </div>

              {/* Email */}
              <span
                style={{
                  fontSize: 12,
                  color: "var(--tf-text3)",
                  fontFamily: "var(--tf-font-mono)",
                  overflow: "hidden",
                  textOverflow: "ellipsis",
                  whiteSpace: "nowrap",
                }}
              >
                {member.userEmail}
              </span>

              {/* Role badge */}
              <div>
                <span
                  style={{
                    display: "inline-flex",
                    alignItems: "center",
                    gap: 4,
                    padding: "3px 8px",
                    borderRadius: 100,
                    fontSize: 11,
                    fontWeight: 600,
                    fontFamily: "var(--tf-font-mono)",
                    ...badgeStyle,
                  }}
                >
                  {member.role === "Owner" && <ShieldCheck size={10} />}
                  {member.role}
                </span>
              </div>

              {/* Actions */}
              <div style={{ display: "flex", gap: 6, justifyContent: "flex-end" }}>
                {canManage && !isCurrentUser && (
                  <>
                    <button
                      type="button"
                      title="Change role"
                      onClick={() => setChangeRoleTarget(member)}
                      style={{
                        display: "flex",
                        alignItems: "center",
                        gap: 4,
                        padding: "4px 10px",
                        borderRadius: 5,
                        border: "1px solid var(--tf-border)",
                        background: "transparent",
                        color: "var(--tf-text2)",
                        fontSize: 12,
                        cursor: "pointer",
                        fontFamily: "var(--tf-font-body)",
                        transition: "all var(--tf-tr)",
                        whiteSpace: "nowrap",
                      }}
                      onMouseEnter={(e) => {
                        e.currentTarget.style.borderColor = "var(--tf-accent)";
                        e.currentTarget.style.color = "var(--tf-accent)";
                      }}
                      onMouseLeave={(e) => {
                        e.currentTarget.style.borderColor = "var(--tf-border)";
                        e.currentTarget.style.color = "var(--tf-text2)";
                      }}
                    >
                      Role
                    </button>
                    <button
                      type="button"
                      title="Remove member"
                      onClick={() => setRemoveTarget(member)}
                      style={{
                        display: "flex",
                        alignItems: "center",
                        padding: "4px 8px",
                        borderRadius: 5,
                        border: "1px solid var(--tf-border)",
                        background: "transparent",
                        color: "var(--tf-text3)",
                        cursor: "pointer",
                        transition: "all var(--tf-tr)",
                      }}
                      onMouseEnter={(e) => {
                        e.currentTarget.style.borderColor = "var(--tf-red)";
                        e.currentTarget.style.color = "var(--tf-red)";
                        e.currentTarget.style.background = "var(--tf-red-dim)";
                      }}
                      onMouseLeave={(e) => {
                        e.currentTarget.style.borderColor = "var(--tf-border)";
                        e.currentTarget.style.color = "var(--tf-text3)";
                        e.currentTarget.style.background = "transparent";
                      }}
                    >
                      <UserMinus size={13} />
                    </button>
                  </>
                )}
              </div>
            </div>
          );
        })}
      </div>

      {/* Change role dialog */}
      {changeRoleTarget && currentUserRole && (
        <ChangeRoleDialog
          open={!!changeRoleTarget}
          member={changeRoleTarget}
          currentUserRole={currentUserRole}
          isPending={isChangingRole}
          onConfirm={handleChangeRoleConfirm}
          onCancel={() => setChangeRoleTarget(null)}
        />
      )}

      {/* Remove member dialog */}
      {removeTarget && (
        <RemoveMemberDialog
          open={!!removeTarget}
          member={removeTarget}
          isPending={isRemoving}
          onConfirm={handleRemoveConfirm}
          onCancel={() => setRemoveTarget(null)}
        />
      )}
    </>
  );
}
