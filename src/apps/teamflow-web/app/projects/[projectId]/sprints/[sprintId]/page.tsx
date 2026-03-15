"use client";

import { useState } from "react";
import { use } from "react";
import { useRouter } from "next/navigation";
import { ArrowLeft, Play, CheckCircle, Lock, Zap, Calendar, Settings } from "lucide-react";
import { toast } from "sonner";
import {
  useSprint,
  useStartSprint,
  useCompleteSprint,
  useDeleteSprint,
} from "@/lib/hooks/use-sprints";
import { useHasPermission } from "@/lib/hooks/use-permission";
import { SprintStatusBadge } from "@/components/sprints/sprint-status-badge";
import { SprintPlanningBoard } from "@/components/sprints/sprint-planning-board";
import { BurndownChart } from "@/components/sprints/burndown-chart";
import { SprintFormDialog } from "@/components/sprints/sprint-form-dialog";
import { CapacityForm } from "@/components/sprints/capacity-form";
import { ConfirmDialog } from "@/components/projects/confirm-dialog";
import { Skeleton } from "@/components/ui/skeleton";
import type { ApiError } from "@/lib/api/client";

interface SprintDetailPageProps {
  params: Promise<{ projectId: string; sprintId: string }>;
}

function formatSprintDate(dateStr: string | null): string {
  if (!dateStr) return "No date set";
  const d = new Date(dateStr);
  return d.toLocaleDateString("en-US", { month: "long", day: "numeric", year: "numeric" });
}

export default function SprintDetailPage({ params }: SprintDetailPageProps) {
  const { projectId, sprintId } = use(params);
  const router = useRouter();

  const [editOpen, setEditOpen] = useState(false);
  const [capacityOpen, setCapacityOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [startConfirmOpen, setStartConfirmOpen] = useState(false);
  const [completeConfirmOpen, setCompleteConfirmOpen] = useState(false);

  const { data: sprint, isLoading, isError } = useSprint(sprintId);
  const { mutate: startSprintMut, isPending: isStarting } = useStartSprint(projectId);
  const { mutate: completeSprintMut, isPending: isCompleting } = useCompleteSprint(projectId);
  const { mutate: deleteSprintMut, isPending: isDeleting } = useDeleteSprint(projectId);

  const canStart = useHasPermission(projectId, "Sprint_Start");
  const canClose = useHasPermission(projectId, "Sprint_Complete");
  const canEdit = useHasPermission(projectId, "Sprint_Edit");

  function handleStartConfirm() {
    startSprintMut(sprintId, {
      onSuccess: () => {
        toast.success("Sprint started.");
        setStartConfirmOpen(false);
      },
      onError: (err) => {
        const apiErr = err as ApiError;
        toast.error(apiErr.message ?? "Failed to start sprint.");
      },
    });
  }

  function handleCompleteConfirm() {
    completeSprintMut(sprintId, {
      onSuccess: () => {
        toast.success("Sprint completed. Incomplete items have been unlinked.");
        setCompleteConfirmOpen(false);
      },
      onError: (err) => {
        const apiErr = err as ApiError;
        toast.error(apiErr.message ?? "Failed to complete sprint.");
      },
    });
  }

  function handleDeleteConfirm() {
    if (!sprint) return;
    deleteSprintMut(sprintId, {
      onSuccess: () => {
        toast.success(`Sprint "${sprint.name}" deleted.`);
        router.push(`/projects/${projectId}/sprints`);
      },
      onError: (err) => {
        const apiErr = err as ApiError;
        toast.error(apiErr.message ?? "Failed to delete sprint.");
      },
    });
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
        <div style={{ display: "flex", gap: 12 }}>
          <Skeleton style={{ flex: 1, height: 300, borderRadius: 8 }} />
          <Skeleton style={{ flex: 1, height: 300, borderRadius: 8 }} />
        </div>
      </div>
    );
  }

  if (isError || !sprint) {
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
          Failed to load sprint. It may have been deleted.
        </div>
      </div>
    );
  }

  const canStartSprint =
    sprint.status === "Planning" &&
    sprint.itemCount > 0 &&
    sprint.startDate !== null &&
    sprint.endDate !== null;

  const canCompleteSprint = sprint.status === "Active";

  return (
    <div style={{ padding: "20px", display: "flex", flexDirection: "column", gap: 16 }}>
      {/* Back navigation */}
      <button
        onClick={() => router.push(`/projects/${projectId}/sprints`)}
        style={{
          display: "inline-flex",
          alignItems: "center",
          gap: 5,
          background: "transparent",
          border: "none",
          cursor: "pointer",
          color: "var(--tf-text3)",
          fontSize: 12,
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
        Back to Sprints
      </button>

      {/* Sprint header card */}
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
              background: "var(--tf-blue-dim)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              flexShrink: 0,
            }}
          >
            <Zap size={15} color="var(--tf-blue)" />
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
                {sprint.name}
              </h1>
              <SprintStatusBadge status={sprint.status} />
              {sprint.status === "Active" && (
                <span
                  style={{
                    display: "inline-flex",
                    alignItems: "center",
                    gap: 4,
                    padding: "2px 8px",
                    borderRadius: 100,
                    fontSize: 10,
                    fontWeight: 600,
                    fontFamily: "var(--tf-font-mono)",
                    background: "var(--tf-yellow-dim)",
                    color: "var(--tf-yellow)",
                    border: "1px solid var(--tf-yellow)",
                  }}
                  title="Sprint is scope-locked"
                >
                  <Lock size={9} />
                  Locked
                </span>
              )}
            </div>
            {sprint.goal && (
              <p
                style={{
                  fontSize: 13,
                  color: "var(--tf-text3)",
                  marginTop: 6,
                  lineHeight: 1.5,
                }}
              >
                {sprint.goal}
              </p>
            )}
          </div>

          {/* Action buttons */}
          <div style={{ display: "flex", gap: 6, flexShrink: 0, flexWrap: "wrap" }}>
            {/* Start button */}
            {canStart && sprint.status === "Planning" && (
              <button
                onClick={() => setStartConfirmOpen(true)}
                disabled={!canStartSprint}
                title={
                  !canStartSprint
                    ? sprint.itemCount === 0
                      ? "Add at least one item to start"
                      : "Set start and end dates to start"
                    : "Start this sprint"
                }
                aria-label="Start sprint"
                style={{
                  display: "inline-flex",
                  alignItems: "center",
                  gap: 5,
                  padding: "6px 12px",
                  borderRadius: 6,
                  border: "1px solid var(--tf-accent)",
                  background: canStartSprint ? "var(--tf-accent)" : "transparent",
                  color: canStartSprint ? "var(--primary-foreground)" : "var(--tf-text3)",
                  fontSize: 12,
                  fontWeight: 600,
                  cursor: canStartSprint ? "pointer" : "not-allowed",
                  fontFamily: "var(--tf-font-body)",
                  opacity: canStartSprint ? 1 : 0.5,
                  transition: "all var(--tf-tr)",
                }}
              >
                <Play size={12} />
                Start Sprint
              </button>
            )}

            {/* Complete button */}
            {canClose && canCompleteSprint && (
              <button
                onClick={() => setCompleteConfirmOpen(true)}
                aria-label="Complete sprint"
                style={{
                  display: "inline-flex",
                  alignItems: "center",
                  gap: 5,
                  padding: "6px 12px",
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
                <CheckCircle size={12} />
                Complete Sprint
              </button>
            )}

            {/* Edit button */}
            {canEdit && sprint.status === "Planning" && (
              <button
                onClick={() => setEditOpen(true)}
                aria-label="Edit sprint"
                style={{
                  padding: "6px 12px",
                  borderRadius: 6,
                  border: "1px solid var(--tf-border)",
                  background: "transparent",
                  color: "var(--tf-text2)",
                  fontSize: 12,
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
            )}

            {/* Capacity button */}
            {canEdit && sprint.status === "Planning" && (
              <button
                onClick={() => setCapacityOpen(true)}
                aria-label="Edit capacity"
                style={{
                  display: "inline-flex",
                  alignItems: "center",
                  gap: 5,
                  padding: "6px 12px",
                  borderRadius: 6,
                  border: "1px solid var(--tf-border)",
                  background: "transparent",
                  color: "var(--tf-text2)",
                  fontSize: 12,
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
                <Settings size={11} />
                Capacity
              </button>
            )}

            {/* Delete button */}
            {canEdit && sprint.status === "Planning" && (
              <button
                onClick={() => setDeleteOpen(true)}
                aria-label="Delete sprint"
                style={{
                  padding: "6px 12px",
                  borderRadius: 6,
                  border: "1px solid var(--tf-border)",
                  background: "transparent",
                  color: "var(--tf-red)",
                  fontSize: 12,
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
            )}
          </div>
        </div>

        {/* Meta row */}
        <div style={{ display: "flex", alignItems: "center", gap: 16, flexWrap: "wrap" }}>
          <span
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 5,
              fontSize: 12,
              color: "var(--tf-text3)",
              fontFamily: "var(--tf-font-mono)",
            }}
          >
            <Calendar size={12} />
            {formatSprintDate(sprint.startDate)}
            {sprint.endDate && ` - ${formatSprintDate(sprint.endDate)}`}
          </span>
          <span
            style={{
              fontSize: 12,
              color: "var(--tf-text3)",
              fontFamily: "var(--tf-font-mono)",
            }}
          >
            {sprint.itemCount} item{sprint.itemCount !== 1 ? "s" : ""} &bull;{" "}
            {sprint.completedPoints}/{sprint.totalPoints} pts completed
          </span>
        </div>

        {/* Progress bar */}
        {sprint.totalPoints > 0 && (
          <div style={{ display: "flex", flexDirection: "column", gap: 5 }}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
              <span
                style={{
                  fontSize: 11,
                  color: "var(--tf-text3)",
                  fontFamily: "var(--tf-font-mono)",
                }}
              >
                Progress
              </span>
              <span
                style={{
                  fontSize: 11,
                  color: "var(--tf-text2)",
                  fontFamily: "var(--tf-font-mono)",
                }}
              >
                {Math.round((sprint.completedPoints / sprint.totalPoints) * 100)}%
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
                  width: `${Math.round((sprint.completedPoints / sprint.totalPoints) * 100)}%`,
                  borderRadius: 100,
                  background:
                    sprint.status === "Completed" ? "var(--tf-text3)" : "var(--tf-accent)",
                  transition: "width 0.3s ease",
                }}
              />
            </div>
          </div>
        )}
      </div>

      {/* Burndown chart (Active or Completed sprints) */}
      {(sprint.status === "Active" || sprint.status === "Completed") && (
        <div
          style={{
            background: "var(--tf-bg2)",
            border: "1px solid var(--tf-border)",
            borderRadius: "var(--tf-radius)",
            padding: 14,
          }}
        >
          <BurndownChart sprintId={sprintId} />
        </div>
      )}

      {/* Planning board (Planning or Active) */}
      {(sprint.status === "Planning" || sprint.status === "Active") && (
        <SprintPlanningBoard sprintId={sprintId} projectId={projectId} />
      )}

      {/* Completed sprint items list */}
      {sprint.status === "Completed" && sprint.items.length > 0 && (
        <div
          style={{
            background: "var(--tf-bg2)",
            border: "1px solid var(--tf-border)",
            borderRadius: "var(--tf-radius)",
            overflow: "hidden",
          }}
        >
          <div
            style={{
              padding: "10px 14px",
              borderBottom: "1px solid var(--tf-border)",
              display: "flex",
              alignItems: "center",
              gap: 8,
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
              Sprint Items
            </span>
            <span
              style={{
                padding: "1px 7px",
                borderRadius: 100,
                background: "var(--tf-bg4)",
                color: "var(--tf-text3)",
                fontSize: 11,
                fontFamily: "var(--tf-font-mono)",
              }}
            >
              {sprint.items.length}
            </span>
          </div>
          {sprint.items.map((item, index) => {
            const isLast = index === sprint.items.length - 1;
            return (
              <div
                key={item.id}
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: 10,
                  padding: "10px 16px",
                  borderBottom: isLast ? "none" : "1px solid var(--tf-border)",
                }}
              >
                <span
                  style={{
                    flex: 1,
                    fontSize: 13,
                    color: "var(--tf-text)",
                    fontWeight: 500,
                    overflow: "hidden",
                    textOverflow: "ellipsis",
                    whiteSpace: "nowrap",
                    minWidth: 0,
                  }}
                >
                  {item.title}
                </span>
                <span
                  style={{
                    fontSize: 10,
                    fontFamily: "var(--tf-font-mono)",
                    color:
                      item.status === "Done"
                        ? "var(--tf-accent)"
                        : "var(--tf-text3)",
                    fontWeight: 500,
                  }}
                >
                  {item.status}
                </span>
              </div>
            );
          })}
        </div>
      )}

      {/* Dialogs */}
      <SprintFormDialog
        open={editOpen}
        sprint={sprint}
        projectId={projectId}
        onClose={() => setEditOpen(false)}
      />

      <CapacityForm
        open={capacityOpen}
        sprintId={sprintId}
        projectId={projectId}
        currentCapacity={sprint.capacity}
        onClose={() => setCapacityOpen(false)}
      />

      <ConfirmDialog
        open={startConfirmOpen}
        title="Start Sprint"
        message={`Start "${sprint.name}"? Once started, the sprint scope will be locked. Only Team Managers can add items to an active sprint.`}
        confirmLabel="Start Sprint"
        isPending={isStarting}
        onConfirm={handleStartConfirm}
        onCancel={() => setStartConfirmOpen(false)}
      />

      <ConfirmDialog
        open={completeConfirmOpen}
        title="Complete Sprint"
        message={`Complete "${sprint.name}"? Incomplete items will be unlinked from this sprint and returned to the backlog.`}
        confirmLabel="Complete Sprint"
        isPending={isCompleting}
        onConfirm={handleCompleteConfirm}
        onCancel={() => setCompleteConfirmOpen(false)}
      />

      <ConfirmDialog
        open={deleteOpen}
        title="Delete Sprint"
        message={`Are you sure you want to delete "${sprint.name}"? All work items will be unlinked from this sprint.`}
        confirmLabel="Delete Sprint"
        destructive
        isPending={isDeleting}
        onConfirm={handleDeleteConfirm}
        onCancel={() => setDeleteOpen(false)}
      />
    </div>
  );
}
