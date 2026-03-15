import {
  useQuery,
  useMutation,
  useQueryClient,
  type UseQueryOptions,
} from "@tanstack/react-query";
import {
  getWorkItem,
  createWorkItem,
  updateWorkItem,
  changeStatus,
  deleteWorkItem,
  moveWorkItem,
  assignWorkItem,
  unassignWorkItem,
  addLink,
  removeLink,
  getLinks,
  getBlockers,
} from "@/lib/api/work-items";
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
} from "@/lib/api/types";

// ---- Query keys ----

export const workItemKeys = {
  detail: (id: string) => ["work-items", id] as const,
  links: (id: string) => ["work-items", id, "links"] as const,
  blockers: (id: string) => ["work-items", id, "blockers"] as const,
};

// ---- Queries ----

export function useWorkItem(
  id: string,
  options?: Partial<UseQueryOptions<WorkItemDto>>,
) {
  return useQuery({
    queryKey: workItemKeys.detail(id),
    queryFn: () => getWorkItem(id),
    enabled: !!id,
    ...options,
  });
}

export function useWorkItemLinks(
  id: string,
  options?: Partial<UseQueryOptions<WorkItemLinksDto>>,
) {
  return useQuery({
    queryKey: workItemKeys.links(id),
    queryFn: () => getLinks(id),
    enabled: !!id,
    ...options,
  });
}

export function useWorkItemBlockers(
  id: string,
  options?: Partial<UseQueryOptions<BlockersDto>>,
) {
  return useQuery({
    queryKey: workItemKeys.blockers(id),
    queryFn: () => getBlockers(id),
    enabled: !!id,
    ...options,
  });
}

// ---- Mutations ----

export function useCreateWorkItem(projectId?: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateWorkItemBody) => createWorkItem(data),
    onSuccess: (_result, variables) => {
      const pid = projectId ?? variables.projectId;
      queryClient.invalidateQueries({ queryKey: ["backlog", pid] });
      queryClient.invalidateQueries({ queryKey: ["kanban", pid] });
    },
  });
}

export function useUpdateWorkItem(projectId?: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateWorkItemBody }) =>
      updateWorkItem(id, data),
    onSuccess: (_result, { id }) => {
      queryClient.invalidateQueries({ queryKey: workItemKeys.detail(id) });
      if (projectId) {
        queryClient.invalidateQueries({ queryKey: ["backlog", projectId] });
        queryClient.invalidateQueries({ queryKey: ["kanban", projectId] });
      }
    },
  });
}

export function useChangeStatus(projectId?: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: ChangeStatusBody }) =>
      changeStatus(id, data),
    onSuccess: (_result, { id }) => {
      queryClient.invalidateQueries({ queryKey: workItemKeys.detail(id) });
      if (projectId) {
        queryClient.invalidateQueries({ queryKey: ["backlog", projectId] });
        queryClient.invalidateQueries({ queryKey: ["kanban", projectId] });
      }
    },
  });
}

export function useDeleteWorkItem(projectId?: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => deleteWorkItem(id),
    onSuccess: (_result, id) => {
      queryClient.removeQueries({ queryKey: workItemKeys.detail(id) });
      if (projectId) {
        queryClient.invalidateQueries({ queryKey: ["backlog", projectId] });
        queryClient.invalidateQueries({ queryKey: ["kanban", projectId] });
      }
    },
  });
}

export function useMoveWorkItem(projectId?: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: MoveWorkItemBody }) =>
      moveWorkItem(id, data),
    onSuccess: (_result, { id }) => {
      queryClient.invalidateQueries({ queryKey: workItemKeys.detail(id) });
      if (projectId) {
        queryClient.invalidateQueries({ queryKey: ["backlog", projectId] });
      }
    },
  });
}

export function useAssignWorkItem(projectId?: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: AssignBody }) =>
      assignWorkItem(id, data),
    onSuccess: (_result, { id }) => {
      queryClient.invalidateQueries({ queryKey: workItemKeys.detail(id) });
      if (projectId) {
        queryClient.invalidateQueries({ queryKey: ["backlog", projectId] });
        queryClient.invalidateQueries({ queryKey: ["kanban", projectId] });
      }
    },
  });
}

export function useUnassignWorkItem(projectId?: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => unassignWorkItem(id),
    onSuccess: (_result, id) => {
      queryClient.invalidateQueries({ queryKey: workItemKeys.detail(id) });
      if (projectId) {
        queryClient.invalidateQueries({ queryKey: ["backlog", projectId] });
        queryClient.invalidateQueries({ queryKey: ["kanban", projectId] });
      }
    },
  });
}

export function useAddLink(projectId?: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: AddLinkBody }) =>
      addLink(id, data),
    onSuccess: (_result, { id }) => {
      queryClient.invalidateQueries({ queryKey: workItemKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: workItemKeys.links(id) });
      queryClient.invalidateQueries({ queryKey: workItemKeys.blockers(id) });
      if (projectId) {
        queryClient.invalidateQueries({ queryKey: ["backlog", projectId] });
      }
    },
  });
}

export function useRemoveLink(projectId?: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, linkId }: { id: string; linkId: string }) =>
      removeLink(id, linkId),
    onSuccess: (_result, { id }) => {
      queryClient.invalidateQueries({ queryKey: workItemKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: workItemKeys.links(id) });
      queryClient.invalidateQueries({ queryKey: workItemKeys.blockers(id) });
      if (projectId) {
        queryClient.invalidateQueries({ queryKey: ["backlog", projectId] });
      }
    },
  });
}
