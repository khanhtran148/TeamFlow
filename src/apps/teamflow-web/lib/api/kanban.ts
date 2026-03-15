import { apiClient } from "./client";
import type { KanbanBoardDto, GetKanbanParams } from "./types";

export async function getKanbanBoard(params: GetKanbanParams): Promise<KanbanBoardDto> {
  const response = await apiClient.get<KanbanBoardDto>("/kanban", { params });
  return response.data;
}
