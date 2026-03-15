"use client";

import { useState } from "react";
import { toast } from "sonner";
import { Plus, X, Loader2, Link2 } from "lucide-react";
import { useWorkItemLinks, useRemoveLink } from "@/lib/hooks/use-work-items";
import { TypeIcon } from "@/components/shared/type-icon";
import { StatusBadge } from "@/components/shared/status-badge";
import { AddLinkDialog } from "./add-link-dialog";
import type { LinkType } from "@/lib/api/types";

const LINK_TYPE_LABELS: Record<LinkType, string> = {
  Blocks: "Blocks",
  RelatesTo: "Relates To",
  DependsOn: "Depends On",
  Duplicates: "Duplicates",
  Causes: "Causes",
  Clones: "Clones",
};

const LINK_TYPE_COLORS: Record<LinkType, string> = {
  Blocks: "var(--tf-red)",
  RelatesTo: "var(--tf-blue)",
  DependsOn: "var(--tf-yellow)",
  Duplicates: "var(--tf-text2)",
  Causes: "var(--tf-orange)",
  Clones: "var(--tf-violet)",
};

interface LinksTabProps {
  workItemId: string;
  projectId: string;
}

export function LinksTab({ workItemId, projectId }: LinksTabProps) {
  const [showAddDialog, setShowAddDialog] = useState(false);
  const [removingLinkId, setRemovingLinkId] = useState<string | null>(null);

  const { data: linksData, isLoading } = useWorkItemLinks(workItemId);
  const removeLinkMutation = useRemoveLink(projectId);

  async function handleRemoveLink(linkId: string, targetTitle: string) {
    setRemovingLinkId(linkId);
    try {
      await removeLinkMutation.mutateAsync({ id: workItemId, linkId });
      toast.success(`Link to "${targetTitle}" removed`);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to remove link";
      toast.error(message);
    } finally {
      setRemovingLinkId(null);
    }
  }

  if (isLoading) {
    return (
      <div
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          padding: 32,
          color: "var(--tf-text3)",
          fontSize: 13,
        }}
      >
        <Loader2 size={16} className="animate-spin" />
      </div>
    );
  }

  const groups = linksData?.groups ?? [];
  const totalLinks = groups.reduce((sum, g) => sum + g.items.length, 0);

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
              fontSize: 12,
              color: "var(--tf-text2)",
              fontFamily: "var(--tf-font-body)",
            }}
          >
            {totalLinks} link{totalLinks !== 1 ? "s" : ""}
          </div>
          <button
            onClick={() => setShowAddDialog(true)}
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
              fontSize: 12,
              fontFamily: "var(--tf-font-body)",
            }}
          >
            <Plus size={13} />
            Add Link
          </button>
        </div>

        {/* Empty state */}
        {groups.length === 0 && (
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
            <Link2 size={28} style={{ marginBottom: 10, opacity: 0.4 }} />
            <div style={{ fontSize: 13, fontFamily: "var(--tf-font-body)" }}>
              No links yet
            </div>
            <div style={{ fontSize: 11, marginTop: 4 }}>
              Link work items to show relationships like blocks, depends on, and more.
            </div>
          </div>
        )}

        {/* Link groups */}
        {groups.map((group) => (
          <div key={group.linkType} style={{ marginBottom: 20 }}>
            {/* Group header */}
            <div
              style={{
                display: "flex",
                alignItems: "center",
                gap: 8,
                marginBottom: 8,
              }}
            >
              <span
                style={{
                  width: 8,
                  height: 8,
                  borderRadius: "50%",
                  background: LINK_TYPE_COLORS[group.linkType],
                  flexShrink: 0,
                }}
              />
              <span
                style={{
                  fontSize: 11,
                  fontWeight: 600,
                  color: LINK_TYPE_COLORS[group.linkType],
                  fontFamily: "var(--tf-font-body)",
                  textTransform: "uppercase",
                  letterSpacing: "0.05em",
                }}
              >
                {LINK_TYPE_LABELS[group.linkType]}
              </span>
              <span
                style={{
                  fontSize: 10,
                  color: "var(--tf-text3)",
                  fontFamily: "var(--tf-font-mono)",
                }}
              >
                ({group.items.length})
              </span>
            </div>

            {/* Items in group */}
            <div
              style={{
                border: "1px solid var(--tf-border)",
                borderRadius: 6,
                overflow: "hidden",
              }}
            >
              {group.items.map((item, idx) => (
                <div
                  key={item.id}
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: 8,
                    padding: "8px 12px",
                    background: "var(--tf-bg3)",
                    borderBottom:
                      idx < group.items.length - 1
                        ? "1px solid var(--tf-border)"
                        : "none",
                  }}
                >
                  <TypeIcon type={item.type} size={14} />
                  <span
                    style={{
                      fontFamily: "var(--tf-font-mono)",
                      fontSize: 10,
                      color: "var(--tf-text3)",
                      minWidth: 60,
                      flexShrink: 0,
                    }}
                  >
                    #{item.id.slice(0, 8)}
                  </span>
                  <span
                    style={{
                      flex: 1,
                      fontSize: 12,
                      color: "var(--tf-text)",
                      fontFamily: "var(--tf-font-body)",
                      overflow: "hidden",
                      textOverflow: "ellipsis",
                      whiteSpace: "nowrap",
                    }}
                  >
                    {item.title}
                  </span>
                  <StatusBadge status={item.status} size="sm" />
                  <button
                    onClick={() => handleRemoveLink(item.id, item.title)}
                    disabled={removingLinkId === item.id}
                    title="Remove link"
                    style={{
                      display: "flex",
                      alignItems: "center",
                      justifyContent: "center",
                      width: 22,
                      height: 22,
                      borderRadius: 4,
                      background: "none",
                      border: "none",
                      cursor: "pointer",
                      color: "var(--tf-text3)",
                      flexShrink: 0,
                    }}
                    onMouseEnter={(e) => {
                      (e.currentTarget as HTMLButtonElement).style.color = "var(--tf-red)";
                      (e.currentTarget as HTMLButtonElement).style.background = "var(--tf-red-dim)";
                    }}
                    onMouseLeave={(e) => {
                      (e.currentTarget as HTMLButtonElement).style.color = "var(--tf-text3)";
                      (e.currentTarget as HTMLButtonElement).style.background = "none";
                    }}
                  >
                    {removingLinkId === item.id ? (
                      <Loader2 size={11} className="animate-spin" />
                    ) : (
                      <X size={11} />
                    )}
                  </button>
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>

      <AddLinkDialog
        open={showAddDialog}
        onOpenChange={setShowAddDialog}
        workItemId={workItemId}
        projectId={projectId}
      />
    </>
  );
}
