"use client";

import { useProjectContext } from "@/lib/contexts/project-context";
import { EmptyState } from "@/components/shared/empty-state";
import { Kanban } from "lucide-react";

export default function BoardPage() {
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
          icon={Kanban}
          title={`${project.name} — Board`}
          description="Phase D will implement the full Kanban board with drag-drop columns."
        />
      </div>
    </div>
  );
}
