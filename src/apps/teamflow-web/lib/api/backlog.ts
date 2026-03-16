import { apiClient } from "./client";
import type {
  PaginatedResponse,
  BacklogItemDto,
  GetBacklogParams,
  ReorderBacklogBody,
  ToggleReadyBody,
  BulkUpdatePriorityBody,
} from "./types";

export async function getBacklog(
  params: GetBacklogParams,
): Promise<PaginatedResponse<BacklogItemDto>> {
  const response = await apiClient.get<PaginatedResponse<BacklogItemDto>>(
    "/backlog",
    { params },
  );
  return response.data;
}

export async function reorderBacklog(data: ReorderBacklogBody): Promise<void> {
  await apiClient.post("/backlog/reorder", data);
}

export async function toggleReadyForSprint(
  workItemId: string,
  data: ToggleReadyBody,
): Promise<void> {
  await apiClient.post(`/workitems/${workItemId}/ready`, data);
}

export async function bulkUpdatePriority(
  data: BulkUpdatePriorityBody,
): Promise<void> {
  await apiClient.post("/backlog/bulk-priority", data);
}
