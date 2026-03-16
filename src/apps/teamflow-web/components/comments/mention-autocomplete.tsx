"use client";

import { useMemo } from "react";

interface MentionAutocompleteProps {
  projectId: string;
  query: string;
  onSelect: (userName: string) => void;
  onClose: () => void;
}

export function MentionAutocomplete({
  projectId,
  query,
  onSelect,
  onClose,
}: MentionAutocompleteProps) {
  // Use project memberships to get project members for @mention
  const { data: memberships } = useProjectMemberships(projectId);

  const filtered = useMemo(() => {
    if (!memberships) return [];
    return memberships
      .filter((m) =>
        m.memberName.toLowerCase().includes(query.toLowerCase()),
      )
      .slice(0, 8);
  }, [memberships, query]);

  if (filtered.length === 0) return null;

  return (
    <div
      style={{
        position: "absolute",
        bottom: "100%",
        left: 0,
        marginBottom: 4,
        background: "var(--tf-bg2)",
        border: "1px solid var(--tf-border)",
        borderRadius: 8,
        boxShadow: "0 4px 12px rgba(0,0,0,0.15)",
        zIndex: 50,
        maxHeight: 200,
        overflow: "auto",
        minWidth: 200,
      }}
    >
      {filtered.map((member) => (
        <button
          key={member.id}
          onClick={() => onSelect(member.memberName)}
          style={{
            display: "flex",
            alignItems: "center",
            gap: 8,
            width: "100%",
            padding: "8px 12px",
            background: "transparent",
            border: "none",
            cursor: "pointer",
            fontSize: 12,
            color: "var(--tf-text)",
            fontFamily: "var(--tf-font-body)",
            textAlign: "left",
            transition: "background var(--tf-tr)",
          }}
          onMouseEnter={(e) => {
            (e.currentTarget as HTMLButtonElement).style.background =
              "var(--tf-bg3)";
          }}
          onMouseLeave={(e) => {
            (e.currentTarget as HTMLButtonElement).style.background =
              "transparent";
          }}
        >
          <div
            style={{
              width: 22,
              height: 22,
              borderRadius: "50%",
              background: "var(--tf-accent-dim2)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              fontSize: 10,
              fontWeight: 600,
              color: "var(--tf-accent)",
              flexShrink: 0,
            }}
          >
            {member.memberName.charAt(0).toUpperCase()}
          </div>
          <span style={{ fontWeight: 500 }}>{member.memberName}</span>
          <span style={{ color: "var(--tf-text3)", fontSize: 11 }}>
            {member.role}
          </span>
        </button>
      ))}
    </div>
  );
}

// Local hook to fetch project memberships for mention autocomplete
import { useQuery } from "@tanstack/react-query";
import { getProjectMemberships, type ProjectMembershipDto } from "@/lib/api/teams";

function useProjectMemberships(projectId: string) {
  return useQuery({
    queryKey: ["project-memberships", projectId],
    queryFn: () => getProjectMemberships(projectId),
    enabled: !!projectId,
    staleTime: 60_000,
  });
}
