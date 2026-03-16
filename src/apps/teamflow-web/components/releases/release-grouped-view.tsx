"use client";

import { useState } from "react";
import { ReleaseGroupSection } from "./release-group-section";
import type { ReleaseGroupDto } from "@/lib/api/types";

type GroupTab = "epic" | "assignee" | "sprint";

interface ReleaseGroupedViewProps {
  byEpic: ReleaseGroupDto[];
  byAssignee: ReleaseGroupDto[];
  bySprint: ReleaseGroupDto[];
}

export function ReleaseGroupedView({
  byEpic,
  byAssignee,
  bySprint,
}: ReleaseGroupedViewProps) {
  const [activeTab, setActiveTab] = useState<GroupTab>("epic");

  const tabs: { key: GroupTab; label: string; data: ReleaseGroupDto[] }[] = [
    { key: "epic", label: "By Epic", data: byEpic },
    { key: "assignee", label: "By Assignee", data: byAssignee },
    { key: "sprint", label: "By Sprint", data: bySprint },
  ];

  const currentData = tabs.find((t) => t.key === activeTab)?.data ?? [];

  return (
    <div>
      {/* Tab bar */}
      <div
        style={{
          display: "flex",
          gap: 2,
          borderBottom: "1px solid var(--tf-border)",
          marginBottom: 12,
        }}
      >
        {tabs.map((tab) => {
          const isActive = activeTab === tab.key;
          return (
            <button
              key={tab.key}
              onClick={() => setActiveTab(tab.key)}
              style={{
                padding: "8px 14px",
                background: "none",
                border: "none",
                borderBottom: isActive
                  ? "2px solid var(--tf-accent)"
                  : "2px solid transparent",
                cursor: "pointer",
                fontFamily: "var(--tf-font-body)",
                fontSize: 13,
                fontWeight: isActive ? 600 : 400,
                color: isActive ? "var(--tf-accent)" : "var(--tf-text2)",
                marginBottom: -1,
                transition: "color var(--tf-tr)",
              }}
            >
              {tab.label}
              {tab.data.length > 0 && (
                <span
                  style={{
                    marginLeft: 5,
                    padding: "0 5px",
                    borderRadius: 100,
                    background: "var(--tf-bg4)",
                    color: "var(--tf-text3)",
                    fontSize: 13,
                    fontFamily: "var(--tf-font-mono)",
                  }}
                >
                  {tab.data.length}
                </span>
              )}
            </button>
          );
        })}
      </div>

      {/* Groups */}
      {currentData.length === 0 ? (
        <div
          style={{
            padding: "20px",
            textAlign: "center",
            color: "var(--tf-text3)",
            fontSize: 13,
          }}
        >
          No data for this view.
        </div>
      ) : (
        <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
          {currentData.map((group) => (
            <ReleaseGroupSection
              key={group.groupId ?? group.groupName}
              group={group}
            />
          ))}
        </div>
      )}
    </div>
  );
}
