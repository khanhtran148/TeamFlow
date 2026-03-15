"use client";

import { useState, useMemo } from "react";
import { X, Search, Loader2 } from "lucide-react";
import { toast } from "sonner";
import { useBacklog } from "@/lib/hooks/use-backlog";
import { useAssignItem } from "@/lib/hooks/use-releases";
import { TypeIcon } from "@/components/shared/type-icon";
import { StatusBadge } from "@/components/shared/status-badge";
import type { WorkItemDto } from "@/lib/api/types";
import type { ApiError } from "@/lib/api/client";

interface AssignItemDialogProps {
  open: boolean;
  releaseId: string;
  projectId: string;
  assignedItemIds: Set<string>;
  onClose: () => void;
}

export function AssignItemDialog({
  open,
  releaseId,
  projectId,
  assignedItemIds,
  onClose,
}: AssignItemDialogProps) {
  const [search, setSearch] = useState("");
  const [assigningId, setAssigningId] = useState<string | null>(null);

  const { data, isLoading } = useBacklog(
    { projectId, pageSize: 100 },
    { enabled: open },
  );

  const { mutate: assignItem } = useAssignItem(projectId);

  const filteredItems = useMemo(() => {
    const allItems = (data?.items ?? []) as WorkItemDto[];
    const available = allItems.filter((item) => !assignedItemIds.has(item.id));
    if (!search.trim()) return available;
    const lower = search.toLowerCase();
    return available.filter((item) => item.title.toLowerCase().includes(lower));
  }, [data, assignedItemIds, search]);

  function handleAssign(item: WorkItemDto) {
    setAssigningId(item.id);
    assignItem(
      { releaseId, workItemId: item.id },
      {
        onSuccess: () => {
          toast.success(`"${item.title}" added to release.`);
          setAssigningId(null);
        },
        onError: (err) => {
          const apiErr = err as ApiError;
          toast.error(apiErr.message ?? "Failed to assign item.");
          setAssigningId(null);
        },
      },
    );
  }

  function handleClose() {
    setSearch("");
    setAssigningId(null);
    onClose();
  }

  if (!open) return null;

  return (
    <div
      style={{
        position: "fixed",
        inset: 0,
        zIndex: 50,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        background: "rgba(0,0,0,0.6)",
        backdropFilter: "blur(2px)",
      }}
      onClick={handleClose}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="assign-item-title"
        onClick={(e) => e.stopPropagation()}
        style={{
          background: "var(--tf-bg2)",
          border: "1px solid var(--tf-border)",
          borderRadius: "var(--tf-radius)",
          boxShadow: "var(--tf-shadow)",
          width: "100%",
          maxWidth: 520,
          maxHeight: "80vh",
          display: "flex",
          flexDirection: "column",
          overflow: "hidden",
        }}
      >
        {/* Header */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            padding: "16px 20px",
            borderBottom: "1px solid var(--tf-border)",
          }}
        >
          <h2
            id="assign-item-title"
            style={{
              fontFamily: "var(--tf-font-head)",
              fontWeight: 700,
              fontSize: 16,
              color: "var(--tf-text)",
            }}
          >
            Assign Work Items
          </h2>
          <button
            onClick={handleClose}
            style={{
              width: 26,
              height: 26,
              borderRadius: 5,
              border: "1px solid var(--tf-border)",
              background: "transparent",
              color: "var(--tf-text3)",
              cursor: "pointer",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
            }}
          >
            <X size={13} />
          </button>
        </div>

        {/* Search */}
        <div style={{ padding: "12px 20px", borderBottom: "1px solid var(--tf-border)" }}>
          <div style={{ position: "relative" }}>
            <Search
              size={13}
              style={{
                position: "absolute",
                left: 10,
                top: "50%",
                transform: "translateY(-50%)",
                color: "var(--tf-text3)",
                pointerEvents: "none",
              }}
            />
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search work items..."
              autoFocus
              style={{
                width: "100%",
                padding: "7px 10px 7px 32px",
                borderRadius: 6,
                border: "1px solid var(--tf-border)",
                background: "var(--tf-bg3)",
                color: "var(--tf-text)",
                fontSize: 13,
                outline: "none",
                fontFamily: "var(--tf-font-body)",
                boxSizing: "border-box",
                transition: "border-color var(--tf-tr)",
              }}
              onFocus={(e) => {
                (e.currentTarget as HTMLInputElement).style.borderColor = "var(--tf-accent)";
              }}
              onBlur={(e) => {
                (e.currentTarget as HTMLInputElement).style.borderColor = "var(--tf-border)";
              }}
            />
          </div>
        </div>

        {/* Item list */}
        <div style={{ flex: 1, overflow: "auto" }}>
          {isLoading ? (
            <div
              style={{
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                padding: 40,
                color: "var(--tf-text3)",
                gap: 8,
                fontSize: 13,
              }}
            >
              <Loader2 size={14} style={{ animation: "spin 1s linear infinite" }} />
              Loading work items...
            </div>
          ) : filteredItems.length === 0 ? (
            <div
              style={{
                padding: 40,
                textAlign: "center",
                color: "var(--tf-text3)",
                fontSize: 13,
              }}
            >
              {search ? "No matching work items found." : "All work items are already assigned."}
            </div>
          ) : (
            <div>
              {filteredItems.map((item) => {
                const isAssigning = assigningId === item.id;
                return (
                  <div
                    key={item.id}
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: 10,
                      padding: "10px 20px",
                      borderBottom: "1px solid var(--tf-border)",
                      transition: "background var(--tf-tr)",
                    }}
                    onMouseEnter={(e) => {
                      (e.currentTarget as HTMLDivElement).style.background = "var(--tf-bg3)";
                    }}
                    onMouseLeave={(e) => {
                      (e.currentTarget as HTMLDivElement).style.background = "transparent";
                    }}
                  >
                    <TypeIcon type={item.type} size={14} />

                    <div style={{ flex: 1, minWidth: 0 }}>
                      <div
                        style={{
                          fontSize: 13,
                          color: "var(--tf-text)",
                          fontWeight: 500,
                          overflow: "hidden",
                          textOverflow: "ellipsis",
                          whiteSpace: "nowrap",
                        }}
                      >
                        {item.title}
                      </div>
                      <div style={{ marginTop: 3 }}>
                        <StatusBadge status={item.status} size="sm" />
                      </div>
                    </div>

                    <button
                      onClick={() => handleAssign(item)}
                      disabled={isAssigning}
                      style={{
                        padding: "5px 12px",
                        borderRadius: 5,
                        border: "1px solid var(--tf-accent)",
                        background: "transparent",
                        color: "var(--tf-accent)",
                        fontSize: 11,
                        fontWeight: 600,
                        cursor: isAssigning ? "not-allowed" : "pointer",
                        opacity: isAssigning ? 0.6 : 1,
                        fontFamily: "var(--tf-font-body)",
                        flexShrink: 0,
                        transition: "background var(--tf-tr)",
                        whiteSpace: "nowrap",
                      }}
                      onMouseEnter={(e) => {
                        if (!isAssigning) {
                          (e.currentTarget as HTMLButtonElement).style.background =
                            "var(--tf-accent-dim2)";
                        }
                      }}
                      onMouseLeave={(e) => {
                        (e.currentTarget as HTMLButtonElement).style.background = "transparent";
                      }}
                    >
                      {isAssigning ? "Adding..." : "Add"}
                    </button>
                  </div>
                );
              })}
            </div>
          )}
        </div>

        {/* Footer */}
        <div
          style={{
            padding: "12px 20px",
            borderTop: "1px solid var(--tf-border)",
            display: "flex",
            justifyContent: "flex-end",
          }}
        >
          <button
            onClick={handleClose}
            style={{
              padding: "7px 14px",
              borderRadius: 6,
              border: "1px solid var(--tf-border)",
              background: "transparent",
              color: "var(--tf-text2)",
              fontSize: 12,
              fontWeight: 500,
              cursor: "pointer",
            }}
          >
            Done
          </button>
        </div>
      </div>
    </div>
  );
}
