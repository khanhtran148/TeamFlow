// ============================================================
// SignalR toast notifications for remote changes
//
// Strategy: distinguish local mutations (no toast) from remote events (show toast).
// Local mutations call markLocalMutation(eventName, workItemId) before firing.
// The SignalR handler checks this set; if present it's local — skip the toast.
//
// NOTE: The "local" suppression is best-effort. It handles the common case of a
// single tab. In multi-tab scenarios each tab correctly toasts changes from the other.
// ============================================================

import { toast } from "sonner";
import type {
  WorkItemEventPayload,
  WorkItemLinkEventPayload,
  ReleaseEventPayload,
  HubEventName,
} from "./event-handlers";
import { HubEvents } from "./event-handlers";
import type { HubConnection } from "@microsoft/signalr";
import type { QueryClient } from "@tanstack/react-query";

// ---- Local mutation tracking ----

type LocalMutationKey = string; // `${eventName}:${entityId}`

const localMutations = new Set<LocalMutationKey>();

/**
 * Call this BEFORE triggering a local mutation so the corresponding SignalR
 * echo is suppressed. The entry expires after 5 seconds.
 */
export function markLocalMutation(event: HubEventName, entityId: string): void {
  const key: LocalMutationKey = `${event}:${entityId}`;
  localMutations.add(key);
  setTimeout(() => localMutations.delete(key), 5000);
}

function isLocalMutation(event: HubEventName, entityId: string): boolean {
  return localMutations.has(`${event}:${entityId}`);
}

// ---- Toast message builders ----

function workItemToastMessage(event: HubEventName, payload: WorkItemEventPayload): string {
  const id = payload.workItemId.slice(0, 8);
  switch (event) {
    case HubEvents.WorkItemCreated:
      return `Work item created`;
    case HubEvents.WorkItemUpdated:
      return `Work item ${id} was updated`;
    case HubEvents.WorkItemDeleted:
      return `Work item ${id} was deleted`;
    case HubEvents.WorkItemStatusChanged:
      return `Work item ${id} status changed`;
    case HubEvents.WorkItemAssigned:
      return `Work item ${id} was assigned`;
    case HubEvents.WorkItemUnassigned:
      return `Work item ${id} was unassigned`;
    case HubEvents.WorkItemMoved:
      return `Work item ${id} was moved`;
    case HubEvents.WorkItemReordered:
      return `Backlog reordered`;
    default:
      return `Work item updated`;
  }
}

function linkToastMessage(event: HubEventName): string {
  switch (event) {
    case HubEvents.WorkItemLinkAdded:
      return "A work item link was added";
    case HubEvents.WorkItemLinkRemoved:
      return "A work item link was removed";
    default:
      return "Work item link changed";
  }
}

function releaseToastMessage(event: HubEventName): string {
  switch (event) {
    case HubEvents.ReleaseCreated:
      return "A release was created";
    case HubEvents.ReleaseUpdated:
      return "A release was updated";
    case HubEvents.ReleaseDeleted:
      return "A release was deleted";
    case HubEvents.ReleaseItemAssigned:
      return "A work item was added to a release";
    case HubEvents.ReleaseItemUnassigned:
      return "A work item was removed from a release";
    default:
      return "Release updated";
  }
}

// ---- Registration ----

/**
 * Registers toast notification listeners on top of the query-invalidation handlers.
 * Toasts are shown only for REMOTE changes (not triggered by the local user).
 * Returns a cleanup function.
 */
export function registerToastNotifications(
  connection: HubConnection,
  _queryClient: QueryClient,
): () => void {
  const workItemEvents: HubEventName[] = [
    HubEvents.WorkItemCreated,
    HubEvents.WorkItemUpdated,
    HubEvents.WorkItemDeleted,
    HubEvents.WorkItemStatusChanged,
    HubEvents.WorkItemAssigned,
    HubEvents.WorkItemUnassigned,
    HubEvents.WorkItemMoved,
    HubEvents.WorkItemReordered,
  ];

  const linkEvents: HubEventName[] = [
    HubEvents.WorkItemLinkAdded,
    HubEvents.WorkItemLinkRemoved,
  ];

  const releaseEvents: HubEventName[] = [
    HubEvents.ReleaseCreated,
    HubEvents.ReleaseUpdated,
    HubEvents.ReleaseDeleted,
    HubEvents.ReleaseItemAssigned,
    HubEvents.ReleaseItemUnassigned,
  ];

  const workItemHandlers = workItemEvents.map((event) => {
    const handler = (payload: WorkItemEventPayload) => {
      if (!isLocalMutation(event, payload.workItemId)) {
        toast(workItemToastMessage(event, payload), {
          duration: 3000,
          // Use a subtle description to distinguish from user-initiated toasts
          description: "Remote change",
        });
      }
    };
    connection.on(event, handler);
    return { event, handler };
  });

  const linkHandlers = linkEvents.map((event) => {
    const handler = (payload: WorkItemLinkEventPayload) => {
      if (!isLocalMutation(event, payload.workItemId)) {
        toast(linkToastMessage(event), {
          duration: 3000,
          description: "Remote change",
        });
      }
    };
    connection.on(event, handler);
    return { event, handler };
  });

  const releaseHandlers = releaseEvents.map((event) => {
    const handler = (payload: ReleaseEventPayload) => {
      if (!isLocalMutation(event, payload.releaseId)) {
        toast(releaseToastMessage(event), {
          duration: 3000,
          description: "Remote change",
        });
      }
    };
    connection.on(event, handler);
    return { event, handler };
  });

  return () => {
    workItemHandlers.forEach(({ event, handler }) => connection.off(event, handler));
    linkHandlers.forEach(({ event, handler }) => connection.off(event, handler));
    releaseHandlers.forEach(({ event, handler }) => connection.off(event, handler));
  };
}
