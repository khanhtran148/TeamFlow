"use client";

import { BarChart3 } from "lucide-react";
import type { RetroSessionDto } from "@/lib/api/types";

interface RetroSummaryProps {
  session: RetroSessionDto;
}

export function RetroSummary({ session }: RetroSummaryProps) {
  if (session.status !== "Closed") return null;

  const wentWell = session.cards.filter(
    (c) => c.category === "WentWell",
  ).length;
  const needsImprovement = session.cards.filter(
    (c) => c.category === "NeedsImprovement",
  ).length;
  const actionCards = session.cards.filter(
    (c) => c.category === "ActionItem",
  ).length;

  const topVoted = [...session.cards]
    .sort((a, b) => b.totalVotes - a.totalVotes)
    .slice(0, 3);

  return (
    <div
      style={{
        background: "var(--tf-bg2)",
        border: "1px solid var(--tf-border)",
        borderRadius: 8,
        padding: 20,
      }}
    >
      <h3
        style={{
          fontFamily: "var(--tf-font-head)",
          fontWeight: 700,
          fontSize: 16,
          color: "var(--tf-text)",
          margin: "0 0 16px 0",
          display: "flex",
          alignItems: "center",
          gap: 8,
        }}
      >
        <BarChart3 size={16} style={{ color: "var(--tf-accent)" }} />
        Session Summary
      </h3>

      {/* Card counts */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "repeat(3, 1fr)",
          gap: 12,
          marginBottom: 20,
        }}
      >
        <div
          style={{
            background: "var(--tf-accent-dim2)",
            borderRadius: 8,
            padding: 12,
            textAlign: "center",
          }}
        >
          <div
            style={{
              fontSize: 22,
              fontWeight: 700,
              color: "var(--tf-accent)",
              fontFamily: "var(--tf-font-mono)",
            }}
          >
            {wentWell}
          </div>
          <div
            style={{
              fontSize: 11,
              color: "var(--tf-text3)",
              marginTop: 2,
            }}
          >
            Went Well
          </div>
        </div>
        <div
          style={{
            background: "var(--tf-orange-dim)",
            borderRadius: 8,
            padding: 12,
            textAlign: "center",
          }}
        >
          <div
            style={{
              fontSize: 22,
              fontWeight: 700,
              color: "var(--tf-orange)",
              fontFamily: "var(--tf-font-mono)",
            }}
          >
            {needsImprovement}
          </div>
          <div
            style={{
              fontSize: 11,
              color: "var(--tf-text3)",
              marginTop: 2,
            }}
          >
            Needs Improvement
          </div>
        </div>
        <div
          style={{
            background: "var(--tf-blue-dim)",
            borderRadius: 8,
            padding: 12,
            textAlign: "center",
          }}
        >
          <div
            style={{
              fontSize: 22,
              fontWeight: 700,
              color: "var(--tf-blue)",
              fontFamily: "var(--tf-font-mono)",
            }}
          >
            {actionCards}
          </div>
          <div
            style={{
              fontSize: 11,
              color: "var(--tf-text3)",
              marginTop: 2,
            }}
          >
            Action Cards
          </div>
        </div>
      </div>

      {/* Top voted */}
      {topVoted.length > 0 && topVoted[0].totalVotes > 0 && (
        <div>
          <h4
            style={{
              fontFamily: "var(--tf-font-head)",
              fontWeight: 600,
              fontSize: 13,
              color: "var(--tf-text2)",
              margin: "0 0 8px 0",
            }}
          >
            Top Voted
          </h4>
          <div
            style={{
              display: "flex",
              flexDirection: "column",
              gap: 6,
            }}
          >
            {topVoted
              .filter((c) => c.totalVotes > 0)
              .map((card, i) => (
                <div
                  key={card.id}
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: 8,
                    fontSize: 12,
                    color: "var(--tf-text)",
                  }}
                >
                  <span
                    style={{
                      fontWeight: 700,
                      color: "var(--tf-text3)",
                      fontFamily: "var(--tf-font-mono)",
                      width: 20,
                    }}
                  >
                    #{i + 1}
                  </span>
                  <span
                    style={{
                      flex: 1,
                      overflow: "hidden",
                      textOverflow: "ellipsis",
                      whiteSpace: "nowrap",
                    }}
                  >
                    {card.content}
                  </span>
                  <span
                    style={{
                      fontFamily: "var(--tf-font-mono)",
                      fontWeight: 600,
                      color: "var(--tf-accent)",
                      fontSize: 11,
                    }}
                  >
                    {card.totalVotes} votes
                  </span>
                </div>
              ))}
          </div>
        </div>
      )}

      {/* Action items count */}
      <div
        style={{
          marginTop: 16,
          padding: "10px 12px",
          background: "var(--tf-bg4)",
          borderRadius: 6,
          fontSize: 12,
          color: "var(--tf-text2)",
        }}
      >
        <strong>{session.actionItems.length}</strong> action item
        {session.actionItems.length !== 1 ? "s" : ""} created from this session.
      </div>
    </div>
  );
}
