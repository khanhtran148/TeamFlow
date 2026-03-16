"use client";

import { useState, useMemo } from "react";
import { use } from "react";
import { useRouter } from "next/navigation";
import { ArrowLeft, Plus, Trash2, Calendar, Package } from "lucide-react";
import { toast } from "sonner";
import { useRelease, useDeleteRelease, useUnassignItem } from "@/lib/hooks/use-releases";
import { useBacklog } from "@/lib/hooks/use-backlog";
import { EditReleaseDialog } from "@/components/releases/edit-release-dialog";
import { AssignItemDialog } from "@/components/releases/assign-item-dialog";
import { ConfirmDialog } from "@/components/projects/confirm-dialog";
import { TypeIcon } from "@/components/shared/type-icon";
import { StatusBadge } from "@/components/shared/status-badge";
import { PriorityIcon } from "@/components/shared/priority-icon";
import { Skeleton } from "@/components/ui/skeleton";
import type { ReleaseStatus, WorkItemStatus } from "@/lib/api/types";
import type { ApiError } from "@/lib/api/client";

interface ReleaseDetailPageProps {
  params: Promise<{ projectId: string; releaseId: string }>;
}

const STATUS_CONFIG: Record<
  ReleaseStatus,
  { label: string; bg: string; color: string; border: string }
> = {
  Unreleased: {
    label: "Unreleased",
    bg: "var(--tf-accent-dim2)",
    color: "var(--tf-accent)",
    border: "var(--tf-accent)",
  },
  Overdue: {
    label: "Overdue",
    bg: "var(--tf-red-dim)",
    color: "var(--tf-red)",
    border: "var(--tf-red-dim)",
  },
  Released: {
    label: "Released",
    bg: "var(--tf-bg4)",
    color: "var(--tf-text3)",
    border: "var(--tf-border)",
  },
};

function formatReleaseDate(dateStr: string | null): string {
  if (!dateStr) return "No date set";
  const d = new Date(dateStr);
  return d.toLocaleDateString("en-US", { month: "long", day: "numeric", year: "numeric" });
}

export default function ReleaseDetailPage({ params }: ReleaseDetailPageProps) {
  const { projectId, releaseId } = use(params);
  const router = useRouter();

  const [editOpen, setEditOpen] = useState(false);
  const [assignOpen, setAssignOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [removingItemId, setRemovingItemId] = useState<string | null>(null);

  const { data: release, isLoading, isError } = useRelease(releaseId);
  const { mutate: deleteRelease, isPending: isDeleting } = useDeleteRelease(projectId);
  const { mutate: unassignItem } = useUnassignItem(projectId);

  // Fetch backlog items to find assigned items and populate the assign dialog
  const { data: backlogData } = useBacklog({ projectId, releaseId, pageSize: 200 });

  const assignedItems = useMemo(
    () => backlogData?.items ?? [],
    [backlogData],
  );

  const assignedItemIds = useMemo(
    () => new Set(assignedItems.map((item) => item.id)),
    [assignedItems],
  );

  function handleDeleteConfirm() {
    if (!release) return;
    deleteRelease(release.id, {
      onSuccess: () => {
        toast.success(`Release "${release.name}" deleted.`);
        router.push(`/projects/${projectId}/releases`);
      },
      onError: (err) => {
        const apiErr = err as ApiError;
        toast.error(apiErr.message ?? "Failed to delete release.");
      },
    });
  }

  function handleRemoveItem(workItemId: string, title: string) {
    setRemovingItemId(workItemId);
    unassignItem(
      { releaseId, workItemId },
      {
        onSuccess: () => {
          toast.success(`"${title}" removed from release.`);
          setRemovingItemId(null);
        },
        onError: (err) => {
          const apiErr = err as ApiError;
          toast.error(apiErr.message ?? "Failed to remove item.");
          setRemovingItemId(null);
        },
      },
    );
  }

  if (isLoading) {
    return (
      <div style={{ padding: "20px", display: "flex", flexDirection: "column", gap: 16 }}>
        <Skeleton style={{ height: 20, width: 120, borderRadius: 4 }} />
        <div
          style={{
            background: "var(--tf-bg2)",
            border: "1px solid var(--tf-border)",
            borderRadius: "var(--tf-radius)",
            padding: 20,
            display: "flex",
            flexDirection: "column",
            gap: 12,
          }}
        >
          <Skeleton style={{ height: 22, width: "40%", borderRadius: 4 }} />
          <Skeleton style={{ height: 4, borderRadius: 100 }} />
          <Skeleton style={{ height: 12, width: "25%", borderRadius: 4 }} />
        </div>
        <div
          style={{
            background: "var(--tf-bg2)",
            border: "1px solid var(--tf-border)",
            borderRadius: "var(--tf-radius)",
            overflow: "hidden",
          }}
        >
          {Array.from({ length: 5 }).map((_, i) => (
            <div
              key={i}
              style={{
                display: "flex",
                gap: 10,
                padding: "12px 16px",
                borderBottom: "1px solid var(--tf-border)",
                alignItems: "center",
              }}
            >
              <Skeleton style={{ width: 14, height: 14, borderRadius: 2, flexShrink: 0 }} />
              <Skeleton style={{ height: 13, flex: 1, borderRadius: 4 }} />
              <Skeleton style={{ height: 20, width: 80, borderRadius: 100 }} />
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (isError || !release) {
    return (
      <div style={{ padding: "20px" }}>
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
          Failed to load release. It may have been deleted.
        </div>
      </div>
    );
  }

  const statusConfig = STATUS_CONFIG[release.status];
  const done = (release.itemCountsByStatus as Record<WorkItemStatus, number>)["Done"] ?? 0;
  const total = release.totalItems;
  const progressPct = total > 0 ? Math.round((done / total) * 100) : 0;

  return (
    <div style={{ padding: "20px", display: "flex", flexDirection: "column", gap: 16 }}>
      {/* Back navigation */}
      <button
        onClick={() => router.push(`/projects/${projectId}/releases`)}
        style={{
          display: "inline-flex",
          alignItems: "center",
          gap: 5,
          background: "transparent",
          border: "none",
          cursor: "pointer",
          color: "var(--tf-text3)",
          fontSize: 13,
          fontFamily: "var(--tf-font-body)",
          padding: 0,
          transition: "color var(--tf-tr)",
        }}
        onMouseEnter={(e) => {
          (e.currentTarget as HTMLButtonElement).style.color = "var(--tf-text)";
        }}
        onMouseLeave={(e) => {
          (e.currentTarget as HTMLButtonElement).style.color = "var(--tf-text3)";
        }}
      >
        <ArrowLeft size={13} />
        Back to Releases
      </button>

      {/* Release header card */}
      <div
        style={{
          background: "var(--tf-bg2)",
          border: "1px solid var(--tf-border)",
          borderRadius: "var(--tf-radius)",
          padding: 20,
          display: "flex",
          flexDirection: "column",
          gap: 14,
        }}
      >
        {/* Title row */}
        <div style={{ display: "flex", alignItems: "flex-start", gap: 10 }}>
          <div
            style={{
              width: 32,
              height: 32,
              borderRadius: 8,
              background: "var(--tf-violet-dim)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              flexShrink: 0,
            }}
          >
            <Package size={15} color="var(--tf-violet)" />
          </div>

          <div style={{ flex: 1, minWidth: 0 }}>
            <div style={{ display: "flex", alignItems: "center", gap: 10, flexWrap: "wrap" }}>
              <h1
                style={{
                  fontFamily: "var(--tf-font-head)",
                  fontWeight: 700,
                  fontSize: 20,
                  color: "var(--tf-text)",
                  margin: 0,
                  lineHeight: 1.2,
                }}
              >
                {release.name}
              </h1>
              <span
                style={{
                  display: "inline-flex",
                  alignItems: "center",
                  padding: "2px 9px",
                  borderRadius: 100,
                  fontSize: 13,
                  fontWeight: 600,
                  fontFamily: "var(--tf-font-mono)",
                  background: statusConfig.bg,
                  color: statusConfig.color,
                  border: `1px solid ${statusConfig.border}`,
                  whiteSpace: "nowrap",
                }}
              >
                {statusConfig.label}
              </span>
            </div>
            {release.description && (
              <p
                style={{
                  fontSize: 13,
                  color: "var(--tf-text3)",
                  marginTop: 6,
                  lineHeight: 1.5,
                }}
              >
                {release.description}
              </p>
            )}
          </div>

          {/* Action buttons */}
          <div style={{ display: "flex", gap: 6, flexShrink: 0 }}>
            <button
              onClick={() => setEditOpen(true)}
              style={{
                padding: "6px 12px",
                borderRadius: 6,
                border: "1px solid var(--tf-border)",
                background: "transparent",
                color: "var(--tf-text2)",
                fontSize: 13,
                fontWeight: 500,
                cursor: "pointer",
                fontFamily: "var(--tf-font-body)",
                transition: "all var(--tf-tr)",
              }}
              onMouseEnter={(e) => {
                (e.currentTarget as HTMLButtonElement).style.background = "var(--tf-bg3)";
                (e.currentTarget as HTMLButtonElement).style.color = "var(--tf-text)";
              }}
              onMouseLeave={(e) => {
                (e.currentTarget as HTMLButtonElement).style.background = "transparent";
                (e.currentTarget as HTMLButtonElement).style.color = "var(--tf-text2)";
              }}
            >
              Edit
            </button>
            <button
              onClick={() => setDeleteOpen(true)}
              style={{
                padding: "6px 12px",
                borderRadius: 6,
                border: "1px solid var(--tf-border)",
                background: "transparent",
                color: "var(--tf-red)",
                fontSize: 13,
                fontWeight: 500,
                cursor: "pointer",
                fontFamily: "var(--tf-font-body)",
                transition: "all var(--tf-tr)",
              }}
              onMouseEnter={(e) => {
                (e.currentTarget as HTMLButtonElement).style.background = "var(--tf-red-dim)";
                (e.currentTarget as HTMLButtonElement).style.borderColor = "var(--tf-red)";
              }}
              onMouseLeave={(e) => {
                (e.currentTarget as HTMLButtonElement).style.background = "transparent";
                (e.currentTarget as HTMLButtonElement).style.borderColor = "var(--tf-border)";
              }}
            >
              Delete
            </button>
          </div>
        </div>

        {/* Meta row */}
        <div style={{ display: "flex", alignItems: "center", gap: 16, flexWrap: "wrap" }}>
          <span
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 5,
              fontSize: 13,
              color: "var(--tf-text3)",
              fontFamily: "var(--tf-font-mono)",
            }}
          >
            <Calendar size={12} />
            {formatReleaseDate(release.releaseDate)}
          </span>
          <span
            style={{
              fontSize: 13,
              color: "var(--tf-text3)",
              fontFamily: "var(--tf-font-mono)",
            }}
          >
            {total} item{total !== 1 ? "s" : ""} &bull; {done} done
          </span>
        </div>

        {/* Progress bar */}
        {total > 0 && (
          <div style={{ display: "flex", flexDirection: "column", gap: 5 }}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
              <span
                style={{
                  fontSize: 13,
                  color: "var(--tf-text3)",
                  fontFamily: "var(--tf-font-mono)",
                }}
              >
                Progress
              </span>
              <span
                style={{
                  fontSize: 13,
                  color: "var(--tf-text2)",
                  fontFamily: "var(--tf-font-mono)",
                }}
              >
                {progressPct}%
              </span>
            </div>
            <div
              style={{
                height: 6,
                borderRadius: 100,
                background: "var(--tf-bg4)",
                overflow: "hidden",
              }}
            >
              <div
                style={{
                  height: "100%",
                  width: `${progressPct}%`,
                  borderRadius: 100,
                  background:
                    release.status === "Overdue"
                      ? "var(--tf-red)"
                      : release.status === "Released"
                        ? "var(--tf-text3)"
                        : "var(--tf-accent)",
                  transition: "width 0.3s ease",
                }}
              />
            </div>
          </div>
        )}
      </div>

      {/* Work items section */}
      <div
        style={{
          background: "var(--tf-bg2)",
          border: "1px solid var(--tf-border)",
          borderRadius: "var(--tf-radius)",
          overflow: "hidden",
        }}
      >
        {/* Section header */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            padding: "12px 16px",
            borderBottom: "1px solid var(--tf-border)",
          }}
        >
          <span
            style={{
              fontFamily: "var(--tf-font-head)",
              fontWeight: 600,
              fontSize: 13,
              color: "var(--tf-text)",
            }}
          >
            Work Items
            {assignedItems.length > 0 && (
              <span
                style={{
                  marginLeft: 7,
                  padding: "1px 7px",
                  borderRadius: 100,
                  background: "var(--tf-bg4)",
                  color: "var(--tf-text3)",
                  fontSize: 13,
                  fontFamily: "var(--tf-font-mono)",
                }}
              >
                {assignedItems.length}
              </span>
            )}
          </span>

          <button
            onClick={() => setAssignOpen(true)}
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 5,
              padding: "5px 11px",
              borderRadius: 5,
              border: "1px solid var(--tf-accent)",
              background: "transparent",
              color: "var(--tf-accent)",
              fontSize: 13,
              fontWeight: 600,
              cursor: "pointer",
              fontFamily: "var(--tf-font-body)",
              transition: "background var(--tf-tr)",
            }}
            onMouseEnter={(e) => {
              (e.currentTarget as HTMLButtonElement).style.background = "var(--tf-accent-dim2)";
            }}
            onMouseLeave={(e) => {
              (e.currentTarget as HTMLButtonElement).style.background = "transparent";
            }}
          >
            <Plus size={11} />
            Assign Items
          </button>
        </div>

        {/* Items list */}
        {assignedItems.length === 0 ? (
          <div
            style={{
              padding: "40px 20px",
              textAlign: "center",
              color: "var(--tf-text3)",
              fontSize: 13,
            }}
          >
            No work items assigned to this release yet.{" "}
            <button
              onClick={() => setAssignOpen(true)}
              style={{
                background: "transparent",
                border: "none",
                color: "var(--tf-accent)",
                cursor: "pointer",
                fontSize: 13,
                fontFamily: "var(--tf-font-body)",
                textDecoration: "underline",
                padding: 0,
              }}
            >
              Assign items
            </button>
          </div>
        ) : (
          <div>
            {assignedItems.map((item, index) => {
              const isRemoving = removingItemId === item.id;
              const isLast = index === assignedItems.length - 1;

              return (
                <div
                  key={item.id}
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: 10,
                    padding: "10px 16px",
                    borderBottom: isLast ? "none" : "1px solid var(--tf-border)",
                    transition: "background var(--tf-tr)",
                    opacity: isRemoving ? 0.5 : 1,
                  }}
                  onMouseEnter={(e) => {
                    (e.currentTarget as HTMLDivElement).style.background = "var(--tf-bg3)";
                  }}
                  onMouseLeave={(e) => {
                    (e.currentTarget as HTMLDivElement).style.background = "transparent";
                  }}
                >
                  <TypeIcon type={item.type} size={14} />

                  <div style={{ flex: 1, minWidth: 0, display: "flex", alignItems: "center", gap: 8 }}>
                    <span
                      style={{
                        fontSize: 13,
                        color: "var(--tf-text)",
                        fontWeight: 500,
                        overflow: "hidden",
                        textOverflow: "ellipsis",
                        whiteSpace: "nowrap",
                        flex: 1,
                        minWidth: 0,
                      }}
                    >
                      {item.title}
                    </span>
                  </div>

                  <div style={{ display: "flex", alignItems: "center", gap: 8, flexShrink: 0 }}>
                    {item.priority && <PriorityIcon priority={item.priority} />}
                    <StatusBadge status={item.status} size="sm" />
                    <button
                      onClick={() => handleRemoveItem(item.id, item.title)}
                      disabled={isRemoving}
                      title="Remove from release"
                      style={{
                        width: 24,
                        height: 24,
                        borderRadius: 4,
                        border: "1px solid transparent",
                        background: "transparent",
                        color: "var(--tf-text3)",
                        cursor: isRemoving ? "not-allowed" : "pointer",
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "center",
                        transition: "all var(--tf-tr)",
                      }}
                      onMouseEnter={(e) => {
                        if (!isRemoving) {
                          (e.currentTarget as HTMLButtonElement).style.background =
                            "var(--tf-red-dim)";
                          (e.currentTarget as HTMLButtonElement).style.color = "var(--tf-red)";
                          (e.currentTarget as HTMLButtonElement).style.borderColor =
                            "var(--tf-red-dim)";
                        }
                      }}
                      onMouseLeave={(e) => {
                        (e.currentTarget as HTMLButtonElement).style.background = "transparent";
                        (e.currentTarget as HTMLButtonElement).style.color = "var(--tf-text3)";
                        (e.currentTarget as HTMLButtonElement).style.borderColor = "transparent";
                      }}
                    >
                      <Trash2 size={11} />
                    </button>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* Dialogs */}
      <EditReleaseDialog
        open={editOpen}
        release={release}
        projectId={projectId}
        onClose={() => setEditOpen(false)}
      />

      <AssignItemDialog
        open={assignOpen}
        releaseId={releaseId}
        projectId={projectId}
        assignedItemIds={assignedItemIds}
        onClose={() => setAssignOpen(false)}
      />

      <ConfirmDialog
        open={deleteOpen}
        title="Delete Release"
        message={`Are you sure you want to delete "${release.name}"? All assigned work items will be unlinked from this release.`}
        confirmLabel="Delete Release"
        destructive
        isPending={isDeleting}
        onConfirm={handleDeleteConfirm}
        onCancel={() => setDeleteOpen(false)}
      />
    </div>
  );
}
