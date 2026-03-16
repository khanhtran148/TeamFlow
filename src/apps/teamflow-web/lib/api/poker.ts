import { apiClient } from "./client";
import type {
  PokerSessionDto,
  CreatePokerSessionBody,
  CastPokerVoteBody,
  ConfirmPokerEstimateBody,
} from "./types";

export async function createPokerSession(
  data: CreatePokerSessionBody,
): Promise<PokerSessionDto> {
  const response = await apiClient.post<PokerSessionDto>("/poker", data);
  return response.data;
}

export async function getPokerSession(id: string): Promise<PokerSessionDto> {
  const response = await apiClient.get<PokerSessionDto>(`/poker/${id}`);
  return response.data;
}

export async function getPokerSessionByWorkItem(
  workItemId: string,
): Promise<PokerSessionDto> {
  const response = await apiClient.get<PokerSessionDto>(
    `/poker/by-workitem/${workItemId}`,
  );
  return response.data;
}

export async function castPokerVote(
  sessionId: string,
  data: CastPokerVoteBody,
): Promise<void> {
  await apiClient.post(`/poker/${sessionId}/vote`, data);
}

export async function revealPokerVotes(
  sessionId: string,
): Promise<PokerSessionDto> {
  const response = await apiClient.post<PokerSessionDto>(
    `/poker/${sessionId}/reveal`,
  );
  return response.data;
}

export async function confirmPokerEstimate(
  sessionId: string,
  data: ConfirmPokerEstimateBody,
): Promise<PokerSessionDto> {
  const response = await apiClient.post<PokerSessionDto>(
    `/poker/${sessionId}/confirm`,
    data,
  );
  return response.data;
}
