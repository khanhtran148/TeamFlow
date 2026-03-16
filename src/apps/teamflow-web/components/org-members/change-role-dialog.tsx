"use client";

import { useState } from "react";
import type { OrgRole, OrganizationMemberDto } from "@/lib/api/types";

const ROLE_OPTIONS: { value: OrgRole; label: string; description: string }[] = [
  { value: "Owner", label: "Owner", description: "Full control, can manage all settings and members." },
  { value: "Admin", label: "Admin", description: "Can manage members and settings, but not transfer ownership." },
  { value: "Member", label: "Member", description: "Can access org resources and participate." },
];

interface ChangeRoleDialogProps {
  open: boolean;
  member: OrganizationMemberDto;
  currentUserRole: OrgRole;
  isPending: boolean;
  onConfirm: (role: OrgRole) => void;
  onCancel: () => void;
}

export function ChangeRoleDialog({
  open,
  member,
  currentUserRole,
  isPending,
  onConfirm,
  onCancel,
}: ChangeRoleDialogProps) {
  const [selectedRole, setSelectedRole] = useState<OrgRole>(member.role);

  if (!open) return null;

  // Admin cannot promote to Owner
  const availableRoles = currentUserRole === "Admin"
    ? ROLE_OPTIONS.filter((r) => r.value !== "Owner")
    : ROLE_OPTIONS;

  return (
    <div
      style={{
        position: "fixed",
        inset: 0,
        zIndex: 60,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        background: "rgba(0,0,0,0.6)",
        backdropFilter: "blur(2px)",
      }}
      onClick={onCancel}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="change-role-dialog-title"
        onClick={(e) => e.stopPropagation()}
        style={{
          background: "var(--tf-bg2)",
          border: "1px solid var(--tf-border)",
          borderRadius: "var(--tf-radius)",
          boxShadow: "var(--tf-shadow)",
          width: "100%",
          maxWidth: 400,
          padding: 20,
          display: "flex",
          flexDirection: "column",
          gap: 16,
        }}
      >
        <h3
          id="change-role-dialog-title"
          style={{
            fontFamily: "var(--tf-font-head)",
            fontWeight: 700,
            fontSize: 14,
            color: "var(--tf-text)",
          }}
        >
          Change Role for {member.userName}
        </h3>

        <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
          {availableRoles.map((option) => (
            <button
              key={option.value}
              type="button"
              onClick={() => setSelectedRole(option.value)}
              style={{
                display: "flex",
                flexDirection: "column",
                gap: 2,
                padding: "10px 12px",
                borderRadius: 6,
                border: `1px solid ${
                  selectedRole === option.value
                    ? "var(--tf-accent)"
                    : "var(--tf-border)"
                }`,
                background:
                  selectedRole === option.value
                    ? "var(--tf-accent-dim)"
                    : "var(--tf-bg3)",
                cursor: "pointer",
                textAlign: "left",
                transition: "all var(--tf-tr)",
              }}
            >
              <span
                style={{
                  fontSize: 13,
                  fontWeight: 600,
                  color:
                    selectedRole === option.value
                      ? "var(--tf-accent)"
                      : "var(--tf-text)",
                  fontFamily: "var(--tf-font-body)",
                }}
              >
                {option.label}
              </span>
              <span
                style={{
                  fontSize: 12,
                  color: "var(--tf-text3)",
                  fontFamily: "var(--tf-font-body)",
                }}
              >
                {option.description}
              </span>
            </button>
          ))}
        </div>

        <div style={{ display: "flex", justifyContent: "flex-end", gap: 8 }}>
          <button
            type="button"
            onClick={onCancel}
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
              fontFamily: "var(--tf-font-body)",
            }}
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={() => onConfirm(selectedRole)}
            disabled={isPending || selectedRole === member.role}
            style={{
              padding: "7px 14px",
              borderRadius: 6,
              border: "1px solid var(--tf-accent)",
              background: "var(--tf-accent)",
              color: "var(--primary-foreground)",
              fontSize: 13,
              fontWeight: 600,
              cursor:
                isPending || selectedRole === member.role
                  ? "not-allowed"
                  : "pointer",
              opacity: isPending || selectedRole === member.role ? 0.6 : 1,
              fontFamily: "var(--tf-font-body)",
            }}
          >
            {isPending ? "Saving..." : "Save Role"}
          </button>
        </div>
      </div>
    </div>
  );
}
