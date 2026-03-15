"use client";

import { useState } from "react";
import { toast } from "sonner";
import { Loader2, AlertCircle } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useAddLink } from "@/lib/hooks/use-work-items";
import { useBacklog } from "@/lib/hooks/use-backlog";
import { TypeIcon } from "@/components/shared/type-icon";
import { StatusBadge } from "@/components/shared/status-badge";
import type { LinkType, BacklogItemDto } from "@/lib/api/types";

const LINK_TYPE_OPTIONS: { value: LinkType; label: string; description: string }[] = [
  { value: "Blocks", label: "Blocks", description: "This item blocks the target" },
  { value: "RelatesTo", label: "Relates To", description: "These items are related" },
  { value: "DependsOn", label: "Depends On", description: "This item depends on the target" },
  { value: "Duplicates", label: "Duplicates", description: "This item duplicates the target" },
  { value: "Causes", label: "Causes", description: "This item causes the target" },
  { value: "Clones", label: "Clones", description: "This item is a clone of the target" },
];

interface AddLinkDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  workItemId: string;
  projectId: string;
}

export function AddLinkDialog({
  open,
  onOpenChange,
  workItemId,
  projectId,
}: AddLinkDialogProps) {
  const [linkType, setLinkType] = useState<LinkType>("RelatesTo");
  const [search, setSearch] = useState("");
  const [selectedItem, setSelectedItem] = useState<BacklogItemDto | null>(null);
  const [apiError, setApiError] = useState<string | null>(null);

  const addLinkMutation = useAddLink(projectId);

  const { data: backlogData, isLoading: isSearching } = useBacklog(
    { projectId, search: search.trim() || undefined, pageSize: 20 },
    { enabled: open },
  );

  const items = (backlogData?.items ?? []).filter((item) => item.id !== workItemId);

  function handleClose() {
    setLinkType("RelatesTo");
    setSearch("");
    setSelectedItem(null);
    setApiError(null);
    onOpenChange(false);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!selectedItem) {
      toast.error("Select a target work item");
      return;
    }

    setApiError(null);

    try {
      await addLinkMutation.mutateAsync({
        id: workItemId,
        data: { targetId: selectedItem.id, linkType },
      });
      toast.success(`Link added: ${linkTypeLabel(linkType)} "${selectedItem.title}"`);
      handleClose();
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to add link";
      setApiError(message);
    }
  }

  const selectStyle: React.CSSProperties = {
    width: "100%",
    padding: "7px 10px",
    borderRadius: 6,
    border: "1px solid var(--tf-border)",
    background: "var(--tf-bg4)",
    color: "var(--tf-text)",
    fontSize: 13,
    fontFamily: "var(--tf-font-body)",
    outline: "none",
    appearance: "none",
  };

  const labelStyle: React.CSSProperties = {
    display: "block",
    fontSize: 11,
    fontWeight: 600,
    color: "var(--tf-text2)",
    fontFamily: "var(--tf-font-body)",
    marginBottom: 4,
    textTransform: "uppercase",
    letterSpacing: "0.05em",
  };

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent
        style={{
          background: "var(--tf-bg2)",
          border: "1px solid var(--tf-border)",
          color: "var(--tf-text)",
          maxWidth: 520,
        }}
      >
        <DialogHeader>
          <DialogTitle
            style={{
              fontFamily: "var(--tf-font-head)",
              color: "var(--tf-text)",
              fontSize: 16,
            }}
          >
            Add Link
          </DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit}>
          <div style={{ display: "flex", flexDirection: "column", gap: 14 }}>
            {/* Link type */}
            <div>
              <label style={labelStyle}>Link Type</label>
              <select
                value={linkType}
                onChange={(e) => setLinkType(e.target.value as LinkType)}
                style={selectStyle}
              >
                {LINK_TYPE_OPTIONS.map((opt) => (
                  <option key={opt.value} value={opt.value}>
                    {opt.label} — {opt.description}
                  </option>
                ))}
              </select>
            </div>

            {/* Search work items */}
            <div>
              <label style={labelStyle}>Target Work Item</label>
              <Input
                value={search}
                onChange={(e) => {
                  setSearch(e.target.value);
                  setSelectedItem(null);
                }}
                placeholder="Search by title…"
                style={{
                  background: "var(--tf-bg4)",
                  borderColor: "var(--tf-border)",
                  color: "var(--tf-text)",
                  fontSize: 13,
                  marginBottom: 6,
                }}
              />

              {/* Results list */}
              <div
                style={{
                  maxHeight: 200,
                  overflowY: "auto",
                  border: "1px solid var(--tf-border)",
                  borderRadius: 6,
                  background: "var(--tf-bg3)",
                }}
              >
                {isSearching ? (
                  <div
                    style={{
                      padding: 16,
                      display: "flex",
                      alignItems: "center",
                      justifyContent: "center",
                      color: "var(--tf-text3)",
                      fontSize: 12,
                    }}
                  >
                    <Loader2 size={14} className="animate-spin" />
                  </div>
                ) : items.length === 0 ? (
                  <div
                    style={{
                      padding: 16,
                      textAlign: "center",
                      color: "var(--tf-text3)",
                      fontSize: 12,
                    }}
                  >
                    {search ? "No matching items" : "No items found"}
                  </div>
                ) : (
                  items.map((item) => {
                    const isSelected = selectedItem?.id === item.id;
                    return (
                      <button
                        key={item.id}
                        type="button"
                        onClick={() => setSelectedItem(item)}
                        style={{
                          display: "flex",
                          alignItems: "center",
                          gap: 8,
                          width: "100%",
                          padding: "7px 10px",
                          background: isSelected
                            ? "var(--tf-accent-dim)"
                            : "transparent",
                          border: "none",
                          borderBottom: "1px solid var(--tf-border)",
                          cursor: "pointer",
                          textAlign: "left",
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
                        {isSelected && (
                          <span style={{ color: "var(--tf-accent)", fontSize: 12 }}>
                            ✓
                          </span>
                        )}
                      </button>
                    );
                  })
                )}
              </div>
            </div>

            {/* Selected item preview */}
            {selectedItem && (
              <div
                style={{
                  background: "var(--tf-bg3)",
                  border: "1px solid var(--tf-accent-dim2)",
                  borderRadius: 6,
                  padding: "8px 12px",
                  fontSize: 12,
                  color: "var(--tf-text2)",
                  display: "flex",
                  alignItems: "center",
                  gap: 8,
                }}
              >
                <TypeIcon type={selectedItem.type} size={14} />
                <span style={{ color: "var(--tf-accent)", fontWeight: 600 }}>
                  This item {linkTypeLabel(linkType).toLowerCase()}:
                </span>
                <span
                  style={{
                    flex: 1,
                    color: "var(--tf-text)",
                    overflow: "hidden",
                    textOverflow: "ellipsis",
                    whiteSpace: "nowrap",
                  }}
                >
                  {selectedItem.title}
                </span>
              </div>
            )}

            {/* API error */}
            {apiError && (
              <div
                style={{
                  display: "flex",
                  alignItems: "flex-start",
                  gap: 8,
                  background: "var(--tf-red-dim)",
                  border: "1px solid var(--tf-red)",
                  borderRadius: 6,
                  padding: "8px 12px",
                  fontSize: 12,
                  color: "var(--tf-red)",
                }}
              >
                <AlertCircle size={14} style={{ flexShrink: 0, marginTop: 1 }} />
                {apiError}
              </div>
            )}
          </div>

          <DialogFooter style={{ marginTop: 20 }}>
            <Button
              type="button"
              variant="ghost"
              onClick={handleClose}
              disabled={addLinkMutation.isPending}
              style={{ color: "var(--tf-text2)" }}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={addLinkMutation.isPending || !selectedItem}
              style={{
                background: "var(--tf-accent)",
                color: "var(--tf-bg)",
                fontWeight: 600,
              }}
            >
              {addLinkMutation.isPending ? (
                <>
                  <Loader2 size={13} className="animate-spin" />
                  Adding…
                </>
              ) : (
                "Add Link"
              )}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function linkTypeLabel(lt: LinkType): string {
  const map: Record<LinkType, string> = {
    Blocks: "Blocks",
    RelatesTo: "Relates To",
    DependsOn: "Depends On",
    Duplicates: "Duplicates",
    Causes: "Causes",
    Clones: "Clones",
  };
  return map[lt];
}
