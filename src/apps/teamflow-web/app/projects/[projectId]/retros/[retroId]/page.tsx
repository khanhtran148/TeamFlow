"use client";

import { use } from "react";
import { useRouter } from "next/navigation";
import { ArrowLeft } from "lucide-react";
import { RetroBoard } from "@/components/retros/retro-board";
import { LoadingSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { useRetroSession } from "@/lib/hooks/use-retros";

interface RetroDetailPageProps {
  params: Promise<{ projectId: string; retroId: string }>;
}

export default function RetroDetailPage({ params }: RetroDetailPageProps) {
  const { projectId, retroId } = use(params);
  const router = useRouter();

  const { data: session, isLoading, isError } = useRetroSession(retroId);

  if (isLoading) {
    return (
      <div style={{ padding: 20 }}>
        <LoadingSkeleton rows={6} type="list-row" />
      </div>
    );
  }

  if (isError || !session) {
    return (
      <div
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          padding: 64,
        }}
      >
        <EmptyState
          title="Retro session not found"
          description="This session may have been deleted."
          action={
            <button
              onClick={() => router.push(`/projects/${projectId}/retros`)}
              style={{
                fontSize: 13,
                color: "var(--tf-accent)",
                textDecoration: "none",
                background: "none",
                border: "none",
                cursor: "pointer",
                fontFamily: "var(--tf-font-body)",
              }}
            >
              Back to Retrospectives
            </button>
          }
        />
      </div>
    );
  }

  return (
    <div style={{ padding: 20, display: "flex", flexDirection: "column", gap: 16 }}>
      {/* Back navigation */}
      <button
        onClick={() => router.push(`/projects/${projectId}/retros`)}
        style={{
          display: "inline-flex",
          alignItems: "center",
          gap: 5,
          background: "transparent",
          border: "none",
          cursor: "pointer",
          color: "var(--tf-text3)",
          fontSize: 13,
          fontFamily: "var(--tf-font-body)",
          padding: 0,
          transition: "color var(--tf-tr)",
        }}
        onMouseEnter={(e) => {
          (e.currentTarget as HTMLButtonElement).style.color = "var(--tf-text)";
        }}
        onMouseLeave={(e) => {
          (e.currentTarget as HTMLButtonElement).style.color =
            "var(--tf-text3)";
        }}
      >
        <ArrowLeft size={13} />
        Back to Retrospectives
      </button>

      <RetroBoard session={session} projectId={projectId} />
    </div>
  );
}
