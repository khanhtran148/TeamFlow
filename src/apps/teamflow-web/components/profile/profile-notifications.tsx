"use client";

import { NotificationPreferences } from "@/components/notifications/notification-preferences";

export function ProfileNotifications() {
  return (
    <div style={{ maxWidth: 600 }}>
      <h3
        style={{
          fontFamily: "var(--tf-font-head)",
          fontWeight: 700,
          fontSize: 16,
          color: "var(--tf-text)",
          marginBottom: 20,
        }}
      >
        Notification Preferences
      </h3>
      <NotificationPreferences />
    </div>
  );
}
