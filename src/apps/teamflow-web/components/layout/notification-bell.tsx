"use client";

import { useState } from "react";
import { Bell } from "lucide-react";
import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/lib/api/client";
import type { InAppNotificationDto } from "@/lib/api/types";

export function NotificationBell() {
  const [open, setOpen] = useState(false);

  // Fetch unread notifications count
  const { data: notifications } = useQuery({
    queryKey: ["notifications"],
    queryFn: async () => {
      try {
        const { data } = await apiClient.get<InAppNotificationDto[]>(
          "/notifications",
          { params: { isRead: false, pageSize: 10 } },
        );
        return data;
      } catch {
        // Notifications endpoint may not exist yet
        return [];
      }
    },
    staleTime: 30_000,
    refetchInterval: 30_000,
  });

  const unreadCount = notifications?.length ?? 0;

  return (
    <div style={{ position: "relative" }}>
      <button
        onClick={() => setOpen(!open)}
        data-testid="notification-bell"
        title={`${unreadCount} unread notifications`}
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          width: 32,
          height: 32,
          borderRadius: 6,
          border: "1px solid var(--tf-border)",
          background: "transparent",
          cursor: "pointer",
          color: "var(--tf-text3)",
          position: "relative",
          transition: "all var(--tf-tr)",
        }}
        onMouseEnter={(e) => {
          (e.currentTarget as HTMLButtonElement).style.background =
            "var(--tf-bg3)";
          (e.currentTarget as HTMLButtonElement).style.color = "var(--tf-text)";
        }}
        onMouseLeave={(e) => {
          (e.currentTarget as HTMLButtonElement).style.background =
            "transparent";
          (e.currentTarget as HTMLButtonElement).style.color =
            "var(--tf-text3)";
        }}
      >
        <Bell size={14} />
        {unreadCount > 0 && (
          <span
            style={{
              position: "absolute",
              top: -3,
              right: -3,
              width: 16,
              height: 16,
              borderRadius: "50%",
              background: "var(--tf-red)",
              color: "#fff",
              fontSize: 9,
              fontWeight: 700,
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              fontFamily: "var(--tf-font-mono)",
            }}
          >
            {unreadCount > 9 ? "9+" : unreadCount}
          </span>
        )}
      </button>

      {/* Dropdown */}
      {open && (
        <>
          <div
            style={{
              position: "fixed",
              inset: 0,
              zIndex: 49,
            }}
            onClick={() => setOpen(false)}
          />
          <div
            style={{
              position: "absolute",
              top: "calc(100% + 6px)",
              right: 0,
              width: 320,
              maxHeight: 400,
              overflow: "auto",
              background: "var(--tf-bg2)",
              border: "1px solid var(--tf-border)",
              borderRadius: 8,
              boxShadow: "0 4px 16px rgba(0,0,0,0.15)",
              zIndex: 50,
            }}
          >
            <div
              style={{
                padding: "10px 14px",
                borderBottom: "1px solid var(--tf-border)",
                fontFamily: "var(--tf-font-head)",
                fontWeight: 600,
                fontSize: 13,
                color: "var(--tf-text)",
              }}
            >
              Notifications
            </div>

            {(!notifications || notifications.length === 0) ? (
              <div
                style={{
                  padding: "24px 14px",
                  textAlign: "center",
                  color: "var(--tf-text3)",
                  fontSize: 12,
                }}
              >
                No new notifications.
              </div>
            ) : (
              notifications.map((n) => (
                <div
                  key={n.id}
                  style={{
                    padding: "10px 14px",
                    borderBottom: "1px solid var(--tf-border)",
                    fontSize: 12,
                    transition: "background var(--tf-tr)",
                  }}
                  onMouseEnter={(e) => {
                    (e.currentTarget as HTMLDivElement).style.background =
                      "var(--tf-bg3)";
                  }}
                  onMouseLeave={(e) => {
                    (e.currentTarget as HTMLDivElement).style.background =
                      "transparent";
                  }}
                >
                  <div
                    style={{
                      fontWeight: 600,
                      color: "var(--tf-text)",
                      marginBottom: 2,
                    }}
                  >
                    {n.title}
                  </div>
                  {n.body && (
                    <div style={{ color: "var(--tf-text3)", fontSize: 11 }}>
                      {n.body}
                    </div>
                  )}
                  <div
                    style={{
                      color: "var(--tf-text3)",
                      fontSize: 10,
                      marginTop: 4,
                      fontFamily: "var(--tf-font-mono)",
                    }}
                  >
                    {new Date(n.createdAt).toLocaleTimeString()}
                  </div>
                </div>
              ))
            )}
          </div>
        </>
      )}
    </div>
  );
}
