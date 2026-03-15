"use client";

import { useProjectContext } from "@/lib/contexts/project-context";
import { EmptyState } from "@/components/shared/empty-state";
import { Tag } from "lucide-react";

export default function ReleasesPage() {
  const { project } = useProjectContext();

  return (
    <div style={{ padding: "20px" }}>
      <div
        style={{
          background: "var(--tf-bg2)",
          border: "1px solid var(--tf-border)",
          borderRadius: "var(--tf-radius)",
        }}
      >
        <EmptyState
          icon={Tag}
          title={`${project.name} — Releases`}
          description="Phase F will implement releases with CRUD, item assignment, and progress tracking."
        />
      </div>
    </div>
  );
}
