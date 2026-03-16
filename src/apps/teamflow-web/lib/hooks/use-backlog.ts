import {
  useQuery,
  useMutation,
  useQueryClient,
  type UseQueryOptions,
} from "@tanstack/react-query";
import {
  getBacklog,
  reorderBacklog,
  toggleReadyForSprint,
  bulkUpdatePriority,
} from "@/lib/api/backlog";
import type {
  BacklogItemDto,
  PaginatedResponse,
  GetBacklogParams,
  ReorderBacklogBody,
  ToggleReadyBody,
  BulkUpdatePriorityBody,
} from "@/lib/api/types";

// ---- Query keys ----

export const backlogKeys = {
  all: (projectId: string) => ["backlog", projectId] as const,
  list: (params: GetBacklogParams) =>
    ["backlog", params.projectId, params] as const,
};

// ---- Queries ----

export function useBacklog(
  params: GetBacklogParams,
  options?: Partial<UseQueryOptions<PaginatedResponse<BacklogItemDto>>>,
) {
  return useQuery({
    queryKey: backlogKeys.list(params),
    queryFn: () => getBacklog(params),
    enabled: !!params.projectId,
    ...options,
  });
}

// ---- Mutations ----

export function useReorderBacklog() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: ReorderBacklogBody) => reorderBacklog(data),
    onSuccess: (_result, variables) => {
      queryClient.invalidateQueries({
        queryKey: backlogKeys.all(variables.projectId),
      });
    },
  });
}

export function useToggleReadyForSprint(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      workItemId,
      data,
    }: {
      workItemId: string;
      data: ToggleReadyBody;
    }) => toggleReadyForSprint(workItemId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: backlogKeys.all(projectId),
      });
    },
  });
}

export function useBulkUpdatePriority(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: BulkUpdatePriorityBody) => bulkUpdatePriority(data),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: backlogKeys.all(projectId),
      });
    },
  });
}
