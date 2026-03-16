"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Plus, GitBranch } from "lucide-react";
import { useBacklog } from "@/lib/hooks/use-backlog";
import { TypeIcon } from "@/components/shared/type-icon";
import { StatusBadge } from "@/components/shared/status-badge";
import { UserAvatar, formatAssignedAt } from "@/components/shared/user-avatar";
import { CreateWorkItemDialog } from "./create-work-item-dialog";
import { LoadingSkeleton } from "@/components/shared/loading-skeleton";

interface ChildrenTabProps {
  workItemId: string;
  projectId: string;
}

export function ChildrenTab({ workItemId, projectId }: ChildrenTabProps) {
  const router = useRouter();
  const [showCreateDialog, setShowCreateDialog] = useState(false);

  // Fetch children — backlog items with this item as parent
  // The backlog API returns all items; we filter by parentId client-side
  // (There's no dedicated /children endpoint, so we use GET /backlog with full list)
  const { data: backlogData, isLoading } = useBacklog(
    { projectId, pageSize: 100 },
    { enabled: !!projectId },
  );

  const children = (backlogData?.items ?? []).filter(
    (item) => item.parentId === workItemId,
  );

  function handleNavigate(childId: string) {
    router.push(`/projects/${projectId}/work-items/${childId}`);
  }

  if (isLoading) {
    return <LoadingSkeleton rows={3} type="list-row" />;
  }

  return (
    <>
      <div style={{ padding: "0 0 16px" }}>
        {/* Header */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            marginBottom: 16,
          }}
        >
          <div
            style={{
              fontSize: 13,
              color: "var(--tf-text2)",
              fontFamily: "var(--tf-font-body)",
            }}
          >
            {children.length} child item{children.length !== 1 ? "s" : ""}
          </div>
          <button
            onClick={() => setShowCreateDialog(true)}
            style={{
              display: "flex",
              alignItems: "center",
              gap: 5,
              padding: "5px 10px",
              background: "var(--tf-bg4)",
              border: "1px solid var(--tf-border)",
              borderRadius: 6,
              cursor: "pointer",
              color: "var(--tf-text2)",
              fontSize: 13,
              fontFamily: "var(--tf-font-body)",
            }}
          >
            <Plus size={13} />
            Add Child
          </button>
        </div>

        {/* Empty state */}
        {children.length === 0 && (
          <div
            style={{
              display: "flex",
              flexDirection: "column",
              alignItems: "center",
              justifyContent: "center",
              padding: "32px 16px",
              color: "var(--tf-text3)",
              textAlign: "center",
            }}
          >
            <GitBranch size={28} style={{ marginBottom: 10, opacity: 0.4 }} />
            <div style={{ fontSize: 13, fontFamily: "var(--tf-font-body)" }}>
              No child items
            </div>
            <div style={{ fontSize: 13, marginTop: 4 }}>
              Break this item down into smaller tasks, stories, or bugs.
            </div>
          </div>
        )}

        {/* Children list */}
        {children.length > 0 && (
          <div
            style={{
              border: "1px solid var(--tf-border)",
              borderRadius: 6,
              overflow: "hidden",
            }}
          >
            {children.map((child, idx) => {
              const initials = child.assigneeName
                ? child.assigneeName
                    .split(" ")
                    .map((n) => n[0])
                    .join("")
                    .slice(0, 2)
                : "";

              return (
                <div
                  key={child.id}
                  onClick={() => handleNavigate(child.id)}
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: 8,
                    padding: "8px 12px",
                    background: "var(--tf-bg3)",
                    borderBottom:
                      idx < children.length - 1
                        ? "1px solid var(--tf-border)"
                        : "none",
                    cursor: "pointer",
                    transition: "background var(--tf-tr)",
                  }}
                  onMouseEnter={(e) => {
                    (e.currentTarget as HTMLDivElement).style.background =
                      "var(--tf-bg4)";
                  }}
                  onMouseLeave={(e) => {
                    (e.currentTarget as HTMLDivElement).style.background =
                      "var(--tf-bg3)";
                  }}
                >
                  <TypeIcon type={child.type} size={14} />
                  <span
                    style={{
                      fontFamily: "var(--tf-font-mono)",
                      fontSize: 13,
                      color: "var(--tf-text3)",
                      minWidth: 60,
                      flexShrink: 0,
                    }}
                  >
                    #{child.id.slice(0, 8)}
                  </span>
                  <span
                    style={{
                      flex: 1,
                      fontSize: 13,
                      color: "var(--tf-text)",
                      fontFamily: "var(--tf-font-body)",
                      overflow: "hidden",
                      textOverflow: "ellipsis",
                      whiteSpace: "nowrap",
                    }}
                  >
                    {child.title}
                  </span>
                  <StatusBadge status={child.status} size="sm" />
                  {child.assigneeName ? (
                    <UserAvatar
                      initials={initials}
                      name={child.assigneeName}
                      subtitle={formatAssignedAt(child.assignedAt)}
                      size="xs"
                    />
                  ) : (
                    <div style={{ width: 18, flexShrink: 0 }} />
                  )}
                </div>
              );
            })}
          </div>
        )}
      </div>

      <CreateWorkItemDialog
        open={showCreateDialog}
        onOpenChange={setShowCreateDialog}
        projectId={projectId}
        defaultParentId={workItemId}
      />
    </>
  );
}
