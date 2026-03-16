"use client";

import type { SprintCapacityMemberDto } from "@/lib/api/types";

interface MemberCapacityProps {
  members: SprintCapacityMemberDto[];
}

export function MemberCapacity({ members }: MemberCapacityProps) {
  if (members.length === 0) {
    return (
      <p style={{ fontSize: 13, color: "var(--tf-text3)", textAlign: "center", padding: "12px 0" }}>
        No capacity configured. Edit sprint capacity to assign points per member.
      </p>
    );
  }

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
      <span
        style={{
          fontSize: 13,
          fontWeight: 600,
          color: "var(--tf-text2)",
          fontFamily: "var(--tf-font-mono)",
          textTransform: "uppercase",
          letterSpacing: "0.05em",
        }}
      >
        Per-member breakdown
      </span>
      <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
        {members.map((member) => {
          const pct =
            member.capacityPoints > 0
              ? (member.assignedPoints / member.capacityPoints) * 100
              : 0;
          const isOver = member.assignedPoints > member.capacityPoints;

          return (
            <div
              key={member.memberId}
              style={{
                display: "flex",
                alignItems: "center",
                gap: 10,
                padding: "6px 0",
              }}
            >
              <span
                style={{
                  fontSize: 13,
                  color: "var(--tf-text)",
                  fontWeight: 500,
                  minWidth: 100,
                  overflow: "hidden",
                  textOverflow: "ellipsis",
                  whiteSpace: "nowrap",
                }}
              >
                {member.memberName}
              </span>
              <div
                style={{
                  flex: 1,
                  height: 4,
                  borderRadius: 100,
                  background: "var(--tf-bg4)",
                  overflow: "hidden",
                }}
              >
                <div
                  style={{
                    height: "100%",
                    width: `${Math.min(pct, 100)}%`,
                    borderRadius: 100,
                    background: isOver ? "var(--tf-red)" : "var(--tf-accent)",
                    transition: "width 0.3s ease",
                  }}
                />
              </div>
              <span
                style={{
                  fontSize: 13,
                  color: isOver ? "var(--tf-red)" : "var(--tf-text3)",
                  fontFamily: "var(--tf-font-mono)",
                  fontWeight: isOver ? 600 : 400,
                  whiteSpace: "nowrap",
                  minWidth: 50,
                  textAlign: "right",
                }}
              >
                {member.assignedPoints}/{member.capacityPoints}
              </span>
            </div>
          );
        })}
      </div>
    </div>
  );
}
