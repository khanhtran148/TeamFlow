"use client";

import Link from "next/link";
import { ExternalLink } from "lucide-react";
import { StatusSelect } from "./status-select";
import { AssigneePicker } from "./assignee-picker";
import type { WorkItemDto } from "@/lib/api/types";

interface WorkItemSidebarProps {
  workItem: WorkItemDto;
  projectId: string;
}

function SidebarSection({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
  return (
    <div
      style={{
        paddingBottom: 14,
        borderBottom: "1px solid var(--tf-border)",
        marginBottom: 14,
      }}
    >
      <div
        style={{
          fontSize: 10,
          fontWeight: 600,
          color: "var(--tf-text3)",
          fontFamily: "var(--tf-font-body)",
          textTransform: "uppercase",
          letterSpacing: "0.06em",
          marginBottom: 6,
        }}
      >
        {label}
      </div>
      {children}
    </div>
  );
}

function DateRow({ label, value }: { label: string; value: string }) {
  const formatted = new Date(value).toLocaleDateString("en-AU", {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
  return (
    <div
      style={{
        display: "flex",
        justifyContent: "space-between",
        fontSize: 11,
        color: "var(--tf-text2)",
        fontFamily: "var(--tf-font-body)",
        marginBottom: 4,
      }}
    >
      <span style={{ color: "var(--tf-text3)" }}>{label}</span>
      <span>{formatted}</span>
    </div>
  );
}

export function WorkItemSidebar({ workItem, projectId }: WorkItemSidebarProps) {
  return (
    <div
      style={{
        display: "flex",
        flexDirection: "column",
        fontSize: 13,
        fontFamily: "var(--tf-font-body)",
      }}
    >
      {/* Status */}
      <SidebarSection label="Status">
        <StatusSelect
          workItemId={workItem.id}
          projectId={projectId}
          currentStatus={workItem.status}
        />
      </SidebarSection>

      {/* Assignee — hidden for Epics (cannot have assignee per domain rules) */}
      {workItem.type !== "Epic" && (
        <SidebarSection label="Assignee">
          <AssigneePicker
            workItemId={workItem.id}
            projectId={projectId}
            assigneeId={workItem.assigneeId}
            assigneeName={workItem.assigneeName}
          />
        </SidebarSection>
      )}

      {/* Release */}
      {workItem.releaseId && (
        <SidebarSection label="Release">
          <div
            style={{
              display: "inline-flex",
              alignItems: "center",
              padding: "2px 10px",
              borderRadius: 100,
              fontSize: 11,
              fontWeight: 600,
              fontFamily: "var(--tf-font-mono)",
              background: "var(--tf-violet-dim)",
              color: "var(--tf-violet)",
              border: "1px solid var(--tf-violet-dim)",
            }}
          >
            {workItem.releaseId.slice(0, 8)}
          </div>
        </SidebarSection>
      )}

      {/* Parent */}
      {workItem.parentId && (
        <SidebarSection label="Parent">
          <Link
            href={`/projects/${projectId}/work-items/${workItem.parentId}`}
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 4,
              fontSize: 12,
              color: "var(--tf-blue)",
              textDecoration: "none",
              fontFamily: "var(--tf-font-body)",
            }}
          >
            #{workItem.parentId.slice(0, 8)}
            <ExternalLink size={11} />
          </Link>
        </SidebarSection>
      )}

      {/* Metadata */}
      <div>
        <div
          style={{
            fontSize: 10,
            fontWeight: 600,
            color: "var(--tf-text3)",
            fontFamily: "var(--tf-font-body)",
            textTransform: "uppercase",
            letterSpacing: "0.06em",
            marginBottom: 8,
          }}
        >
          Details
        </div>
        <DateRow label="Created" value={workItem.createdAt} />
        <DateRow label="Updated" value={workItem.updatedAt} />
        {workItem.childCount > 0 && (
          <div
            style={{
              display: "flex",
              justifyContent: "space-between",
              fontSize: 11,
              color: "var(--tf-text2)",
              fontFamily: "var(--tf-font-body)",
              marginTop: 4,
            }}
          >
            <span style={{ color: "var(--tf-text3)" }}>Children</span>
            <span>{workItem.childCount}</span>
          </div>
        )}
        {workItem.linkCount > 0 && (
          <div
            style={{
              display: "flex",
              justifyContent: "space-between",
              fontSize: 11,
              color: "var(--tf-text2)",
              fontFamily: "var(--tf-font-body)",
              marginTop: 4,
            }}
          >
            <span style={{ color: "var(--tf-text3)" }}>Links</span>
            <span>{workItem.linkCount}</span>
          </div>
        )}
      </div>
    </div>
  );
}
