"use client";

import { useState } from "react";
import { Dices, Plus } from "lucide-react";
import { toast } from "sonner";
import { PokerCard } from "./poker-card";
import { PokerVoteSummary } from "./poker-vote-summary";
import { PokerControls } from "./poker-controls";
import { PokerResult } from "./poker-result";
import { LoadingSkeleton } from "@/components/shared/loading-skeleton";
import {
  usePokerSessionByWorkItem,
  useCreatePokerSession,
  useCastPokerVote,
  useRevealPokerVotes,
  useConfirmPokerEstimate,
} from "@/lib/hooks/use-poker";
import { useHasPermission } from "@/lib/hooks/use-permission";
import type { ApiError } from "@/lib/api/client";

const FIBONACCI_VALUES = [1, 2, 3, 5, 8, 13, 21];

interface PokerSessionProps {
  workItemId: string;
  projectId: string;
}

export function PokerSession({ workItemId, projectId }: PokerSessionProps) {
  const [selectedValue, setSelectedValue] = useState<number | null>(null);

  const canVote = useHasPermission(projectId, "Poker_Vote");
  const canFacilitate = useHasPermission(projectId, "Poker_Facilitate");
  const canConfirm = useHasPermission(projectId, "Poker_ConfirmEstimate");

  const {
    data: session,
    isLoading,
    isError,
  } = usePokerSessionByWorkItem(workItemId);
  const createMutation = useCreatePokerSession();

  const sessionId = session?.id ?? "";
  const voteMutation = useCastPokerVote(sessionId, workItemId);
  const revealMutation = useRevealPokerVotes(sessionId, workItemId);
  const confirmMutation = useConfirmPokerEstimate(sessionId, workItemId);

  async function handleCreate() {
    try {
      await createMutation.mutateAsync({ workItemId });
      toast.success("Poker session started");
    } catch (err) {
      toast.error(
        (err as ApiError).message ?? "Failed to start poker session",
      );
    }
  }

  async function handleVote(value: number) {
    try {
      setSelectedValue(value);
      await voteMutation.mutateAsync({ value });
      toast.success(`Voted: ${value}`);
    } catch (err) {
      toast.error((err as ApiError).message ?? "Failed to vote");
      setSelectedValue(null);
    }
  }

  async function handleReveal() {
    try {
      await revealMutation.mutateAsync();
      toast.success("Votes revealed");
    } catch (err) {
      toast.error((err as ApiError).message ?? "Failed to reveal votes");
    }
  }

  async function handleConfirm(value: number) {
    try {
      await confirmMutation.mutateAsync({ finalEstimate: value });
      toast.success(`Estimate confirmed: ${value}`);
    } catch (err) {
      toast.error(
        (err as ApiError).message ?? "Failed to confirm estimate",
      );
    }
  }

  if (isLoading) {
    return <LoadingSkeleton rows={2} type="list-row" />;
  }

  // No active session - show create button
  if (isError || !session) {
    return (
      <div
        style={{
          background: "var(--tf-bg2)",
          border: "1px solid var(--tf-border)",
          borderRadius: 8,
          padding: "20px",
          textAlign: "center",
        }}
      >
        <Dices
          size={24}
          style={{
            color: "var(--tf-text3)",
            marginBottom: 8,
          }}
        />
        <p
          style={{
            fontSize: 13,
            color: "var(--tf-text3)",
            margin: "0 0 12px 0",
          }}
        >
          No planning poker session for this story.
        </p>
        {canFacilitate && (
          <button
            onClick={handleCreate}
            disabled={createMutation.isPending}
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 5,
              padding: "7px 16px",
              borderRadius: 6,
              fontSize: 13,
              fontWeight: 600,
              border: "none",
              background: "var(--tf-accent)",
              color: "var(--tf-bg)",
              cursor: "pointer",
              fontFamily: "var(--tf-font-body)",
            }}
          >
            <Plus size={12} />
            Start Poker Session
          </button>
        )}
      </div>
    );
  }

  // Session exists
  const hasVoted = selectedValue !== null;
  const isCompleted = session.finalEstimate !== null;

  return (
    <div
      style={{
        background: "var(--tf-bg2)",
        border: "1px solid var(--tf-border)",
        borderRadius: 8,
        padding: "20px",
        display: "flex",
        flexDirection: "column",
        gap: 16,
      }}
    >
      {/* Header */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 8,
        }}
      >
        <Dices size={16} style={{ color: "var(--tf-accent)" }} />
        <span
          style={{
            fontFamily: "var(--tf-font-head)",
            fontWeight: 600,
            fontSize: 14,
            color: "var(--tf-text)",
          }}
        >
          Planning Poker
        </span>
        <span
          style={{
            fontSize: 13,
            color: "var(--tf-text3)",
          }}
        >
          Facilitated by {session.facilitatorName}
        </span>
      </div>

      {/* Final result */}
      {isCompleted && <PokerResult finalEstimate={session.finalEstimate!} />}

      {/* Card selection - for voters who haven't seen reveal */}
      {!isCompleted && canVote && !session.isRevealed && (
        <div>
          <div
            style={{
              fontSize: 13,
              color: "var(--tf-text3)",
              marginBottom: 8,
            }}
          >
            Select your estimate:
          </div>
          <div
            style={{
              display: "flex",
              gap: 8,
              justifyContent: "center",
              flexWrap: "wrap",
            }}
          >
            {FIBONACCI_VALUES.map((val) => (
              <PokerCard
                key={val}
                value={val}
                isSelected={selectedValue === val}
                onClick={() => handleVote(val)}
                disabled={voteMutation.isPending}
              />
            ))}
          </div>
        </div>
      )}

      {/* Vote summary */}
      <PokerVoteSummary session={session} />

      {/* Facilitator controls */}
      {!isCompleted && (
        <PokerControls
          canFacilitate={canFacilitate || canConfirm}
          isRevealed={session.isRevealed}
          finalEstimate={session.finalEstimate}
          onReveal={handleReveal}
          onConfirm={handleConfirm}
          revealPending={revealMutation.isPending}
          confirmPending={confirmMutation.isPending}
        />
      )}
    </div>
  );
}
