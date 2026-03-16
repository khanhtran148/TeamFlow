"use client";

import type { InAppNotificationDto } from "@/lib/api/types";
import { useMarkAsRead } from "@/lib/hooks/use-notifications";

export function NotificationItem({ notification }: { notification: InAppNotificationDto }) {
  const markAsRead = useMarkAsRead();

  return (
    <div
      className={`p-3 border rounded ${notification.isRead ? "bg-white" : "bg-blue-50 border-blue-200"}`}
    >
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <div className="flex items-center gap-2">
            <span className="text-xs px-1.5 py-0.5 bg-gray-100 rounded text-gray-600">
              {notification.type}
            </span>
            <span className="text-xs text-gray-400">
              {new Date(notification.createdAt).toLocaleString()}
            </span>
          </div>
          <p className="font-medium text-sm mt-1">{notification.title}</p>
          {notification.body && (
            <p className="text-sm text-gray-600 mt-0.5">{notification.body}</p>
          )}
        </div>
        {!notification.isRead && (
          <button
            onClick={() => markAsRead.mutate(notification.id)}
            className="text-xs text-blue-600 hover:underline ml-2 shrink-0"
          >
            Mark read
          </button>
        )}
      </div>
    </div>
  );
}
