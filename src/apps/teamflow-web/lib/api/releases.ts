import { apiClient } from "./client";
import type {
  ReleaseDto,
  PaginatedResponse,
  GetReleasesParams,
  CreateReleaseBody,
  UpdateReleaseBody,
} from "./types";

export async function getReleases(
  params: GetReleasesParams,
): Promise<PaginatedResponse<ReleaseDto>> {
  const response = await apiClient.get<PaginatedResponse<ReleaseDto>>("/releases", { params });
  return response.data;
}

export async function getRelease(id: string): Promise<ReleaseDto> {
  const response = await apiClient.get<ReleaseDto>(`/releases/${id}`);
  return response.data;
}

export async function createRelease(data: CreateReleaseBody): Promise<ReleaseDto> {
  const response = await apiClient.post<ReleaseDto>("/releases", data);
  return response.data;
}

export async function updateRelease(id: string, data: UpdateReleaseBody): Promise<ReleaseDto> {
  const response = await apiClient.put<ReleaseDto>(`/releases/${id}`, data);
  return response.data;
}

export async function deleteRelease(id: string): Promise<void> {
  await apiClient.delete(`/releases/${id}`);
}

export async function assignItem(releaseId: string, workItemId: string): Promise<void> {
  await apiClient.post(`/releases/${releaseId}/items/${workItemId}`);
}

export async function unassignItem(releaseId: string, workItemId: string): Promise<void> {
  await apiClient.delete(`/releases/${releaseId}/items/${workItemId}`);
}
