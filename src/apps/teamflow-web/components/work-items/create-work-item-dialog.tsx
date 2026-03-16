"use client";

import { useState } from "react";
import { toast } from "sonner";
import { Loader2 } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useCreateWorkItem } from "@/lib/hooks/use-work-items";
import { useBacklog } from "@/lib/hooks/use-backlog";
import type {
  WorkItemType,
  Priority,
  BacklogItemDto,
} from "@/lib/api/types";

const TYPE_OPTIONS: { value: WorkItemType; label: string }[] = [
  { value: "Epic", label: "Epic" },
  { value: "UserStory", label: "User Story" },
  { value: "Task", label: "Task" },
  { value: "Bug", label: "Bug" },
  { value: "Spike", label: "Spike" },
];

const PRIORITY_OPTIONS: { value: Priority; label: string }[] = [
  { value: "Critical", label: "Critical" },
  { value: "High", label: "High" },
  { value: "Medium", label: "Medium" },
  { value: "Low", label: "Low" },
];

interface CreateWorkItemDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  projectId: string;
  defaultParentId?: string;
}

export function CreateWorkItemDialog({
  open,
  onOpenChange,
  projectId,
  defaultParentId,
}: CreateWorkItemDialogProps) {
  const [type, setType] = useState<WorkItemType>("UserStory");
  const [title, setTitle] = useState("");
  const [priority, setPriority] = useState<Priority>("Medium");
  const [description, setDescription] = useState("");
  const [parentId, setParentId] = useState(defaultParentId ?? "");

  const createMutation = useCreateWorkItem(projectId);

  // Load epics and stories for parent dropdown
  const { data: epicsData } = useBacklog(
    { projectId, type: "Epic", pageSize: 100 },
    { enabled: open && type !== "Epic" },
  );
  const { data: storiesData } = useBacklog(
    { projectId, type: "UserStory", pageSize: 100 },
    { enabled: open && (type === "Task" || type === "Bug" || type === "Spike") },
  );

  const parentOptions: BacklogItemDto[] = [
    ...(epicsData?.items ?? []),
    ...(storiesData?.items ?? []),
  ];

  function handleClose() {
    setTitle("");
    setDescription("");
    setType("UserStory");
    setPriority("Medium");
    setParentId(defaultParentId ?? "");
    onOpenChange(false);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();

    if (!title.trim()) {
      toast.error("Title is required");
      return;
    }

    try {
      await createMutation.mutateAsync({
        projectId,
        type,
        title: title.trim(),
        description: description.trim() || undefined,
        priority,
        parentId: parentId || undefined,
      });
      toast.success(`${type} created successfully`);
      handleClose();
    } catch (err) {
      const message =
        err instanceof Error ? err.message : "Failed to create work item";
      toast.error(message);
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
    fontSize: 13,
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
            Create Work Item
          </DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit}>
          <div style={{ display: "flex", flexDirection: "column", gap: 14 }}>
            {/* Type */}
            <div>
              <label style={labelStyle}>Type</label>
              <select
                value={type}
                onChange={(e) => {
                  setType(e.target.value as WorkItemType);
                  if (e.target.value === "Epic") setParentId("");
                }}
                style={selectStyle}
              >
                {TYPE_OPTIONS.map((opt) => (
                  <option key={opt.value} value={opt.value}>
                    {opt.label}
                  </option>
                ))}
              </select>
            </div>

            {/* Title */}
            <div>
              <label style={labelStyle}>Title *</label>
              <Input
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder={`Enter ${type.toLowerCase()} title…`}
                autoFocus
                style={{
                  background: "var(--tf-bg4)",
                  borderColor: "var(--tf-border)",
                  color: "var(--tf-text)",
                  fontSize: 13,
                }}
              />
            </div>

            {/* Parent (not for Epics) */}
            {type !== "Epic" && (
              <div>
                <label style={labelStyle}>Parent (optional)</label>
                <select
                  value={parentId}
                  onChange={(e) => setParentId(e.target.value)}
                  style={selectStyle}
                >
                  <option value="">— None —</option>
                  {parentOptions.map((opt) => (
                    <option key={opt.id} value={opt.id}>
                      [{opt.type}] {opt.title}
                    </option>
                  ))}
                </select>
              </div>
            )}

            {/* Priority */}
            <div>
              <label style={labelStyle}>Priority</label>
              <select
                value={priority}
                onChange={(e) => setPriority(e.target.value as Priority)}
                style={selectStyle}
              >
                {PRIORITY_OPTIONS.map((opt) => (
                  <option key={opt.value} value={opt.value}>
                    {opt.label}
                  </option>
                ))}
              </select>
            </div>

            {/* Description */}
            <div>
              <label style={labelStyle}>Description (optional)</label>
              <textarea
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Add a description…"
                rows={3}
                style={{
                  ...selectStyle,
                  resize: "vertical",
                  lineHeight: 1.5,
                }}
              />
            </div>
          </div>

          <DialogFooter style={{ marginTop: 20 }}>
            <Button
              type="button"
              variant="ghost"
              onClick={handleClose}
              disabled={createMutation.isPending}
              style={{ color: "var(--tf-text2)" }}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={createMutation.isPending || !title.trim()}
              style={{
                background: "var(--tf-accent)",
                color: "var(--tf-bg)",
                fontWeight: 600,
              }}
            >
              {createMutation.isPending ? (
                <>
                  <Loader2 size={13} className="animate-spin" />
                  Creating…
                </>
              ) : (
                "Create"
              )}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
