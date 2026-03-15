import { apiClient } from "./client";
import type {
  SprintDto,
  SprintDetailDto,
  BurndownDto,
  PaginatedResponse,
  GetSprintsParams,
  CreateSprintBody,
  UpdateSprintBody,
  UpdateCapacityBody,
} from "./types";

export async function getSprints(
  params: GetSprintsParams,
): Promise<PaginatedResponse<SprintDto>> {
  const response = await apiClient.get<PaginatedResponse<SprintDto>>("/sprints", { params });
  return response.data;
}

export async function getSprint(id: string): Promise<SprintDetailDto> {
  const response = await apiClient.get<SprintDetailDto>(`/sprints/${id}`);
  return response.data;
}

export async function createSprint(data: CreateSprintBody): Promise<SprintDto> {
  const response = await apiClient.post<SprintDto>("/sprints", data);
  return response.data;
}

export async function updateSprint(id: string, data: UpdateSprintBody): Promise<SprintDto> {
  const response = await apiClient.put<SprintDto>(`/sprints/${id}`, data);
  return response.data;
}

export async function deleteSprint(id: string): Promise<void> {
  await apiClient.delete(`/sprints/${id}`);
}

export async function startSprint(id: string): Promise<SprintDto> {
  const response = await apiClient.post<SprintDto>(`/sprints/${id}/start`);
  return response.data;
}

export async function completeSprint(id: string): Promise<SprintDto> {
  const response = await apiClient.post<SprintDto>(`/sprints/${id}/complete`);
  return response.data;
}

export async function addItemToSprint(sprintId: string, workItemId: string): Promise<void> {
  await apiClient.post(`/sprints/${sprintId}/items/${workItemId}`);
}

export async function removeItemFromSprint(sprintId: string, workItemId: string): Promise<void> {
  await apiClient.delete(`/sprints/${sprintId}/items/${workItemId}`);
}

export async function updateSprintCapacity(
  sprintId: string,
  data: UpdateCapacityBody,
): Promise<void> {
  await apiClient.put(`/sprints/${sprintId}/capacity`, data);
}

export async function getSprintBurndown(sprintId: string): Promise<BurndownDto> {
  const response = await apiClient.get<BurndownDto>(`/sprints/${sprintId}/burndown`);
  return response.data;
}
