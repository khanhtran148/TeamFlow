"use client";

import Link from "next/link";
import { Users } from "lucide-react";
import type { TeamDto } from "@/lib/api/teams";

interface TeamCardProps {
  team: TeamDto;
  orgSlug?: string;
}

export function TeamCard({ team, orgSlug }: TeamCardProps) {
  const href = orgSlug ? `/org/${orgSlug}/teams/${team.id}` : `/teams/${team.id}`;
  return (
    <Link
      href={href}
      style={{
        display: "block",
        padding: 16,
        borderRadius: 10,
        border: "1px solid var(--tf-border)",
        background: "var(--tf-bg2)",
        textDecoration: "none",
        transition: "border-color 0.2s",
      }}
      onMouseEnter={(e) => {
        e.currentTarget.style.borderColor = "var(--tf-border2)";
      }}
      onMouseLeave={(e) => {
        e.currentTarget.style.borderColor = "var(--tf-border)";
      }}
    >
      <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
        <div
          style={{
            width: 36,
            height: 36,
            borderRadius: 8,
            background: "var(--tf-bg3)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            color: "var(--tf-accent)",
          }}
        >
          <Users size={18} />
        </div>
        <div>
          <div
            style={{
              fontSize: 14,
              fontWeight: 600,
              color: "var(--tf-text)",
            }}
          >
            {team.name}
          </div>
          {team.description && (
            <div
              style={{
                fontSize: 13,
                color: "var(--tf-text3)",
                marginTop: 2,
              }}
            >
              {team.description}
            </div>
          )}
        </div>
        <div
          style={{
            marginLeft: "auto",
            fontSize: 13,
            color: "var(--tf-text2)",
            display: "flex",
            alignItems: "center",
            gap: 4,
          }}
        >
          <Users size={13} />
          {team.memberCount}
        </div>
      </div>
    </Link>
  );
}
