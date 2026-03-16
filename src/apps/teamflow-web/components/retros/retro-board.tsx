"use client";

import { useState } from "react";
import { Pencil, X, Plus, Check } from "lucide-react";
import { toast } from "sonner";
import { RetroCard } from "./retro-card";
import { RetroCardForm } from "./retro-card-form";
import { RetroSessionControls } from "./retro-session-controls";
import { RetroActionItemForm } from "./retro-action-item-form";
import { RetroActionItemList } from "./retro-action-item-list";
import { RetroPreviousActions } from "./retro-previous-actions";
import { RetroSummary } from "./retro-summary";
import {
  useStartRetroSession,
  useTransitionRetroSession,
  useCloseRetroSession,
  useSubmitRetroCard,
  useCastRetroVote,
  useMarkCardDiscussed,
  useCreateRetroActionItem,
  useUpdateColumnsConfig,
} from "@/lib/hooks/use-retros";
import { useHasPermission } from "@/lib/hooks/use-permission";
import type {
  RetroSessionDto,
  RetroCardCategory,
  RetroSessionStatus,
  RetroColumnConfig,
} from "@/lib/api/types";
import type { ApiError } from "@/lib/api/client";

interface RetroBoardProps {
  session: RetroSessionDto;
  projectId: string;
}

const DEFAULT_COLUMNS: RetroColumnConfig[] = [
  { key: "WentWell", label: "Went Well", headerColor: "var(--tf-accent)", visible: true },
  { key: "NeedsImprovement", label: "Needs Improvement", headerColor: "var(--tf-orange)", visible: true },
  { key: "ActionItem", label: "Action Items", headerColor: "var(--tf-blue)", visible: true },
];

export function RetroBoard({ session, projectId }: RetroBoardProps) {
  const canFacilitate = useHasPermission(projectId, "Retro_Facilitate");
  const canSubmitCard = useHasPermission(projectId, "Retro_SubmitCard");
  const canVote = useHasPermission(projectId, "Retro_Vote");

  const startMutation = useStartRetroSession(projectId);
  const transitionMutation = useTransitionRetroSession(projectId);
  const closeMutation = useCloseRetroSession(projectId);
  const submitCardMutation = useSubmitRetroCard(projectId);
  const castVoteMutation = useCastRetroVote(projectId);
  const markDiscussedMutation = useMarkCardDiscussed(projectId);
  const createActionMutation = useCreateRetroActionItem(projectId);
  const updateColumnsMutation = useUpdateColumnsConfig();

  // Inline rename state per column key
  const [renamingKey, setRenamingKey] = useState<RetroCardCategory | null>(null);
  const [renameValue, setRenameValue] = useState("");

  const columns: RetroColumnConfig[] = session.columnsConfig ?? DEFAULT_COLUMNS;
  const visibleColumns = columns.filter((c) => c.visible);
  const hiddenColumns = columns.filter((c) => !c.visible);

  const isOpen = session.status === "Open";
  const isVoting = session.status === "Voting";
  const isDiscussing = session.status === "Discussing";
  const isClosed = session.status === "Closed";

  async function handleStart() {
    try {
      await startMutation.mutateAsync(session.id);
      toast.success("Session started");
    } catch (err) {
      toast.error((err as ApiError).message ?? "Failed to start");
    }
  }

  async function handleTransition(target: RetroSessionStatus) {
    try {
      await transitionMutation.mutateAsync({
        sessionId: session.id,
        data: { targetStatus: target },
      });
      toast.success(`Transitioned to ${target}`);
    } catch (err) {
      toast.error((err as ApiError).message ?? "Failed to transition");
    }
  }

  async function handleClose() {
    try {
      await closeMutation.mutateAsync(session.id);
      toast.success("Session closed");
    } catch (err) {
      toast.error((err as ApiError).message ?? "Failed to close");
    }
  }

  async function handleSubmitCard(category: RetroCardCategory, content: string) {
    try {
      await submitCardMutation.mutateAsync({
        sessionId: session.id,
        data: { category, content },
      });
    } catch (err) {
      toast.error((err as ApiError).message ?? "Failed to submit card");
    }
  }

  async function handleVote(cardId: string, voteCount: number) {
    try {
      await castVoteMutation.mutateAsync({
        sessionId: session.id,
        cardId,
        data: { voteCount },
      });
    } catch (err) {
      toast.error((err as ApiError).message ?? "Failed to vote");
    }
  }

  async function handleMarkDiscussed(cardId: string) {
    try {
      await markDiscussedMutation.mutateAsync({
        sessionId: session.id,
        cardId,
      });
    } catch (err) {
      toast.error(
        (err as ApiError).message ?? "Failed to mark as discussed",
      );
    }
  }

  async function handleCreateAction(data: {
    title: string;
    description?: string;
    assigneeId?: string;
    dueDate?: string;
    linkToBacklog?: boolean;
  }) {
    try {
      await createActionMutation.mutateAsync({
        sessionId: session.id,
        data,
      });
      toast.success("Action item created");
    } catch (err) {
      toast.error(
        (err as ApiError).message ?? "Failed to create action item",
      );
    }
  }

  function saveColumns(updated: RetroColumnConfig[]) {
    updateColumnsMutation.mutate({ sessionId: session.id, columnsConfig: updated });
  }

  function startRename(col: RetroColumnConfig) {
    setRenamingKey(col.key);
    setRenameValue(col.label);
  }

  function commitRename(key: RetroCardCategory) {
    const trimmed = renameValue.trim();
    if (!trimmed) {
      setRenamingKey(null);
      return;
    }
    const updated = columns.map((c) =>
      c.key === key ? { ...c, label: trimmed } : c,
    );
    saveColumns(updated);
    setRenamingKey(null);
  }

  function cancelRename() {
    setRenamingKey(null);
  }

  function removeColumn(key: RetroCardCategory) {
    const updated = columns.map((c) =>
      c.key === key ? { ...c, visible: false } : c,
    );
    saveColumns(updated);
  }

  function restoreColumn(key: RetroCardCategory) {
    const updated = columns.map((c) =>
      c.key === key ? { ...c, visible: true } : c,
    );
    saveColumns(updated);
  }

  // Show summary for closed sessions
  if (isClosed) {
    return (
      <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
        <RetroSummary session={session} />
        <RetroActionItemList
          items={session.actionItems}
          projectId={projectId}
        />
      </div>
    );
  }

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
      {/* Previous action items banner */}
      <RetroPreviousActions projectId={projectId} />

      {/* Facilitator controls */}
      <RetroSessionControls
        status={session.status}
        canFacilitate={canFacilitate}
        onStart={handleStart}
        onTransition={handleTransition}
        onClose={handleClose}
        isPending={
          startMutation.isPending ||
          transitionMutation.isPending ||
          closeMutation.isPending
        }
      />

      {/* Status info */}
      <div
        style={{
          fontSize: 13,
          color: "var(--tf-text3)",
          display: "flex",
          alignItems: "center",
          gap: 8,
        }}
      >
        <span>
          Mode:{" "}
          <strong style={{ color: "var(--tf-text2)" }}>
            {session.anonymityMode}
          </strong>
        </span>
        <span>|</span>
        <span>
          Facilitator:{" "}
          <strong style={{ color: "var(--tf-text2)" }}>
            {session.facilitatorName}
          </strong>
        </span>
        <span>|</span>
        <span>
          {session.cards.length} card
          {session.cards.length !== 1 ? "s" : ""}
        </span>
      </div>

      {/* Hidden column restore pills - only for facilitators */}
      {canFacilitate && hiddenColumns.length > 0 && (
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 6,
            flexWrap: "wrap",
          }}
        >
          <span style={{ fontSize: 13, color: "var(--tf-text3)" }}>
            Hidden:
          </span>
          {hiddenColumns.map((col) => (
            <button
              key={col.key}
              onClick={() => restoreColumn(col.key)}
              style={{
                display: "inline-flex",
                alignItems: "center",
                gap: 4,
                padding: "2px 8px",
                borderRadius: 100,
                fontSize: 13,
                border: "1px dashed var(--tf-border)",
                background: "var(--tf-bg3)",
                color: "var(--tf-text3)",
                cursor: "pointer",
                fontFamily: "var(--tf-font-body)",
                transition: "all var(--tf-tr)",
              }}
              onMouseEnter={(e) => {
                (e.currentTarget as HTMLButtonElement).style.borderColor = "var(--tf-accent)";
                (e.currentTarget as HTMLButtonElement).style.color = "var(--tf-accent)";
              }}
              onMouseLeave={(e) => {
                (e.currentTarget as HTMLButtonElement).style.borderColor = "var(--tf-border)";
                (e.currentTarget as HTMLButtonElement).style.color = "var(--tf-text3)";
              }}
            >
              <Plus size={10} />
              {col.label}
            </button>
          ))}
        </div>
      )}

      {/* Board columns */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns: `repeat(${visibleColumns.length}, 1fr)`,
          gap: 16,
          minHeight: 300,
        }}
      >
        {visibleColumns.map((col) => {
          const columnCards = session.cards
            .filter((c) => c.category === col.key)
            .sort((a, b) => b.totalVotes - a.totalVotes);
          const isRenaming = renamingKey === col.key;

          return (
            <div
              key={col.key}
              style={{
                background: "var(--tf-bg2)",
                border: "1px solid var(--tf-border)",
                borderRadius: 8,
                display: "flex",
                flexDirection: "column",
              }}
            >
              {/* Column header */}
              <div
                style={{
                  padding: "10px 14px",
                  borderBottom: `2px solid ${col.headerColor}`,
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "space-between",
                  gap: 8,
                }}
              >
                {isRenaming ? (
                  <div style={{ display: "flex", alignItems: "center", gap: 4, flex: 1 }}>
                    <input
                      autoFocus
                      value={renameValue}
                      onChange={(e) => setRenameValue(e.target.value)}
                      onKeyDown={(e) => {
                        if (e.key === "Enter") commitRename(col.key);
                        if (e.key === "Escape") cancelRename();
                      }}
                      style={{
                        flex: 1,
                        padding: "2px 6px",
                        fontSize: 13,
                        fontWeight: 600,
                        background: "var(--tf-bg4)",
                        border: "1px solid var(--tf-accent)",
                        borderRadius: 4,
                        color: col.headerColor,
                        fontFamily: "var(--tf-font-head)",
                        outline: "none",
                      }}
                    />
                    <button
                      onClick={() => commitRename(col.key)}
                      aria-label="Confirm rename"
                      style={{
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "center",
                        width: 20,
                        height: 20,
                        borderRadius: 4,
                        border: "none",
                        background: "var(--tf-accent)",
                        color: "var(--tf-bg)",
                        cursor: "pointer",
                        flexShrink: 0,
                      }}
                    >
                      <Check size={11} />
                    </button>
                    <button
                      onClick={cancelRename}
                      aria-label="Cancel rename"
                      style={{
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "center",
                        width: 20,
                        height: 20,
                        borderRadius: 4,
                        border: "1px solid var(--tf-border)",
                        background: "transparent",
                        color: "var(--tf-text3)",
                        cursor: "pointer",
                        flexShrink: 0,
                      }}
                    >
                      <X size={11} />
                    </button>
                  </div>
                ) : (
                  <>
                    <span
                      style={{
                        fontFamily: "var(--tf-font-head)",
                        fontWeight: 600,
                        fontSize: 13,
                        color: col.headerColor,
                        flex: 1,
                      }}
                    >
                      {col.label}
                    </span>
                    {canFacilitate && (
                      <div style={{ display: "flex", alignItems: "center", gap: 4 }}>
                        <button
                          onClick={() => startRename(col)}
                          aria-label={`Rename ${col.label}`}
                          style={{
                            display: "flex",
                            alignItems: "center",
                            justifyContent: "center",
                            width: 20,
                            height: 20,
                            borderRadius: 4,
                            border: "none",
                            background: "transparent",
                            color: "var(--tf-text3)",
                            cursor: "pointer",
                          }}
                          onMouseEnter={(e) => {
                            (e.currentTarget as HTMLButtonElement).style.color = "var(--tf-text)";
                          }}
                          onMouseLeave={(e) => {
                            (e.currentTarget as HTMLButtonElement).style.color = "var(--tf-text3)";
                          }}
                        >
                          <Pencil size={11} />
                        </button>
                        <button
                          onClick={() => removeColumn(col.key)}
                          aria-label={`Remove ${col.label} column`}
                          style={{
                            display: "flex",
                            alignItems: "center",
                            justifyContent: "center",
                            width: 20,
                            height: 20,
                            borderRadius: 4,
                            border: "none",
                            background: "transparent",
                            color: "var(--tf-text3)",
                            cursor: "pointer",
                          }}
                          onMouseEnter={(e) => {
                            (e.currentTarget as HTMLButtonElement).style.color = "var(--tf-red)";
                          }}
                          onMouseLeave={(e) => {
                            (e.currentTarget as HTMLButtonElement).style.color = "var(--tf-text3)";
                          }}
                        >
                          <X size={11} />
                        </button>
                      </div>
                    )}
                    <span
                      style={{
                        padding: "1px 7px",
                        borderRadius: 100,
                        background: "var(--tf-bg4)",
                        color: "var(--tf-text3)",
                        fontSize: 13,
                        fontFamily: "var(--tf-font-mono)",
                        flexShrink: 0,
                      }}
                    >
                      {columnCards.length}
                    </span>
                  </>
                )}
              </div>

              {/* Cards */}
              <div
                style={{
                  flex: 1,
                  padding: 10,
                  display: "flex",
                  flexDirection: "column",
                  gap: 8,
                  overflowY: "auto",
                }}
              >
                {columnCards.map((card) => (
                  <RetroCard
                    key={card.id}
                    card={card}
                    isVotingPhase={isVoting}
                    isDiscussingPhase={isDiscussing}
                    canVote={canVote}
                    canFacilitate={canFacilitate}
                    onVote={handleVote}
                    onMarkDiscussed={handleMarkDiscussed}
                  />
                ))}
              </div>

              {/* Add card form - only when session is Open */}
              {isOpen && canSubmitCard && (
                <div
                  style={{
                    padding: 10,
                    borderTop: "1px solid var(--tf-border)",
                  }}
                >
                  <RetroCardForm
                    category={col.key}
                    onSubmit={(content) => handleSubmitCard(col.key, content)}
                    isPending={submitCardMutation.isPending}
                  />
                </div>
              )}
            </div>
          );
        })}
      </div>

      {/* Action items section - during Discussing phase */}
      {isDiscussing && (
        <div
          style={{
            background: "var(--tf-bg2)",
            border: "1px solid var(--tf-border)",
            borderRadius: 8,
            padding: 16,
          }}
        >
          <RetroActionItemList
            items={session.actionItems}
            projectId={projectId}
          />
          {canFacilitate && (
            <div style={{ marginTop: 12 }}>
              <RetroActionItemForm
                onSubmit={handleCreateAction}
                isPending={createActionMutation.isPending}
              />
            </div>
          )}
        </div>
      )}
    </div>
  );
}
