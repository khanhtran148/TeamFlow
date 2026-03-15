import {
  useQuery,
  useMutation,
  useQueryClient,
  type UseQueryOptions,
} from "@tanstack/react-query";
import {
  getSprints,
  getSprint,
  createSprint,
  updateSprint,
  deleteSprint,
  startSprint,
  completeSprint,
  addItemToSprint,
  removeItemFromSprint,
  updateSprintCapacity,
  getSprintBurndown,
} from "@/lib/api/sprints";
import { backlogKeys } from "@/lib/hooks/use-backlog";
import type {
  SprintDto,
  SprintDetailDto,
  BurndownDto,
  PaginatedResponse,
  GetSprintsParams,
  CreateSprintBody,
  UpdateSprintBody,
  UpdateCapacityBody,
} from "@/lib/api/types";

// ---- Query keys ----

export const sprintKeys = {
  all: (projectId: string) => ["sprints", projectId] as const,
  list: (params: GetSprintsParams) => ["sprints", params.projectId, "list", params] as const,
  detail: (id: string) => ["sprints", "detail", id] as const,
  burndown: (id: string) => ["sprints", "burndown", id] as const,
};

// ---- Queries ----

export function useSprints(
  params: GetSprintsParams,
  options?: Partial<UseQueryOptions<PaginatedResponse<SprintDto>>>,
) {
  return useQuery({
    queryKey: sprintKeys.list(params),
    queryFn: () => getSprints(params),
    enabled: !!params.projectId,
    ...options,
  });
}

export function useSprint(
  id: string,
  options?: Partial<UseQueryOptions<SprintDetailDto>>,
) {
  return useQuery({
    queryKey: sprintKeys.detail(id),
    queryFn: () => getSprint(id),
    enabled: !!id,
    ...options,
  });
}

export function useSprintBurndown(
  sprintId: string,
  options?: Partial<UseQueryOptions<BurndownDto>>,
) {
  return useQuery({
    queryKey: sprintKeys.burndown(sprintId),
    queryFn: () => getSprintBurndown(sprintId),
    enabled: !!sprintId,
    ...options,
  });
}

// ---- Mutations ----

export function useCreateSprint(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateSprintBody) => createSprint(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: sprintKeys.all(projectId) });
    },
  });
}

export function useUpdateSprint(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateSprintBody }) =>
      updateSprint(id, data),
    onSuccess: (_result, { id }) => {
      queryClient.invalidateQueries({ queryKey: sprintKeys.all(projectId) });
      queryClient.invalidateQueries({ queryKey: sprintKeys.detail(id) });
    },
  });
}

export function useDeleteSprint(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => deleteSprint(id),
    onSuccess: (_result, id) => {
      queryClient.invalidateQueries({ queryKey: sprintKeys.all(projectId) });
      queryClient.removeQueries({ queryKey: sprintKeys.detail(id) });
      // Unlinked items affect backlog
      queryClient.invalidateQueries({ queryKey: backlogKeys.all(projectId) });
    },
  });
}

export function useStartSprint(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => startSprint(id),
    onSuccess: (_result, id) => {
      queryClient.invalidateQueries({ queryKey: sprintKeys.all(projectId) });
      queryClient.invalidateQueries({ queryKey: sprintKeys.detail(id) });
    },
  });
}

export function useCompleteSprint(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => completeSprint(id),
    onSuccess: (_result, id) => {
      queryClient.invalidateQueries({ queryKey: sprintKeys.all(projectId) });
      queryClient.invalidateQueries({ queryKey: sprintKeys.detail(id) });
      // Carried-over items go back to backlog
      queryClient.invalidateQueries({ queryKey: backlogKeys.all(projectId) });
    },
  });
}

export function useAddItemToSprint(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ sprintId, workItemId }: { sprintId: string; workItemId: string }) =>
      addItemToSprint(sprintId, workItemId),
    onSuccess: (_result, { sprintId }) => {
      queryClient.invalidateQueries({ queryKey: sprintKeys.all(projectId) });
      queryClient.invalidateQueries({ queryKey: sprintKeys.detail(sprintId) });
      queryClient.invalidateQueries({ queryKey: backlogKeys.all(projectId) });
    },
  });
}

export function useRemoveItemFromSprint(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ sprintId, workItemId }: { sprintId: string; workItemId: string }) =>
      removeItemFromSprint(sprintId, workItemId),
    onSuccess: (_result, { sprintId }) => {
      queryClient.invalidateQueries({ queryKey: sprintKeys.all(projectId) });
      queryClient.invalidateQueries({ queryKey: sprintKeys.detail(sprintId) });
      queryClient.invalidateQueries({ queryKey: backlogKeys.all(projectId) });
    },
  });
}

export function useUpdateSprintCapacity(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ sprintId, data }: { sprintId: string; data: UpdateCapacityBody }) =>
      updateSprintCapacity(sprintId, data),
    onSuccess: (_result, { sprintId }) => {
      queryClient.invalidateQueries({ queryKey: sprintKeys.all(projectId) });
      queryClient.invalidateQueries({ queryKey: sprintKeys.detail(sprintId) });
    },
  });
}
