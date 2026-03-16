"use client";

import { CheckCircle2, ExternalLink } from "lucide-react";
import Link from "next/link";
import type { RetroActionItemDto } from "@/lib/api/types";

interface RetroActionItemListProps {
  items: RetroActionItemDto[];
  projectId: string;
}

export function RetroActionItemList({
  items,
  projectId,
}: RetroActionItemListProps) {
  if (items.length === 0) return null;

  return (
    <div>
      <h4
        style={{
          fontFamily: "var(--tf-font-head)",
          fontWeight: 600,
          fontSize: 13,
          color: "var(--tf-text)",
          margin: "0 0 10px 0",
          display: "flex",
          alignItems: "center",
          gap: 6,
        }}
      >
        <CheckCircle2 size={14} style={{ color: "var(--tf-accent)" }} />
        Action Items
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
          {items.length}
        </span>
      </h4>

      <div
        style={{
          display: "flex",
          flexDirection: "column",
          gap: 6,
        }}
      >
        {items.map((item) => (
          <div
            key={item.id}
            style={{
              display: "flex",
              alignItems: "center",
              gap: 8,
              padding: "8px 10px",
              background: "var(--tf-bg4)",
              borderRadius: 6,
              border: "1px solid var(--tf-border)",
            }}
          >
            <div style={{ flex: 1, minWidth: 0 }}>
              <div
                style={{
                  fontSize: 12,
                  fontWeight: 500,
                  color: "var(--tf-text)",
                }}
              >
                {item.title}
              </div>
              {item.assigneeName && (
                <div
                  style={{
                    fontSize: 11,
                    color: "var(--tf-text3)",
                    marginTop: 2,
                  }}
                >
                  Assigned to: {item.assigneeName}
                </div>
              )}
              {item.dueDate && (
                <div
                  style={{
                    fontSize: 11,
                    color: "var(--tf-text3)",
                    marginTop: 2,
                    fontFamily: "var(--tf-font-mono)",
                  }}
                >
                  Due: {item.dueDate}
                </div>
              )}
            </div>

            {item.linkedTaskId && (
              <Link
                href={`/projects/${projectId}/work-items/${item.linkedTaskId}`}
                title="View linked task"
                style={{
                  display: "inline-flex",
                  alignItems: "center",
                  gap: 3,
                  padding: "3px 8px",
                  borderRadius: 4,
                  fontSize: 10,
                  color: "var(--tf-accent)",
                  textDecoration: "none",
                  border: "1px solid var(--tf-accent)",
                  transition: "all var(--tf-tr)",
                }}
              >
                <ExternalLink size={10} />
                Task
              </Link>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
