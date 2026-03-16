import { apiClient } from "./client";
import type {
  PaginatedResponse,
  WorkItemDto,
  SavedFilterDto,
  SaveFilterBody,
  UpdateSavedFilterBody,
  SearchParams,
} from "./types";

export async function fullTextSearch(
  params: SearchParams,
): Promise<PaginatedResponse<WorkItemDto>> {
  const response = await apiClient.get<PaginatedResponse<WorkItemDto>>(
    "/search",
    { params },
  );
  return response.data;
}

export async function getSavedFilters(
  projectId: string,
): Promise<SavedFilterDto[]> {
  const response = await apiClient.get<SavedFilterDto[]>(
    `/projects/${projectId}/saved-filters`,
  );
  return response.data;
}

export async function saveFilter(
  projectId: string,
  body: SaveFilterBody,
): Promise<SavedFilterDto> {
  const response = await apiClient.post<SavedFilterDto>(
    `/projects/${projectId}/saved-filters`,
    body,
  );
  return response.data;
}

export async function updateSavedFilter(
  projectId: string,
  filterId: string,
  body: UpdateSavedFilterBody,
): Promise<SavedFilterDto> {
  const response = await apiClient.put<SavedFilterDto>(
    `/projects/${projectId}/saved-filters/${filterId}`,
    body,
  );
  return response.data;
}

export async function deleteSavedFilter(
  projectId: string,
  filterId: string,
): Promise<void> {
  await apiClient.delete(`/projects/${projectId}/saved-filters/${filterId}`);
}
