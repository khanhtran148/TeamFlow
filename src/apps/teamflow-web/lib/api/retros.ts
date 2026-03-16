import { apiClient } from "./client";
import type {
  RetroSessionDto,
  RetroCardDto,
  RetroActionItemDto,
  ListRetroSessionsResponse,
  CreateRetroSessionBody,
  SubmitRetroCardBody,
  CastRetroVoteBody,
  TransitionRetroBody,
  CreateRetroActionItemBody,
} from "./types";

export async function listRetroSessions(
  projectId: string,
  page = 1,
  pageSize = 20,
): Promise<ListRetroSessionsResponse> {
  const response = await apiClient.get<ListRetroSessionsResponse>("/retros", {
    params: { projectId, page, pageSize },
  });
  return response.data;
}

export async function getRetroSession(id: string): Promise<RetroSessionDto> {
  const response = await apiClient.get<RetroSessionDto>(`/retros/${id}`);
  return response.data;
}

export async function createRetroSession(
  data: CreateRetroSessionBody,
): Promise<RetroSessionDto> {
  const response = await apiClient.post<RetroSessionDto>("/retros", data);
  return response.data;
}

export async function startRetroSession(id: string): Promise<RetroSessionDto> {
  const response = await apiClient.post<RetroSessionDto>(`/retros/${id}/start`);
  return response.data;
}

export async function transitionRetroSession(
  id: string,
  data: TransitionRetroBody,
): Promise<RetroSessionDto> {
  const response = await apiClient.post<RetroSessionDto>(
    `/retros/${id}/transition`,
    data,
  );
  return response.data;
}

export async function closeRetroSession(id: string): Promise<RetroSessionDto> {
  const response = await apiClient.post<RetroSessionDto>(`/retros/${id}/close`);
  return response.data;
}

export async function submitRetroCard(
  sessionId: string,
  data: SubmitRetroCardBody,
): Promise<RetroCardDto> {
  const response = await apiClient.post<RetroCardDto>(
    `/retros/${sessionId}/cards`,
    data,
  );
  return response.data;
}

export async function castRetroVote(
  sessionId: string,
  cardId: string,
  data: CastRetroVoteBody,
): Promise<void> {
  await apiClient.post(`/retros/${sessionId}/cards/${cardId}/vote`, data);
}

export async function markCardDiscussed(
  sessionId: string,
  cardId: string,
): Promise<void> {
  await apiClient.post(`/retros/${sessionId}/cards/${cardId}/discussed`);
}

export async function createRetroActionItem(
  sessionId: string,
  data: Omit<CreateRetroActionItemBody, "sessionId">,
): Promise<RetroActionItemDto> {
  const response = await apiClient.post<RetroActionItemDto>(
    `/retros/${sessionId}/action-items`,
    { ...data, sessionId },
  );
  return response.data;
}

export async function renameRetroSession(
  sessionId: string,
  name: string,
): Promise<void> {
  await apiClient.put(`/retros/${sessionId}/name`, { name });
}

export async function deleteRetroSession(sessionId: string): Promise<void> {
  await apiClient.delete(`/retros/${sessionId}`);
}

export async function updateColumnsConfig(
  sessionId: string,
  columnsConfig: unknown,
): Promise<void> {
  await apiClient.put(`/retros/${sessionId}/columns-config`, { columnsConfig });
}

export async function getPreviousActionItems(
  projectId: string,
): Promise<RetroActionItemDto[]> {
  const response = await apiClient.get<RetroActionItemDto[]>(
    "/retros/previous-actions",
    { params: { projectId } },
  );
  return response.data;
}
