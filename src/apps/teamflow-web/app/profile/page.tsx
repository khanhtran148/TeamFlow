"use client";

import { useState } from "react";
import { AuthGuard } from "@/components/auth/auth-guard";
import { TopBar } from "@/components/layout/top-bar";
import { ProfileDetails } from "@/components/profile/profile-details";
import { ProfileSecurity } from "@/components/profile/profile-security";
import { ProfileNotifications } from "@/components/profile/profile-notifications";
import { ProfileActivity } from "@/components/profile/profile-activity";

type Tab = "details" | "security" | "notifications" | "activity";

const TABS: { id: Tab; label: string }[] = [
  { id: "details", label: "Details" },
  { id: "security", label: "Security" },
  { id: "notifications", label: "Notifications" },
  { id: "activity", label: "Activity" },
];

function ProfilePageContent() {
  const [activeTab, setActiveTab] = useState<Tab>("details");

  return (
    <div
      style={{
        minHeight: "100vh",
        background: "var(--tf-bg)",
        display: "flex",
        flexDirection: "column",
      }}
    >
      <TopBar />
      <div style={{ padding: "32px 24px", flex: 1 }}>
      <div style={{ maxWidth: 760, margin: "0 auto" }}>
        {/* Page header */}
        <h1
          style={{
            fontFamily: "var(--tf-font-head)",
            fontWeight: 700,
            fontSize: 22,
            color: "var(--tf-text)",
            marginBottom: 24,
          }}
        >
          Profile
        </h1>

        {/* Tab bar */}
        <nav
          role="tablist"
          aria-label="Profile sections"
          style={{
            display: "flex",
            gap: 2,
            borderBottom: "1px solid var(--tf-border)",
            marginBottom: 28,
          }}
        >
          {TABS.map((tab) => {
            const isActive = activeTab === tab.id;
            return (
              <button
                key={tab.id}
                role="tab"
                aria-selected={isActive}
                aria-controls={`profile-panel-${tab.id}`}
                id={`profile-tab-${tab.id}`}
                onClick={() => setActiveTab(tab.id)}
                style={{
                  padding: "8px 16px",
                  border: "none",
                  borderBottom: isActive
                    ? "2px solid var(--tf-accent)"
                    : "2px solid transparent",
                  background: "transparent",
                  fontSize: 13,
                  fontWeight: isActive ? 600 : 400,
                  color: isActive ? "var(--tf-accent)" : "var(--tf-text2)",
                  cursor: "pointer",
                  marginBottom: -1,
                  borderRadius: "4px 4px 0 0",
                  minHeight: 44,
                  transition: "color 0.15s",
                }}
              >
                {tab.label}
              </button>
            );
          })}
        </nav>

        {/* Tab panels */}
        <div
          role="tabpanel"
          id={`profile-panel-${activeTab}`}
          aria-labelledby={`profile-tab-${activeTab}`}
        >
          {activeTab === "details" && <ProfileDetails />}
          {activeTab === "security" && <ProfileSecurity />}
          {activeTab === "notifications" && <ProfileNotifications />}
          {activeTab === "activity" && <ProfileActivity />}
        </div>
      </div>
      </div>
    </div>
  );
}

export default function ProfilePage() {
  return (
    <AuthGuard>
      <ProfilePageContent />
    </AuthGuard>
  );
}
