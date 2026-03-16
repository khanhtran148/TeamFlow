"use client";

import { useState } from "react";
import { useNotifications, useMarkAllAsRead } from "@/lib/hooks/use-notifications";
import { NotificationItem } from "./notification-item";

export function NotificationList() {
  const [isReadFilter, setIsReadFilter] = useState<boolean | undefined>(undefined);
  const [page, setPage] = useState(1);
  const { data, isLoading } = useNotifications({ isRead: isReadFilter, page, pageSize: 20 });
  const markAllAsRead = useMarkAllAsRead();

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2">
        <select
          value={isReadFilter === undefined ? "all" : String(isReadFilter)}
          onChange={(e) => {
            const v = e.target.value;
            setIsReadFilter(v === "all" ? undefined : v === "true");
            setPage(1);
          }}
          className="text-sm border rounded px-2 py-1"
        >
          <option value="all">All</option>
          <option value="false">Unread</option>
          <option value="true">Read</option>
        </select>
        <button
          onClick={() => markAllAsRead.mutate()}
          className="text-sm text-blue-600 hover:underline"
        >
          Mark all as read
        </button>
      </div>

      {isLoading && <p className="text-gray-500">Loading...</p>}

      {data?.items.map((n) => (
        <NotificationItem key={n.id} notification={n} />
      ))}

      {data && data.totalCount > 20 && (
        <div className="flex gap-2 justify-center mt-4">
          <button
            disabled={page <= 1}
            onClick={() => setPage(page - 1)}
            className="px-3 py-1 text-sm border rounded disabled:opacity-50"
          >
            Previous
          </button>
          <span className="px-3 py-1 text-sm">Page {page}</span>
          <button
            disabled={page * 20 >= data.totalCount}
            onClick={() => setPage(page + 1)}
            className="px-3 py-1 text-sm border rounded disabled:opacity-50"
          >
            Next
          </button>
        </div>
      )}
    </div>
  );
}
