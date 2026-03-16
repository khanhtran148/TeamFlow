"use client";

import { NotificationList } from "@/components/notifications/notification-list";
import { NotificationPreferences } from "@/components/notifications/notification-preferences";
import { useState } from "react";

export default function NotificationsPage() {
  const [tab, setTab] = useState<"inbox" | "preferences">("inbox");

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-4">
        <h1 className="text-2xl font-semibold">Notifications</h1>
        <div className="flex gap-2">
          <button
            onClick={() => setTab("inbox")}
            className={`px-3 py-1 rounded text-sm ${tab === "inbox" ? "bg-blue-600 text-white" : "bg-gray-200"}`}
          >
            Inbox
          </button>
          <button
            onClick={() => setTab("preferences")}
            className={`px-3 py-1 rounded text-sm ${tab === "preferences" ? "bg-blue-600 text-white" : "bg-gray-200"}`}
          >
            Preferences
          </button>
        </div>
      </div>
      {tab === "inbox" ? <NotificationList /> : <NotificationPreferences />}
    </div>
  );
}
