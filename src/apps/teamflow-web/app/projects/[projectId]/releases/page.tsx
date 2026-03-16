"use client";

import { useState } from "react";
import { Plus, Tag } from "lucide-react";
import { toast } from "sonner";
import { useProjectContext } from "@/lib/contexts/project-context";
import { useReleases, useDeleteRelease } from "@/lib/hooks/use-releases";
import { ReleaseCard } from "@/components/releases/release-card";
import { CreateReleaseDialog } from "@/components/releases/create-release-dialog";
import { EditReleaseDialog } from "@/components/releases/edit-release-dialog";
import { ConfirmDialog } from "@/components/projects/confirm-dialog";
import { EmptyState } from "@/components/shared/empty-state";
import { Skeleton } from "@/components/ui/skeleton";
import type { ReleaseDto } from "@/lib/api/types";
import type { ApiError } from "@/lib/api/client";

export default function ReleasesPage() {
  const { project } = useProjectContext();
  const [createOpen, setCreateOpen] = useState(false);
  const [editTarget, setEditTarget] = useState<ReleaseDto | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<ReleaseDto | null>(null);

  const { data, isLoading, isError } = useReleases({ projectId: project.id });
  const { mutate: deleteRelease, isPending: isDeleting } = useDeleteRelease(project.id);

  function handleDeleteConfirm() {
    if (!deleteTarget) return;
    deleteRelease(deleteTarget.id, {
      onSuccess: () => {
        toast.success(`Release "${deleteTarget.name}" deleted.`);
        setDeleteTarget(null);
      },
      onError: (err) => {
        const apiErr = err as ApiError;
        toast.error(apiErr.message ?? "Failed to delete release.");
      },
    });
  }

  const releases = data?.items ?? [];

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
            Releases
          </h1>
          {!isLoading && !isError && (
            <p style={{ fontSize: 13, color: "var(--tf-text3)", marginTop: 3 }}>
              {releases.length} {releases.length === 1 ? "release" : "releases"} in {project.name}
            </p>
          )}
        </div>

        <button
          onClick={() => setCreateOpen(true)}
          style={{
            display: "inline-flex",
            alignItems: "center",
            gap: 6,
            padding: "7px 14px",
            borderRadius: 6,
            border: "1px solid var(--tf-accent)",
            background: "var(--tf-accent)",
            color: "var(--primary-foreground)",
            fontSize: 13,
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
          New Release
        </button>
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
          Failed to load releases. Please try again.
        </div>
      ) : releases.length === 0 ? (
        <div
          style={{
            background: "var(--tf-bg2)",
            border: "1px solid var(--tf-border)",
            borderRadius: "var(--tf-radius)",
          }}
        >
          <EmptyState
            icon={Tag}
            title="No releases yet"
            description="Create a release to track work items and shipping progress."
            action={
              <button
                onClick={() => setCreateOpen(true)}
                style={{
                  padding: "6px 14px",
                  background: "var(--tf-accent)",
                  color: "var(--tf-bg)",
                  border: "none",
                  borderRadius: 6,
                  fontSize: 13,
                  fontWeight: 600,
                  cursor: "pointer",
                  fontFamily: "var(--tf-font-body)",
                }}
              >
                New Release
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
          {releases.map((release) => (
            <ReleaseCard
              key={release.id}
              release={release}
              projectId={project.id}
              onEdit={setEditTarget}
              onDelete={setDeleteTarget}
            />
          ))}
        </div>
      )}

      {/* Dialogs */}
      <CreateReleaseDialog
        open={createOpen}
        projectId={project.id}
        onClose={() => setCreateOpen(false)}
      />

      <EditReleaseDialog
        open={editTarget !== null}
        release={editTarget}
        projectId={project.id}
        onClose={() => setEditTarget(null)}
      />

      <ConfirmDialog
        open={deleteTarget !== null}
        title="Delete Release"
        message={`Are you sure you want to delete "${deleteTarget?.name}"? All assigned work items will be unlinked from this release.`}
        confirmLabel="Delete Release"
        destructive
        isPending={isDeleting}
        onConfirm={handleDeleteConfirm}
        onCancel={() => setDeleteTarget(null)}
      />
    </div>
  );
}
