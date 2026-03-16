"use client";

import { useUnreadCount } from "@/lib/hooks/use-notifications";
import { useNotificationStore } from "@/lib/stores/notification-store";
import { useEffect } from "react";

export function NotificationBell({ onClick }: { onClick?: () => void }) {
  const { data } = useUnreadCount();
  const { unreadCount, setUnreadCount } = useNotificationStore();

  useEffect(() => {
    if (data) setUnreadCount(data.count);
  }, [data, setUnreadCount]);

  return (
    <button onClick={onClick} className="relative p-2" aria-label="Notifications">
      <svg className="w-6 h-6 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path
          strokeLinecap="round"
          strokeLinejoin="round"
          strokeWidth={2}
          d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
        />
      </svg>
      {unreadCount > 0 && (
        <span className="absolute top-0 right-0 flex h-5 w-5 items-center justify-center rounded-full bg-red-500 text-[10px] text-white font-bold">
          {unreadCount > 99 ? "99+" : unreadCount}
        </span>
      )}
    </button>
  );
}
