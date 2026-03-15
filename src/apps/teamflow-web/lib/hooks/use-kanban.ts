import { useQuery, type UseQueryOptions } from "@tanstack/react-query";
import { getKanbanBoard } from "@/lib/api/kanban";
import type { KanbanBoardDto, GetKanbanParams } from "@/lib/api/types";

// ---- Query keys ----

export const kanbanKeys = {
  board: (params: GetKanbanParams) => ["kanban", params.projectId, params] as const,
};

// ---- Queries ----

export function useKanbanBoard(
  params: GetKanbanParams,
  options?: Partial<UseQueryOptions<KanbanBoardDto>>,
) {
  return useQuery({
    queryKey: kanbanKeys.board(params),
    queryFn: () => getKanbanBoard(params),
    enabled: !!params.projectId,
    staleTime: 30_000,
    ...options,
  });
}
