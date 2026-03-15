import { apiClient } from "./client";
import type {
  ProjectDto,
  PaginatedResponse,
  GetProjectsParams,
  CreateProjectBody,
  UpdateProjectBody,
} from "./types";

export async function getProjects(
  params?: GetProjectsParams,
): Promise<PaginatedResponse<ProjectDto>> {
  const response = await apiClient.get<PaginatedResponse<ProjectDto>>(
    "/projects",
    { params },
  );
  return response.data;
}

export async function getProject(id: string): Promise<ProjectDto> {
  const response = await apiClient.get<ProjectDto>(`/projects/${id}`);
  return response.data;
}

export async function createProject(data: CreateProjectBody): Promise<ProjectDto> {
  const response = await apiClient.post<ProjectDto>("/projects", data);
  return response.data;
}

export async function updateProject(
  id: string,
  data: UpdateProjectBody,
): Promise<ProjectDto> {
  const response = await apiClient.put<ProjectDto>(`/projects/${id}`, data);
  return response.data;
}

export async function archiveProject(id: string): Promise<void> {
  await apiClient.post(`/projects/${id}/archive`);
}

export async function deleteProject(id: string): Promise<void> {
  await apiClient.delete(`/projects/${id}`);
}
