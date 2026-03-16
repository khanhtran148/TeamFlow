"use client";

import { ThumbsUp, Check } from "lucide-react";
import type { RetroCardDto } from "@/lib/api/types";

interface RetroCardProps {
  card: RetroCardDto;
  isVotingPhase: boolean;
  isDiscussingPhase: boolean;
  canVote: boolean;
  canFacilitate: boolean;
  onVote: (cardId: string, voteCount: number) => void;
  onMarkDiscussed: (cardId: string) => void;
}

const CATEGORY_COLORS: Record<string, { bg: string; border: string }> = {
  WentWell: {
    bg: "var(--tf-accent-dim2)",
    border: "var(--tf-accent)",
  },
  NeedsImprovement: {
    bg: "var(--tf-orange-dim)",
    border: "var(--tf-orange)",
  },
  ActionItem: {
    bg: "var(--tf-blue-dim)",
    border: "var(--tf-blue)",
  },
};

export function RetroCard({
  card,
  isVotingPhase,
  isDiscussingPhase,
  canVote,
  canFacilitate,
  onVote,
  onMarkDiscussed,
}: RetroCardProps) {
  const colors = CATEGORY_COLORS[card.category] ?? CATEGORY_COLORS.WentWell;

  return (
    <div
      style={{
        background: colors.bg,
        border: `1px solid ${card.isDiscussed ? "var(--tf-border)" : colors.border}`,
        borderRadius: 8,
        padding: "12px 14px",
        opacity: card.isDiscussed ? 0.6 : 1,
        transition: "all var(--tf-tr)",
      }}
    >
      {/* Content */}
      <p
        style={{
          fontSize: 13,
          color: "var(--tf-text)",
          lineHeight: 1.5,
          margin: 0,
          whiteSpace: "pre-wrap",
          wordBreak: "break-word",
        }}
      >
        {card.content}
      </p>

      {/* Meta row */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          marginTop: 10,
        }}
      >
        <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
          {/* Author (if public) */}
          {card.authorName && (
            <span
              style={{
                fontSize: 11,
                color: "var(--tf-text3)",
              }}
            >
              {card.authorName}
            </span>
          )}

          {/* Vote count */}
          {card.totalVotes > 0 && (
            <span
              style={{
                display: "inline-flex",
                alignItems: "center",
                gap: 3,
                fontSize: 11,
                fontWeight: 600,
                color: "var(--tf-text2)",
                fontFamily: "var(--tf-font-mono)",
              }}
            >
              <ThumbsUp size={10} />
              {card.totalVotes}
            </span>
          )}
        </div>

        <div style={{ display: "flex", alignItems: "center", gap: 6 }}>
          {/* Vote buttons - only during voting phase */}
          {isVotingPhase && canVote && (
            <div style={{ display: "flex", gap: 4 }}>
              <button
                onClick={() => onVote(card.id, 1)}
                title="Vote +1"
                style={{
                  display: "inline-flex",
                  alignItems: "center",
                  gap: 3,
                  padding: "3px 8px",
                  borderRadius: 5,
                  border: "1px solid var(--tf-border)",
                  background: "transparent",
                  color: "var(--tf-text2)",
                  fontSize: 11,
                  cursor: "pointer",
                  fontFamily: "var(--tf-font-body)",
                  transition: "all var(--tf-tr)",
                }}
                onMouseEnter={(e) => {
                  (e.currentTarget as HTMLButtonElement).style.background =
                    "var(--tf-bg2)";
                  (e.currentTarget as HTMLButtonElement).style.borderColor =
                    "var(--tf-accent)";
                }}
                onMouseLeave={(e) => {
                  (e.currentTarget as HTMLButtonElement).style.background =
                    "transparent";
                  (e.currentTarget as HTMLButtonElement).style.borderColor =
                    "var(--tf-border)";
                }}
              >
                <ThumbsUp size={10} />
                +1
              </button>
              <button
                onClick={() => onVote(card.id, 2)}
                title="Vote +2"
                style={{
                  display: "inline-flex",
                  alignItems: "center",
                  gap: 3,
                  padding: "3px 8px",
                  borderRadius: 5,
                  border: "1px solid var(--tf-border)",
                  background: "transparent",
                  color: "var(--tf-text2)",
                  fontSize: 11,
                  cursor: "pointer",
                  fontFamily: "var(--tf-font-body)",
                  transition: "all var(--tf-tr)",
                }}
                onMouseEnter={(e) => {
                  (e.currentTarget as HTMLButtonElement).style.background =
                    "var(--tf-bg2)";
                  (e.currentTarget as HTMLButtonElement).style.borderColor =
                    "var(--tf-accent)";
                }}
                onMouseLeave={(e) => {
                  (e.currentTarget as HTMLButtonElement).style.background =
                    "transparent";
                  (e.currentTarget as HTMLButtonElement).style.borderColor =
                    "var(--tf-border)";
                }}
              >
                <ThumbsUp size={10} />
                +2
              </button>
            </div>
          )}

          {/* Mark discussed - only during discussing phase, facilitator only */}
          {isDiscussingPhase && canFacilitate && !card.isDiscussed && (
            <button
              onClick={() => onMarkDiscussed(card.id)}
              title="Mark as discussed"
              style={{
                display: "inline-flex",
                alignItems: "center",
                gap: 3,
                padding: "3px 8px",
                borderRadius: 5,
                border: "1px solid var(--tf-accent)",
                background: "transparent",
                color: "var(--tf-accent)",
                fontSize: 11,
                cursor: "pointer",
                fontFamily: "var(--tf-font-body)",
                transition: "all var(--tf-tr)",
              }}
            >
              <Check size={10} />
              Discussed
            </button>
          )}

          {card.isDiscussed && (
            <span
              style={{
                display: "inline-flex",
                alignItems: "center",
                gap: 3,
                fontSize: 10,
                color: "var(--tf-text3)",
                fontStyle: "italic",
              }}
            >
              <Check size={10} />
              Discussed
            </span>
          )}
        </div>
      </div>
    </div>
  );
}
