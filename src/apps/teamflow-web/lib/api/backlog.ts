import { apiClient } from "./client";
import type {
  PaginatedResponse,
  BacklogItemDto,
  GetBacklogParams,
  ReorderBacklogBody,
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
