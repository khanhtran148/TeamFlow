"use client";

import { useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { Trash2, UserPlus } from "lucide-react";
import { TopBar } from "@/components/layout/top-bar";
import { Breadcrumb, BreadcrumbSeparator } from "@/components/layout/breadcrumb";
import { MemberList } from "@/components/teams/member-list";
import { AddMemberDialog } from "@/components/teams/add-member-dialog";
import { LoadingSkeleton } from "@/components/shared/loading-skeleton";
import { ErrorDisplay } from "@/components/shared/error-display";
import { useTeam, useDeleteTeam } from "@/lib/hooks/use-teams";
import { toast } from "sonner";

export default function TeamDetailPage() {
  const params = useParams();
  const router = useRouter();
  const teamId = params.teamId as string;
  const { data: team, isLoading, error } = useTeam(teamId);
  const deleteTeam = useDeleteTeam();
  const [showAddMember, setShowAddMember] = useState(false);

  async function handleDelete() {
    if (!team) return;
    if (!confirm(`Delete team "${team.name}"? This cannot be undone.`)) return;
    try {
      await deleteTeam.mutateAsync(teamId);
      toast.success("Team deleted");
      router.push("/teams");
    } catch {
      toast.error("Failed to delete team");
    }
  }

  return (
    <div style={{ minHeight: "100vh", background: "var(--tf-bg)" }}>
      <TopBar
        breadcrumb={
          <Breadcrumb>
            <a href="/teams" style={{ color: "var(--tf-text2)", textDecoration: "none", fontSize: 12 }}>
              Teams
            </a>
            <BreadcrumbSeparator />
            <span style={{ color: "var(--tf-text)", fontWeight: 500, fontSize: 12 }}>
              {team?.name ?? "..."}
            </span>
          </Breadcrumb>
        }
        actions={
          <div style={{ display: "flex", gap: 6 }}>
            <button
              onClick={() => setShowAddMember(true)}
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
                fontSize: 12,
                cursor: "pointer",
              }}
            >
              <UserPlus size={14} />
              Add Member
            </button>
            <button
              onClick={handleDelete}
              style={{
                display: "flex",
                alignItems: "center",
                gap: 5,
                padding: "6px 12px",
                borderRadius: 8,
                border: "1px solid var(--tf-border)",
                background: "transparent",
                color: "var(--tf-text2)",
                fontSize: 12,
                cursor: "pointer",
              }}
            >
              <Trash2 size={14} />
            </button>
          </div>
        }
      />

      <main style={{ padding: 24, maxWidth: 800, margin: "0 auto" }}>
        {isLoading && <LoadingSkeleton rows={6} />}
        {error && <ErrorDisplay error={error} />}

        {team && (
          <div>
            <div style={{ marginBottom: 24 }}>
              <h1
                style={{
                  fontSize: 22,
                  fontWeight: 700,
                  color: "var(--tf-text)",
                  fontFamily: "var(--tf-font-head)",
                  margin: 0,
                }}
              >
                {team.name}
              </h1>
              {team.description && (
                <p style={{ fontSize: 13, color: "var(--tf-text3)", marginTop: 6 }}>
                  {team.description}
                </p>
              )}
            </div>

            <div
              style={{
                padding: 16,
                borderRadius: 10,
                border: "1px solid var(--tf-border)",
                background: "var(--tf-bg2)",
              }}
            >
              <h2
                style={{
                  fontSize: 14,
                  fontWeight: 600,
                  color: "var(--tf-text)",
                  margin: "0 0 12px",
                }}
              >
                Members ({team.members?.length ?? 0})
              </h2>
              <MemberList teamId={teamId} members={team.members ?? []} />
            </div>
          </div>
        )}
      </main>

      <AddMemberDialog
        teamId={teamId}
        open={showAddMember}
        onClose={() => setShowAddMember(false)}
      />
    </div>
  );
}
