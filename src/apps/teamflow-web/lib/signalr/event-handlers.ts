// ============================================================
// SignalR event-to-TanStack Query invalidation map
//
// Event payload shapes match the domain events broadcast by the backend.
// Each handler invalidates the minimum set of query keys affected.
// ============================================================

import type { QueryClient } from "@tanstack/react-query";
import { backlogKeys } from "@/lib/hooks/use-backlog";
import { releaseKeys } from "@/lib/hooks/use-releases";
import { workItemKeys } from "@/lib/hooks/use-work-items";

// ---- Payload types (must match backend event payloads) ----

export interface WorkItemEventPayload {
  projectId: string;
  workItemId: string;
}

export interface WorkItemLinkEventPayload {
  projectId: string;
  workItemId: string;
  targetItemId?: string;
}

export interface ReleaseEventPayload {
  projectId: string;
  releaseId: string;
}

// ---- Event name constants (must match backend hub broadcasts) ----

export const HubEvents = {
  // Work item events
  WorkItemCreated: "WorkItem.Created",
  WorkItemUpdated: "WorkItem.Updated",
  WorkItemDeleted: "WorkItem.Deleted",
  WorkItemStatusChanged: "WorkItem.StatusChanged",
  WorkItemAssigned: "WorkItem.Assigned",
  WorkItemUnassigned: "WorkItem.Unassigned",
  WorkItemMoved: "WorkItem.Moved",
  WorkItemReordered: "WorkItem.Reordered",
  // Link events
  WorkItemLinkAdded: "WorkItem.LinkAdded",
  WorkItemLinkRemoved: "WorkItem.LinkRemoved",
  // Release events
  ReleaseCreated: "Release.Created",
  ReleaseUpdated: "Release.Updated",
  ReleaseDeleted: "Release.Deleted",
  ReleaseItemAssigned: "Release.ItemAssigned",
  ReleaseItemUnassigned: "Release.ItemUnassigned",
} as const;

export type HubEventName = (typeof HubEvents)[keyof typeof HubEvents];

// ---- Handler: WorkItem events (Created, Updated, Deleted, StatusChanged, Assigned, Unassigned, Moved, Reordered) ----

function handleWorkItemEvent(
  queryClient: QueryClient,
  payload: WorkItemEventPayload,
): void {
  const { projectId, workItemId } = payload;

  // Invalidate list views
  queryClient.invalidateQueries({ queryKey: backlogKeys.all(projectId) });
  queryClient.invalidateQueries({ queryKey: ["kanban", projectId] });

  // Invalidate the specific work item detail if cached
  queryClient.invalidateQueries({ queryKey: workItemKeys.detail(workItemId) });
}

// ---- Handler: Link events ----

function handleLinkEvent(
  queryClient: QueryClient,
  payload: WorkItemLinkEventPayload,
): void {
  const { projectId, workItemId, targetItemId } = payload;

  // Invalidate list views
  queryClient.invalidateQueries({ queryKey: backlogKeys.all(projectId) });
  queryClient.invalidateQueries({ queryKey: ["kanban", projectId] });

  // Invalidate the item detail and its links sub-key
  queryClient.invalidateQueries({ queryKey: workItemKeys.detail(workItemId) });
  queryClient.invalidateQueries({ queryKey: workItemKeys.links(workItemId) });
  queryClient.invalidateQueries({ queryKey: workItemKeys.blockers(workItemId) });

  // Also invalidate the target item's links if known (reverse link)
  if (targetItemId) {
    queryClient.invalidateQueries({ queryKey: workItemKeys.detail(targetItemId) });
    queryClient.invalidateQueries({ queryKey: workItemKeys.links(targetItemId) });
    queryClient.invalidateQueries({ queryKey: workItemKeys.blockers(targetItemId) });
  }
}

// ---- Handler: Release events ----

function handleReleaseEvent(
  queryClient: QueryClient,
  payload: ReleaseEventPayload,
): void {
  const { projectId, releaseId } = payload;

  queryClient.invalidateQueries({ queryKey: releaseKeys.all(projectId) });
  queryClient.invalidateQueries({ queryKey: releaseKeys.detail(releaseId) });

  // Release item changes also affect backlog (release badge updates)
  queryClient.invalidateQueries({ queryKey: backlogKeys.all(projectId) });
}

// ---- Main registration: wires all event listeners onto a hub connection ----

import type { HubConnection } from "@microsoft/signalr";

/**
 * Registers all SignalR event handlers on the given connection.
 * Call this after the connection starts.
 * Returns a cleanup function that removes all listeners.
 */
export function registerEventHandlers(
  connection: HubConnection,
  queryClient: QueryClient,
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

  // Build handler references so we can remove them on cleanup
  const workItemHandlers = workItemEvents.map((event) => {
    const handler = (payload: WorkItemEventPayload) => {
      handleWorkItemEvent(queryClient, payload);
    };
    connection.on(event, handler);
    return { event, handler };
  });

  const linkHandlers = linkEvents.map((event) => {
    const handler = (payload: WorkItemLinkEventPayload) => {
      handleLinkEvent(queryClient, payload);
    };
    connection.on(event, handler);
    return { event, handler };
  });

  const releaseHandlers = releaseEvents.map((event) => {
    const handler = (payload: ReleaseEventPayload) => {
      handleReleaseEvent(queryClient, payload);
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
