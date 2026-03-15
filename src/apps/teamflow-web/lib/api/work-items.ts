import { apiClient } from "./client";
import type {
  WorkItemDto,
  WorkItemLinksDto,
  BlockersDto,
  CreateWorkItemBody,
  UpdateWorkItemBody,
  ChangeStatusBody,
  AssignBody,
  MoveWorkItemBody,
  AddLinkBody,
} from "./types";

export async function getWorkItem(id: string): Promise<WorkItemDto> {
  const response = await apiClient.get<WorkItemDto>(`/workitems/${id}`);
  return response.data;
}

export async function createWorkItem(data: CreateWorkItemBody): Promise<WorkItemDto> {
  const response = await apiClient.post<WorkItemDto>("/workitems", data);
  return response.data;
}

export async function updateWorkItem(
  id: string,
  data: UpdateWorkItemBody,
): Promise<WorkItemDto> {
  const response = await apiClient.put<WorkItemDto>(`/workitems/${id}`, data);
  return response.data;
}

export async function changeStatus(
  id: string,
  data: ChangeStatusBody,
): Promise<WorkItemDto> {
  const response = await apiClient.post<WorkItemDto>(`/workitems/${id}/status`, data);
  return response.data;
}

export async function deleteWorkItem(id: string): Promise<void> {
  await apiClient.delete(`/workitems/${id}`);
}

export async function moveWorkItem(
  id: string,
  data: MoveWorkItemBody,
): Promise<void> {
  await apiClient.post(`/workitems/${id}/move`, data);
}

export async function assignWorkItem(
  id: string,
  data: AssignBody,
): Promise<void> {
  await apiClient.post(`/workitems/${id}/assign`, data);
}

export async function unassignWorkItem(id: string): Promise<void> {
  await apiClient.post(`/workitems/${id}/unassign`);
}

export async function addLink(
  id: string,
  data: AddLinkBody,
): Promise<void> {
  await apiClient.post(`/workitems/${id}/links`, data);
}

export async function removeLink(id: string, linkId: string): Promise<void> {
  await apiClient.delete(`/workitems/${id}/links/${linkId}`);
}

export async function getLinks(id: string): Promise<WorkItemLinksDto> {
  const response = await apiClient.get<WorkItemLinksDto>(`/workitems/${id}/links`);
  return response.data;
}

export async function getBlockers(id: string): Promise<BlockersDto> {
  const response = await apiClient.get<BlockersDto>(`/workitems/${id}/blockers`);
  return response.data;
}
