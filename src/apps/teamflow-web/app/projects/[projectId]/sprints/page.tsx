"use client";

import { useState } from "react";
import { Plus, Zap } from "lucide-react";
import { toast } from "sonner";
import { useProjectContext } from "@/lib/contexts/project-context";
import { useSprints, useDeleteSprint } from "@/lib/hooks/use-sprints";
import { useHasPermission } from "@/lib/hooks/use-permission";
import { SprintCard } from "@/components/sprints/sprint-card";
import { SprintFormDialog } from "@/components/sprints/sprint-form-dialog";
import { ConfirmDialog } from "@/components/projects/confirm-dialog";
import { EmptyState } from "@/components/shared/empty-state";
import { Skeleton } from "@/components/ui/skeleton";
import type { SprintDto } from "@/lib/api/types";
import type { ApiError } from "@/lib/api/client";

export default function SprintsPage() {
  const { project } = useProjectContext();
  const [createOpen, setCreateOpen] = useState(false);
  const [editTarget, setEditTarget] = useState<SprintDto | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<SprintDto | null>(null);

  const { data, isLoading, isError } = useSprints({ projectId: project.id });
  const { mutate: deleteSprint, isPending: isDeleting } = useDeleteSprint(project.id);
  const canCreate = useHasPermission(project.id, "Sprint_Create");

  function handleDeleteConfirm() {
    if (!deleteTarget) return;
    deleteSprint(deleteTarget.id, {
      onSuccess: () => {
        toast.success(`Sprint "${deleteTarget.name}" deleted.`);
        setDeleteTarget(null);
      },
      onError: (err) => {
        const apiErr = err as ApiError;
        toast.error(apiErr.message ?? "Failed to delete sprint.");
      },
    });
  }

  const sprints = data?.items ?? [];

  return (
    <div style={{ padding: "20px", display: "flex", flexDirection: "column", gap: 16 }}>
      {/* Page header */}
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        <div>
          <h1
            style={{
              fontFamily: "var(--tf-font-head)",
              fontWeight: 700,
              fontSize: 18,
              color: "var(--tf-text)",
              margin: 0,
            }}
          >
            Sprints
          </h1>
          {!isLoading && !isError && (
            <p style={{ fontSize: 12, color: "var(--tf-text3)", marginTop: 3 }}>
              {sprints.length} {sprints.length === 1 ? "sprint" : "sprints"} in {project.name}
            </p>
          )}
        </div>

        {canCreate && (
          <button
            onClick={() => setCreateOpen(true)}
            aria-label="Create new sprint"
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 6,
              padding: "7px 14px",
              borderRadius: 6,
              border: "1px solid var(--tf-accent)",
              background: "var(--tf-accent)",
              color: "var(--primary-foreground)",
              fontSize: 12,
              fontWeight: 600,
              cursor: "pointer",
              fontFamily: "var(--tf-font-body)",
              transition: "opacity var(--tf-tr)",
            }}
            onMouseEnter={(e) => {
              (e.currentTarget as HTMLButtonElement).style.opacity = "0.85";
            }}
            onMouseLeave={(e) => {
              (e.currentTarget as HTMLButtonElement).style.opacity = "1";
            }}
          >
            <Plus size={13} />
            New Sprint
          </button>
        )}
      </div>

      {/* Content */}
      {isLoading ? (
        <div
          style={{
            display: "grid",
            gridTemplateColumns: "repeat(auto-fill, minmax(320px, 1fr))",
            gap: 12,
          }}
        >
          {Array.from({ length: 4 }).map((_, i) => (
            <div
              key={i}
              style={{
                background: "var(--tf-bg2)",
                border: "1px solid var(--tf-border)",
                borderRadius: "var(--tf-radius)",
                padding: "14px 16px",
                display: "flex",
                flexDirection: "column",
                gap: 10,
              }}
            >
              <div style={{ display: "flex", gap: 8 }}>
                <Skeleton style={{ width: 28, height: 28, borderRadius: 6, flexShrink: 0 }} />
                <div style={{ flex: 1, display: "flex", flexDirection: "column", gap: 6 }}>
                  <Skeleton style={{ height: 14, width: "60%", borderRadius: 4 }} />
                  <Skeleton style={{ height: 12, width: "85%", borderRadius: 4 }} />
                </div>
              </div>
              <Skeleton style={{ height: 4, borderRadius: 100 }} />
              <div style={{ display: "flex", gap: 8 }}>
                <Skeleton style={{ height: 11, width: 100, borderRadius: 4 }} />
              </div>
            </div>
          ))}
        </div>
      ) : isError ? (
        <div
          style={{
            background: "var(--tf-bg2)",
            border: "1px solid var(--tf-border)",
            borderRadius: "var(--tf-radius)",
            padding: "40px 20px",
            textAlign: "center",
            color: "var(--tf-red)",
            fontSize: 13,
          }}
        >
          Failed to load sprints. Please try again.
        </div>
      ) : sprints.length === 0 ? (
        <div
          style={{
            background: "var(--tf-bg2)",
            border: "1px solid var(--tf-border)",
            borderRadius: "var(--tf-radius)",
          }}
        >
          <EmptyState
            icon={Zap}
            title="No sprints yet"
            description="Create a sprint to plan and track work in time-boxed iterations."
            action={
              <button
                onClick={() => setCreateOpen(true)}
                style={{
                  padding: "6px 14px",
                  background: "var(--tf-accent)",
                  color: "var(--tf-bg)",
                  border: "none",
                  borderRadius: 6,
                  fontSize: 12,
                  fontWeight: 600,
                  cursor: "pointer",
                  fontFamily: "var(--tf-font-body)",
                }}
              >
                New Sprint
              </button>
            }
          />
        </div>
      ) : (
        <div
          style={{
            display: "grid",
            gridTemplateColumns: "repeat(auto-fill, minmax(320px, 1fr))",
            gap: 12,
          }}
        >
          {sprints.map((sprint) => (
            <SprintCard
              key={sprint.id}
              sprint={sprint}
              projectId={project.id}
              onEdit={setEditTarget}
              onDelete={setDeleteTarget}
            />
          ))}
        </div>
      )}

      {/* Dialogs */}
      <SprintFormDialog
        open={createOpen}
        projectId={project.id}
        onClose={() => setCreateOpen(false)}
      />

      <SprintFormDialog
        open={editTarget !== null}
        sprint={editTarget}
        projectId={project.id}
        onClose={() => setEditTarget(null)}
      />

      <ConfirmDialog
        open={deleteTarget !== null}
        title="Delete Sprint"
        message={`Are you sure you want to delete "${deleteTarget?.name}"? All work items will be unlinked from this sprint.`}
        confirmLabel="Delete Sprint"
        destructive
        isPending={isDeleting}
        onConfirm={handleDeleteConfirm}
        onCancel={() => setDeleteTarget(null)}
      />
    </div>
  );
}
