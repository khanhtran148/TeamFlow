"use client";

import { useState } from "react";
import Link from "next/link";
import { Plus, Clock, Users } from "lucide-react";
import { toast } from "sonner";
import { LoadingSkeleton } from "@/components/shared/loading-skeleton";
import { Pagination } from "@/components/shared/pagination";
import {
  useRetroSessions,
  useCreateRetroSession,
} from "@/lib/hooks/use-retros";
import { useHasPermission } from "@/lib/hooks/use-permission";
import type { RetroSessionStatus, RetroSessionSummaryDto } from "@/lib/api/types";
import type { ApiError } from "@/lib/api/client";

interface RetroSessionListProps {
  projectId: string;
}

const STATUS_CONFIG: Record<
  RetroSessionStatus,
  { label: string; bg: string; color: string }
> = {
  Draft: { label: "Draft", bg: "var(--tf-bg4)", color: "var(--tf-text3)" },
  Open: {
    label: "Open",
    bg: "var(--tf-accent-dim2)",
    color: "var(--tf-accent)",
  },
  Voting: {
    label: "Voting",
    bg: "var(--tf-blue-dim)",
    color: "var(--tf-blue)",
  },
  Discussing: {
    label: "Discussing",
    bg: "var(--tf-orange-dim)",
    color: "var(--tf-orange)",
  },
  Closed: { label: "Closed", bg: "var(--tf-bg4)", color: "var(--tf-text3)" },
};

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
  });
}

export function RetroSessionList({ projectId }: RetroSessionListProps) {
  const [page, setPage] = useState(1);
  const [showCreateDialog, setShowCreateDialog] = useState(false);
  const canFacilitate = useHasPermission(projectId, "Retro_Facilitate");

  const { data, isLoading } = useRetroSessions(projectId, page, 10);
  const createMutation = useCreateRetroSession(projectId);

  async function handleCreate(anonymityMode: string) {
    try {
      await createMutation.mutateAsync({
        projectId,
        anonymityMode,
      });
      setShowCreateDialog(false);
      toast.success("Retro session created");
    } catch (err) {
      const apiErr = err as ApiError;
      toast.error(apiErr.message ?? "Failed to create session");
    }
  }

  if (isLoading) {
    return (
      <div style={{ padding: 20 }}>
        <LoadingSkeleton rows={5} type="list-row" />
      </div>
    );
  }

  const sessions = data?.items ?? [];
  const totalPages = data
    ? Math.ceil(data.totalCount / data.pageSize)
    : 1;

  return (
    <div style={{ padding: 20, display: "flex", flexDirection: "column", gap: 16 }}>
      {/* Header */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
        }}
      >
        <h2
          style={{
            fontFamily: "var(--tf-font-head)",
            fontWeight: 700,
            fontSize: 18,
            color: "var(--tf-text)",
            margin: 0,
          }}
        >
          Retrospectives
        </h2>

        {canFacilitate && (
          <button
            onClick={() => setShowCreateDialog(true)}
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 6,
              padding: "6px 14px",
              borderRadius: 6,
              fontSize: 12,
              fontWeight: 600,
              border: "none",
              background: "var(--tf-accent)",
              color: "var(--tf-bg)",
              cursor: "pointer",
              fontFamily: "var(--tf-font-body)",
            }}
          >
            <Plus size={13} />
            New Retro
          </button>
        )}
      </div>

      {/* Session list */}
      {sessions.length === 0 ? (
        <div
          style={{
            background: "var(--tf-bg2)",
            border: "1px solid var(--tf-border)",
            borderRadius: "var(--tf-radius)",
            padding: "40px 20px",
            textAlign: "center",
            color: "var(--tf-text3)",
            fontSize: 13,
          }}
        >
          No retrospective sessions yet.
        </div>
      ) : (
        <div
          style={{
            background: "var(--tf-bg2)",
            border: "1px solid var(--tf-border)",
            borderRadius: "var(--tf-radius)",
            overflow: "hidden",
          }}
        >
          {sessions.map((session, i) => {
            const statusConfig = STATUS_CONFIG[session.status];
            const isLast = i === sessions.length - 1;

            return (
              <Link
                key={session.id}
                href={`/projects/${projectId}/retros/${session.id}`}
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: 12,
                  padding: "14px 16px",
                  borderBottom: isLast
                    ? "none"
                    : "1px solid var(--tf-border)",
                  textDecoration: "none",
                  transition: "background var(--tf-tr)",
                }}
                onMouseEnter={(e) => {
                  (e.currentTarget as HTMLAnchorElement).style.background =
                    "var(--tf-bg3)";
                }}
                onMouseLeave={(e) => {
                  (e.currentTarget as HTMLAnchorElement).style.background =
                    "transparent";
                }}
              >
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: 8,
                    }}
                  >
                    <span
                      style={{
                        fontSize: 13,
                        fontWeight: 600,
                        color: "var(--tf-text)",
                      }}
                    >
                      Retro
                    </span>
                    <span
                      style={{
                        display: "inline-flex",
                        padding: "1px 8px",
                        borderRadius: 100,
                        fontSize: 10,
                        fontWeight: 600,
                        fontFamily: "var(--tf-font-mono)",
                        background: statusConfig.bg,
                        color: statusConfig.color,
                      }}
                    >
                      {statusConfig.label}
                    </span>
                    {session.anonymityMode === "Anonymous" && (
                      <span
                        style={{
                          display: "inline-flex",
                          alignItems: "center",
                          gap: 3,
                          padding: "1px 8px",
                          borderRadius: 100,
                          fontSize: 10,
                          fontWeight: 500,
                          background: "var(--tf-violet-dim)",
                          color: "var(--tf-violet)",
                        }}
                      >
                        <Users size={9} />
                        Anonymous
                      </span>
                    )}
                  </div>
                  <div
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: 10,
                      marginTop: 4,
                      fontSize: 11,
                      color: "var(--tf-text3)",
                    }}
                  >
                    <span>
                      Facilitator: {session.facilitatorName}
                    </span>
                    <span>
                      {session.cardCount} card{session.cardCount !== 1 ? "s" : ""}
                    </span>
                    <span>
                      {session.actionItemCount} action item
                      {session.actionItemCount !== 1 ? "s" : ""}
                    </span>
                  </div>
                </div>

                <span
                  style={{
                    display: "inline-flex",
                    alignItems: "center",
                    gap: 4,
                    fontSize: 11,
                    color: "var(--tf-text3)",
                    fontFamily: "var(--tf-font-mono)",
                    flexShrink: 0,
                  }}
                >
                  <Clock size={11} />
                  {formatDate(session.createdAt)}
                </span>
              </Link>
            );
          })}
        </div>
      )}

      {data && data.totalCount > data.pageSize && (
        <Pagination
          page={page}
          pageSize={data.pageSize}
          totalCount={data.totalCount}
          onPageChange={setPage}
        />
      )}

      {/* Simple create dialog */}
      {showCreateDialog && (
        <div
          style={{
            position: "fixed",
            inset: 0,
            background: "rgba(0,0,0,0.5)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            zIndex: 100,
          }}
          onClick={() => setShowCreateDialog(false)}
        >
          <div
            onClick={(e) => e.stopPropagation()}
            style={{
              background: "var(--tf-bg2)",
              border: "1px solid var(--tf-border)",
              borderRadius: 12,
              padding: 24,
              width: 360,
              display: "flex",
              flexDirection: "column",
              gap: 16,
            }}
          >
            <h3
              style={{
                fontFamily: "var(--tf-font-head)",
                fontWeight: 700,
                fontSize: 16,
                color: "var(--tf-text)",
                margin: 0,
              }}
            >
              New Retrospective
            </h3>
            <p
              style={{
                fontSize: 13,
                color: "var(--tf-text3)",
                margin: 0,
              }}
            >
              Choose the anonymity mode for this session.
            </p>
            <div style={{ display: "flex", gap: 8 }}>
              <button
                onClick={() => handleCreate("Public")}
                disabled={createMutation.isPending}
                style={{
                  flex: 1,
                  padding: "10px 14px",
                  borderRadius: 8,
                  border: "1px solid var(--tf-border)",
                  background: "transparent",
                  color: "var(--tf-text)",
                  fontSize: 13,
                  fontWeight: 600,
                  cursor: "pointer",
                  fontFamily: "var(--tf-font-body)",
                  transition: "all var(--tf-tr)",
                }}
                onMouseEnter={(e) => {
                  (e.currentTarget as HTMLButtonElement).style.borderColor =
                    "var(--tf-accent)";
                }}
                onMouseLeave={(e) => {
                  (e.currentTarget as HTMLButtonElement).style.borderColor =
                    "var(--tf-border)";
                }}
              >
                Public
              </button>
              <button
                onClick={() => handleCreate("Anonymous")}
                disabled={createMutation.isPending}
                style={{
                  flex: 1,
                  padding: "10px 14px",
                  borderRadius: 8,
                  border: "1px solid var(--tf-border)",
                  background: "transparent",
                  color: "var(--tf-text)",
                  fontSize: 13,
                  fontWeight: 600,
                  cursor: "pointer",
                  fontFamily: "var(--tf-font-body)",
                  transition: "all var(--tf-tr)",
                }}
                onMouseEnter={(e) => {
                  (e.currentTarget as HTMLButtonElement).style.borderColor =
                    "var(--tf-violet)";
                }}
                onMouseLeave={(e) => {
                  (e.currentTarget as HTMLButtonElement).style.borderColor =
                    "var(--tf-border)";
                }}
              >
                Anonymous
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
