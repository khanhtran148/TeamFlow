"use client";

// ============================================================
// SignalR context + lifecycle provider
//
// PHASE 1 KNOWN ISSUE:
// TeamFlowHub has [Authorize] on the backend. Anonymous connections will
// be rejected until that attribute is removed (or [AllowAnonymous] added)
// for Phase 1. The connection will fail gracefully — the app continues to
// work via polling/manual refresh. The hub error is logged to the console.
// Backend fix required: remove [Authorize] from TeamFlowHub.cs.
// This is a one-line change — flag for human review before backend deploy.
// ============================================================

import {
  createContext,
  useContext,
  useEffect,
  useRef,
  type ReactNode,
} from "react";
import { HubConnectionState } from "@microsoft/signalr";
import type { HubConnection } from "@microsoft/signalr";
import { useQueryClient } from "@tanstack/react-query";
import { createHubConnection } from "./connection";
import { registerEventHandlers } from "./event-handlers";
import { registerToastNotifications } from "./toast-notifications";

// ---- Context ----

interface SignalRContextValue {
  /** Join a project group to receive all project-scoped events. */
  joinProject: (projectId: string) => Promise<void>;
  /** Leave a project group. */
  leaveProject: (projectId: string) => Promise<void>;
  /** Join a work item group for detail-page events. */
  joinWorkItem: (workItemId: string) => Promise<void>;
  /** Leave a work item group. */
  leaveWorkItem: (workItemId: string) => Promise<void>;
  /** Current connection state (for debugging/status display). */
  connectionState: HubConnectionState | null;
}

const SignalRContext = createContext<SignalRContextValue | null>(null);

// ---- Provider ----

interface SignalRProviderProps {
  children: ReactNode;
}

export function SignalRProvider({ children }: SignalRProviderProps) {
  const queryClient = useQueryClient();
  const connectionRef = useRef<HubConnection | null>(null);
  const cleanupEventHandlersRef = useRef<(() => void) | null>(null);
  const cleanupToastsRef = useRef<(() => void) | null>(null);

  // Track connection state for the context value
  const connectionStateRef = useRef<HubConnectionState | null>(null);

  useEffect(() => {
    const connection = createHubConnection();
    connectionRef.current = connection;

    // Track state changes for the context
    connection.onreconnecting(() => {
      connectionStateRef.current = HubConnectionState.Reconnecting;
    });
    connection.onreconnected(() => {
      connectionStateRef.current = HubConnectionState.Connected;
    });
    connection.onclose(() => {
      connectionStateRef.current = HubConnectionState.Disconnected;
    });

    async function start() {
      try {
        await connection.start();
        connectionStateRef.current = HubConnectionState.Connected;

        // Register event → query invalidation
        cleanupEventHandlersRef.current = registerEventHandlers(connection, queryClient);
        // Register toast notifications for remote changes
        cleanupToastsRef.current = registerToastNotifications(connection, queryClient);

        if (process.env.NODE_ENV === "development") {
          console.debug("[SignalR] Connected:", connection.connectionId);
        }
      } catch (err) {
        connectionStateRef.current = HubConnectionState.Disconnected;
        // Phase 1: Hub has [Authorize] — connection will fail until backend removes it.
        // Log as warning, not error, so it doesn't alarm developers.
        console.warn(
          "[SignalR] Connection failed. If the backend TeamFlowHub still has [Authorize], " +
            "anonymous connections are rejected. Remove [Authorize] from TeamFlowHub.cs for Phase 1.",
          err,
        );
      }
    }

    void start();

    return () => {
      // Remove event listeners before stopping
      cleanupEventHandlersRef.current?.();
      cleanupToastsRef.current?.();
      cleanupEventHandlersRef.current = null;
      cleanupToastsRef.current = null;

      connection
        .stop()
        .catch((err) =>
          console.warn("[SignalR] Error stopping connection:", err),
        );
      connectionRef.current = null;
    };
  }, [queryClient]);

  // ---- Group join/leave helpers ----

  async function joinProject(projectId: string): Promise<void> {
    const conn = connectionRef.current;
    if (!conn || conn.state !== HubConnectionState.Connected) return;
    try {
      await conn.invoke("JoinProject", projectId);
    } catch (err) {
      console.warn("[SignalR] Failed to join project group:", projectId, err);
    }
  }

  async function leaveProject(projectId: string): Promise<void> {
    const conn = connectionRef.current;
    if (!conn || conn.state !== HubConnectionState.Connected) return;
    try {
      await conn.invoke("LeaveProject", projectId);
    } catch (err) {
      console.warn("[SignalR] Failed to leave project group:", projectId, err);
    }
  }

  async function joinWorkItem(workItemId: string): Promise<void> {
    const conn = connectionRef.current;
    if (!conn || conn.state !== HubConnectionState.Connected) return;
    try {
      await conn.invoke("JoinWorkItem", workItemId);
    } catch (err) {
      console.warn("[SignalR] Failed to join work item group:", workItemId, err);
    }
  }

  async function leaveWorkItem(workItemId: string): Promise<void> {
    const conn = connectionRef.current;
    if (!conn || conn.state !== HubConnectionState.Connected) return;
    try {
      await conn.invoke("LeaveWorkItem", workItemId);
    } catch (err) {
      console.warn("[SignalR] Failed to leave work item group:", workItemId, err);
    }
  }

  const value: SignalRContextValue = {
    joinProject,
    leaveProject,
    joinWorkItem,
    leaveWorkItem,
    connectionState: connectionStateRef.current,
  };

  return (
    <SignalRContext.Provider value={value}>{children}</SignalRContext.Provider>
  );
}

// ---- Hooks ----

/**
 * Returns SignalR context. Must be used inside SignalRProvider.
 */
export function useSignalR(): SignalRContextValue {
  const ctx = useContext(SignalRContext);
  if (!ctx) {
    throw new Error("useSignalR must be used inside <SignalRProvider>");
  }
  return ctx;
}

/**
 * Joins a project group on mount and leaves on unmount.
 * Safe to call when projectId is undefined — no-op in that case.
 */
export function useProjectGroup(projectId: string | undefined): void {
  const { joinProject, leaveProject } = useSignalR();

  useEffect(() => {
    if (!projectId) return;
    void joinProject(projectId);
    return () => {
      void leaveProject(projectId);
    };
  }, [projectId, joinProject, leaveProject]);
}

/**
 * Joins a work item group on mount and leaves on unmount.
 * Use this on the work item detail page.
 */
export function useWorkItemGroup(workItemId: string | undefined): void {
  const { joinWorkItem, leaveWorkItem } = useSignalR();

  useEffect(() => {
    if (!workItemId) return;
    void joinWorkItem(workItemId);
    return () => {
      void leaveWorkItem(workItemId);
    };
  }, [workItemId, joinWorkItem, leaveWorkItem]);
}
