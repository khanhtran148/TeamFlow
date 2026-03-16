"use client";

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
} from "@/lib/hooks/use-retros";
import { useHasPermission } from "@/lib/hooks/use-permission";
import type { RetroSessionDto, RetroCardCategory, RetroSessionStatus } from "@/lib/api/types";
import type { ApiError } from "@/lib/api/client";

interface RetroBoardProps {
  session: RetroSessionDto;
  projectId: string;
}

const COLUMNS: { key: RetroCardCategory; label: string; headerColor: string }[] = [
  { key: "WentWell", label: "Went Well", headerColor: "var(--tf-accent)" },
  {
    key: "NeedsImprovement",
    label: "Needs Improvement",
    headerColor: "var(--tf-orange)",
  },
  { key: "ActionItem", label: "Action Items", headerColor: "var(--tf-blue)" },
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
          fontSize: 12,
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

      {/* Three-column board */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "repeat(3, 1fr)",
          gap: 16,
          minHeight: 300,
        }}
      >
        {COLUMNS.map((col) => {
          const columnCards = session.cards
            .filter((c) => c.category === col.key)
            .sort((a, b) => b.totalVotes - a.totalVotes);

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
                }}
              >
                <span
                  style={{
                    fontFamily: "var(--tf-font-head)",
                    fontWeight: 600,
                    fontSize: 13,
                    color: col.headerColor,
                  }}
                >
                  {col.label}
                </span>
                <span
                  style={{
                    padding: "1px 7px",
                    borderRadius: 100,
                    background: "var(--tf-bg4)",
                    color: "var(--tf-text3)",
                    fontSize: 10,
                    fontFamily: "var(--tf-font-mono)",
                  }}
                >
                  {columnCards.length}
                </span>
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
