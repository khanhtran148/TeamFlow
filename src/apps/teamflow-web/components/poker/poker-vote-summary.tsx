"use client";

import { Users, Eye } from "lucide-react";
import type { PokerSessionDto } from "@/lib/api/types";

interface PokerVoteSummaryProps {
  session: PokerSessionDto;
}

export function PokerVoteSummary({ session }: PokerVoteSummaryProps) {
  if (!session.isRevealed) {
    // Before reveal: show count only
    return (
      <div
        style={{
          background: "var(--tf-bg2)",
          border: "1px solid var(--tf-border)",
          borderRadius: 8,
          padding: "16px 20px",
          textAlign: "center",
        }}
      >
        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            gap: 8,
            marginBottom: 8,
          }}
        >
          <Users size={14} style={{ color: "var(--tf-text3)" }} />
          <span
            style={{
              fontSize: 13,
              color: "var(--tf-text2)",
              fontWeight: 500,
            }}
          >
            Votes
          </span>
        </div>
        <div
          style={{
            fontSize: 28,
            fontWeight: 700,
            color: "var(--tf-accent)",
            fontFamily: "var(--tf-font-mono)",
          }}
        >
          {session.voteCount}
        </div>
        <div
          style={{
            fontSize: 13,
            color: "var(--tf-text3)",
            marginTop: 4,
          }}
        >
          {session.voteCount === 0
            ? "Waiting for votes..."
            : `${session.voteCount} vote${session.voteCount !== 1 ? "s" : ""} cast`}
        </div>
      </div>
    );
  }

  // After reveal: show all votes
  const values = session.votes
    .map((v) => v.value)
    .filter((v): v is number => v !== null);
  const avg =
    values.length > 0
      ? (values.reduce((a, b) => a + b, 0) / values.length).toFixed(1)
      : "N/A";
  const min = values.length > 0 ? Math.min(...values) : 0;
  const max = values.length > 0 ? Math.max(...values) : 0;

  return (
    <div
      style={{
        background: "var(--tf-bg2)",
        border: "1px solid var(--tf-border)",
        borderRadius: 8,
        padding: "16px 20px",
      }}
    >
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 8,
          marginBottom: 12,
        }}
      >
        <Eye size={14} style={{ color: "var(--tf-accent)" }} />
        <span
          style={{
            fontSize: 13,
            fontWeight: 600,
            color: "var(--tf-text)",
          }}
        >
          Results
        </span>
      </div>

      {/* Stats */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "repeat(3, 1fr)",
          gap: 10,
          marginBottom: 14,
        }}
      >
        <div style={{ textAlign: "center" }}>
          <div
            style={{
              fontSize: 18,
              fontWeight: 700,
              color: "var(--tf-accent)",
              fontFamily: "var(--tf-font-mono)",
            }}
          >
            {avg}
          </div>
          <div style={{ fontSize: 13, color: "var(--tf-text3)" }}>
            Average
          </div>
        </div>
        <div style={{ textAlign: "center" }}>
          <div
            style={{
              fontSize: 18,
              fontWeight: 700,
              color: "var(--tf-text2)",
              fontFamily: "var(--tf-font-mono)",
            }}
          >
            {min}
          </div>
          <div style={{ fontSize: 13, color: "var(--tf-text3)" }}>Min</div>
        </div>
        <div style={{ textAlign: "center" }}>
          <div
            style={{
              fontSize: 18,
              fontWeight: 700,
              color: "var(--tf-text2)",
              fontFamily: "var(--tf-font-mono)",
            }}
          >
            {max}
          </div>
          <div style={{ fontSize: 13, color: "var(--tf-text3)" }}>Max</div>
        </div>
      </div>

      {/* Individual votes */}
      <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
        {session.votes.map((vote) => (
          <div
            key={vote.id}
            style={{
              display: "flex",
              alignItems: "center",
              justifyContent: "space-between",
              padding: "6px 8px",
              background: "var(--tf-bg4)",
              borderRadius: 5,
            }}
          >
            <span style={{ fontSize: 13, color: "var(--tf-text)" }}>
              {vote.voterName}
            </span>
            <span
              style={{
                fontSize: 14,
                fontWeight: 700,
                color: "var(--tf-accent)",
                fontFamily: "var(--tf-font-mono)",
              }}
            >
              {vote.value ?? "?"}
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}
