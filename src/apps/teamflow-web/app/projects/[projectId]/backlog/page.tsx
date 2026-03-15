"use client";

import { useProjectContext } from "@/lib/contexts/project-context";
import { EmptyState } from "@/components/shared/empty-state";
import { LayoutList } from "lucide-react";

export default function BacklogPage() {
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
          icon={LayoutList}
          title={`${project.name} — Backlog`}
          description="Phase C will implement the full backlog view with work items, filters, and drag-drop reorder."
        />
      </div>
    </div>
  );
}
