"use client";

import { useState } from "react";
import { useRouter, useParams } from "next/navigation";
import { toast } from "sonner";
import { ArrowLeft, Trash2 } from "lucide-react";
import Link from "next/link";
import {
  useWorkItem,
  useUpdateWorkItem,
} from "@/lib/hooks/use-work-items";
import { WorkItemHeader } from "@/components/work-items/work-item-header";
import { WorkItemForm } from "@/components/work-items/work-item-form";
import { WorkItemSidebar } from "@/components/work-items/work-item-sidebar";
import { LinksTab } from "@/components/work-items/links-tab";
import { ChildrenTab } from "@/components/work-items/children-tab";
import { CommentList } from "@/components/comments/comment-list";
import { PokerSession } from "@/components/poker/poker-session";
import { DeleteWorkItemDialog } from "@/components/work-items/delete-work-item-dialog";
import { LoadingSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import type { Priority, UpdateWorkItemBody } from "@/lib/api/types";

type TabId = "links" | "children" | "comments" | "poker";

export default function WorkItemDetailPage() {
  const params = useParams<{ projectId: string; workItemId: string }>();
  const projectId = params?.projectId ?? "";
  const workItemId = params?.workItemId ?? "";
  const router = useRouter();

  const [activeTab, setActiveTab] = useState<TabId>("comments");
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);

  const { data: workItem, isLoading, isError } = useWorkItem(workItemId);
  const updateMutation = useUpdateWorkItem(projectId);

  const isSaving = updateMutation.isPending;

  async function handleTitleSave(newTitle: string) {
    if (!workItem) return;
    try {
      await updateMutation.mutateAsync({
        id: workItemId,
        data: {
          title: newTitle,
          description: workItem.description ?? undefined,
          priority: workItem.priority ?? undefined,
          estimationValue: workItem.estimationValue ?? undefined,
        },
      });
      toast.success("Title updated");
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to update title";
      toast.error(message);
    }
  }

  async function handleFormSave(data: {
    description?: string;
    acceptanceCriteria?: string;
    estimationValue?: number;
    priority?: Priority;
  }) {
    if (!workItem) return;
    const body: UpdateWorkItemBody = {
      title: workItem.title,
      description: data.description,
      priority: data.priority,
      estimationValue: data.estimationValue,
      acceptanceCriteria: data.acceptanceCriteria,
    };
    try {
      await updateMutation.mutateAsync({ id: workItemId, data: body });
      toast.success("Work item saved");
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to save";
      toast.error(message);
    }
  }

  function handleDeleted() {
    router.push(`/projects/${projectId}/backlog`);
  }

  // ---- Loading / error states ----

  if (isLoading) {
    return (
      <div style={{ padding: "24px 32px", maxWidth: 1100 }}>
        <LoadingSkeleton rows={6} type="list-row" />
      </div>
    );
  }

  if (isError || !workItem) {
    return (
      <div
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          padding: 64,
        }}
      >
        <EmptyState
          title="Work item not found"
          description="This item may have been deleted or does not exist."
          action={
            <Link
              href={`/projects/${projectId}/backlog`}
              style={{
                fontSize: 13,
                color: "var(--tf-accent)",
                textDecoration: "none",
              }}
            >
              Back to Backlog
            </Link>
          }
        />
      </div>
    );
  }

  // ---- Render ----

  return (
    <div
      style={{
        padding: "20px 32px",
        maxWidth: 1100,
        margin: "0 auto",
      }}
    >
      {/* Top nav bar */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          marginBottom: 20,
        }}
      >
        <Link
          href={`/projects/${projectId}/backlog`}
          style={{
            display: "flex",
            alignItems: "center",
            gap: 5,
            fontSize: 13,
            color: "var(--tf-text2)",
            textDecoration: "none",
            fontFamily: "var(--tf-font-body)",
          }}
          onMouseEnter={(e) =>
            ((e.currentTarget as HTMLElement).style.color = "var(--tf-text)")
          }
          onMouseLeave={(e) =>
            ((e.currentTarget as HTMLElement).style.color = "var(--tf-text2)")
          }
        >
          <ArrowLeft size={13} />
          Back to Backlog
        </Link>

        <button
          onClick={() => setShowDeleteDialog(true)}
          title="Delete work item"
          style={{
            display: "flex",
            alignItems: "center",
            gap: 5,
            padding: "5px 10px",
            background: "none",
            border: "1px solid var(--tf-border)",
            borderRadius: 6,
            cursor: "pointer",
            color: "var(--tf-text3)",
            fontSize: 13,
            fontFamily: "var(--tf-font-body)",
            transition: "all var(--tf-tr)",
          }}
          onMouseEnter={(e) => {
            (e.currentTarget as HTMLButtonElement).style.color = "var(--tf-red)";
            (e.currentTarget as HTMLButtonElement).style.borderColor = "var(--tf-red)";
            (e.currentTarget as HTMLButtonElement).style.background =
              "var(--tf-red-dim)";
          }}
          onMouseLeave={(e) => {
            (e.currentTarget as HTMLButtonElement).style.color = "var(--tf-text3)";
            (e.currentTarget as HTMLButtonElement).style.borderColor =
              "var(--tf-border)";
            (e.currentTarget as HTMLButtonElement).style.background = "none";
          }}
        >
          <Trash2 size={13} />
          Delete
        </button>
      </div>

      {/* Two-column layout */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "1fr 320px",
          gap: 32,
          alignItems: "start",
        }}
      >
        {/* ---- Left: main content ---- */}
        <div>
          <WorkItemHeader
            workItem={workItem}
            onTitleSave={handleTitleSave}
            isSaving={isSaving}
          />

          <div
            style={{
              background: "var(--tf-bg2)",
              border: "1px solid var(--tf-border)",
              borderRadius: 8,
              padding: "20px 24px",
              marginBottom: 20,
            }}
          >
            <WorkItemForm
              workItem={workItem}
              onSave={handleFormSave}
              isSaving={isSaving}
            />
          </div>

          {/* Tabs */}
          <div>
            {/* Tab bar */}
            <div
              style={{
                display: "flex",
                alignItems: "center",
                gap: 2,
                borderBottom: "1px solid var(--tf-border)",
                marginBottom: 16,
              }}
            >
              {(["comments", "links", "children", ...(workItem.type === "UserStory" ? ["poker" as TabId] : [])] as TabId[]).map((tab) => {
                const isActive = activeTab === tab;
                const label =
                  tab === "comments"
                    ? "Comments"
                    : tab === "links"
                      ? `Links${workItem.linkCount > 0 ? ` (${workItem.linkCount})` : ""}`
                      : tab === "poker"
                        ? "Poker"
                        : `Children${workItem.childCount > 0 ? ` (${workItem.childCount})` : ""}`;
                return (
                  <button
                    key={tab}
                    onClick={() => setActiveTab(tab)}
                    style={{
                      padding: "8px 14px",
                      background: "none",
                      border: "none",
                      borderBottom: isActive
                        ? "2px solid var(--tf-accent)"
                        : "2px solid transparent",
                      cursor: "pointer",
                      fontFamily: "var(--tf-font-body)",
                      fontSize: 13,
                      fontWeight: isActive ? 600 : 400,
                      color: isActive ? "var(--tf-accent)" : "var(--tf-text2)",
                      marginBottom: -1,
                      transition: "color var(--tf-tr)",
                    }}
                  >
                    {label}
                  </button>
                );
              })}
            </div>

            {/* Tab content */}
            {activeTab === "comments" && (
              <CommentList workItemId={workItemId} projectId={projectId} />
            )}
            {activeTab === "links" && (
              <LinksTab workItemId={workItemId} projectId={projectId} />
            )}
            {activeTab === "children" && (
              <ChildrenTab workItemId={workItemId} projectId={projectId} />
            )}
            {activeTab === "poker" && workItem.type === "UserStory" && (
              <PokerSession workItemId={workItemId} projectId={projectId} />
            )}
          </div>
        </div>

        {/* ---- Right: sidebar ---- */}
        <div
          style={{
            background: "var(--tf-bg2)",
            border: "1px solid var(--tf-border)",
            borderRadius: 8,
            padding: "20px",
            position: "sticky",
            top: 20,
          }}
        >
          <WorkItemSidebar workItem={workItem} projectId={projectId} />
        </div>
      </div>

      {/* Delete dialog */}
      <DeleteWorkItemDialog
        open={showDeleteDialog}
        onOpenChange={setShowDeleteDialog}
        workItem={workItem}
        projectId={projectId}
        onDeleted={handleDeleted}
      />
    </div>
  );
}
