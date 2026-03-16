"use client";

import { useState, useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { HistoryEntry } from "./history-entry";
import { LoadingSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { useWorkItemHistory } from "@/lib/hooks/use-work-item-history";
import { useSignalR } from "@/lib/signalr/signalr-provider";

interface HistoryTabProps {
  workItemId: string;
}

export function HistoryTab({ workItemId }: HistoryTabProps) {
  const [page, setPage] = useState(1);
  const pageSize = 20;
  const { data, isLoading } = useWorkItemHistory(workItemId, page, pageSize);
  const queryClient = useQueryClient();

  // Realtime: invalidate history when a history_added event arrives
  useEffect(() => {
    function handleHistoryAdded() {
      queryClient.invalidateQueries({
        queryKey: ["work-item-history", workItemId],
      });
    }

    // Listen on window for custom signalr event (dispatched by event-handlers.ts)
    window.addEventListener("signalr:history_added", handleHistoryAdded);
    return () =>
      window.removeEventListener("signalr:history_added", handleHistoryAdded);
  }, [workItemId, queryClient]);

  if (isLoading) return <LoadingSkeleton rows={5} />;

  if (!data || data.items.length === 0) {
    return <EmptyState title="No history" description="Changes will appear here" />;
  }

  return (
    <div>
      <div style={{ display: "flex", flexDirection: "column" }}>
        {data.items.map((entry) => (
          <HistoryEntry key={entry.id} entry={entry} />
        ))}
      </div>

      {data.totalCount > pageSize && (
        <div
          style={{
            display: "flex",
            justifyContent: "center",
            gap: 8,
            marginTop: 16,
          }}
        >
          <button
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page === 1}
            style={{
              padding: "6px 12px",
              borderRadius: 6,
              border: "1px solid var(--tf-border)",
              background: "transparent",
              color: page === 1 ? "var(--tf-text3)" : "var(--tf-text2)",
              cursor: page === 1 ? "default" : "pointer",
              fontSize: 13,
            }}
          >
            Previous
          </button>
          <span style={{ fontSize: 13, color: "var(--tf-text3)", lineHeight: "28px" }}>
            Page {page} of {Math.ceil(data.totalCount / pageSize)}
          </span>
          <button
            onClick={() => setPage((p) => p + 1)}
            disabled={page >= Math.ceil(data.totalCount / pageSize)}
            style={{
              padding: "6px 12px",
              borderRadius: 6,
              border: "1px solid var(--tf-border)",
              background: "transparent",
              color:
                page >= Math.ceil(data.totalCount / pageSize)
                  ? "var(--tf-text3)"
                  : "var(--tf-text2)",
              cursor:
                page >= Math.ceil(data.totalCount / pageSize)
                  ? "default"
                  : "pointer",
              fontSize: 13,
            }}
          >
            Next
          </button>
        </div>
      )}
    </div>
  );
}
