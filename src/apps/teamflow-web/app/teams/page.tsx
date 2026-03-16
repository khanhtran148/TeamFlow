"use client";

import { useState } from "react";
import { Plus } from "lucide-react";
import { TopBar } from "@/components/layout/top-bar";
import { Breadcrumb } from "@/components/layout/breadcrumb";
import { TeamCard } from "@/components/teams/team-card";
import { CreateTeamDialog } from "@/components/teams/create-team-dialog";
import { LoadingSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { ErrorDisplay } from "@/components/shared/error-display";
import { useTeams } from "@/lib/hooks/use-teams";
import { useAuthStore } from "@/lib/stores/auth-store";

// Default org ID — in production, this would come from user context
const DEFAULT_ORG_ID = "00000000-0000-0000-0000-000000000010";

export default function TeamsPage() {
  const [showCreate, setShowCreate] = useState(false);
  const user = useAuthStore((s) => s.user);
  const { data, isLoading, error } = useTeams(DEFAULT_ORG_ID);

  return (
    <div style={{ minHeight: "100vh", background: "var(--tf-bg)" }}>
      <TopBar
        breadcrumb={
          <Breadcrumb segments={[{ label: "Teams", bold: true }]} />
        }
        actions={
          <button
            onClick={() => setShowCreate(true)}
            style={{
              display: "flex",
              alignItems: "center",
              gap: 5,
              padding: "6px 12px",
              borderRadius: 8,
              border: "none",
              background:
                "linear-gradient(135deg, var(--tf-accent), var(--tf-blue))",
              color: "#0a0a0b",
              fontWeight: 600,
              fontSize: 13,
              cursor: "pointer",
            }}
          >
            <Plus size={14} />
            New Team
          </button>
        }
      />

      <main style={{ padding: 24, maxWidth: 800, margin: "0 auto" }}>
        {isLoading && <LoadingSkeleton rows={5} />}

        {error && <ErrorDisplay error={error} />}

        {data && data.items.length === 0 && (
          <EmptyState
            title="No teams yet"
            description="Create your first team to start collaborating"
          />
        )}

        {data && data.items.length > 0 && (
          <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
            {data.items.map((team) => (
              <TeamCard key={team.id} team={team} />
            ))}
          </div>
        )}
      </main>

      <CreateTeamDialog
        orgId={DEFAULT_ORG_ID}
        open={showCreate}
        onClose={() => setShowCreate(false)}
      />
    </div>
  );
}
