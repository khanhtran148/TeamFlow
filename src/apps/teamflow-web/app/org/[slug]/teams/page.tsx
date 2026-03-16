"use client";

import { useState } from "react";
import { useParams } from "next/navigation";
import { Plus } from "lucide-react";
import { TopBar } from "@/components/layout/top-bar";
import { Breadcrumb } from "@/components/layout/breadcrumb";
import { OrgSwitcher } from "@/components/layout/org-switcher";
import { TeamCard } from "@/components/teams/team-card";
import { CreateTeamDialog } from "@/components/teams/create-team-dialog";
import { LoadingSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { ErrorDisplay } from "@/components/shared/error-display";
import { useTeams } from "@/lib/hooks/use-teams";
import { useOrgContext } from "@/lib/contexts/org-context";

export default function OrgTeamsPage() {
  const params = useParams();
  const slug = params.slug as string;
  const { org } = useOrgContext();
  const [showCreate, setShowCreate] = useState(false);
  const { data, isLoading, error } = useTeams(org.id);

  const breadcrumb = (
    <Breadcrumb
      segments={[
        { label: org.name, href: `/org/${slug}/projects` },
        { label: "Teams", bold: true },
      ]}
    />
  );

  return (
    <div style={{ minHeight: "100vh", background: "var(--tf-bg)" }}>
      <TopBar
        breadcrumb={breadcrumb}
        actions={
          <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
            <OrgSwitcher currentSlug={slug} />
            <button
              onClick={() => setShowCreate(true)}
              style={{
                display: "flex",
                alignItems: "center",
                gap: 5,
                padding: "6px 12px",
                borderRadius: 8,
                border: "none",
                background: "linear-gradient(135deg, var(--tf-accent), var(--tf-blue))",
                color: "#0a0a0b",
                fontWeight: 600,
                fontSize: 13,
                cursor: "pointer",
              }}
            >
              <Plus size={14} />
              New Team
            </button>
          </div>
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
              <TeamCard key={team.id} team={team} orgSlug={slug} />
            ))}
          </div>
        )}
      </main>

      <CreateTeamDialog
        orgId={org.id}
        open={showCreate}
        onClose={() => setShowCreate(false)}
      />
    </div>
  );
}
