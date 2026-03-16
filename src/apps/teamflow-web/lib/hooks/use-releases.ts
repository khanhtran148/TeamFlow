import {
  useQuery,
  useMutation,
  useQueryClient,
  type UseQueryOptions,
} from "@tanstack/react-query";
import {
  getReleases,
  getRelease,
  createRelease,
  updateRelease,
  deleteRelease,
  assignItem,
  unassignItem,
  getReleaseDetail,
  updateReleaseNotes,
  shipRelease,
} from "@/lib/api/releases";
import type {
  ReleaseDto,
  ReleaseDetailDto,
  ShipReleaseResultDto,
  PaginatedResponse,
  GetReleasesParams,
  CreateReleaseBody,
  UpdateReleaseBody,
  UpdateReleaseNotesBody,
  ShipReleaseBody,
} from "@/lib/api/types";

// ---- Query keys ----

export const releaseKeys = {
  all: (projectId: string) => ["releases", projectId] as const,
  list: (params: GetReleasesParams) => ["releases", params.projectId, "list", params] as const,
  detail: (id: string) => ["releases", "detail", id] as const,
  fullDetail: (id: string) => ["releases", "full-detail", id] as const,
};

// ---- Queries ----

export function useReleases(
  params: GetReleasesParams,
  options?: Partial<UseQueryOptions<PaginatedResponse<ReleaseDto>>>,
) {
  return useQuery({
    queryKey: releaseKeys.list(params),
    queryFn: () => getReleases(params),
    enabled: !!params.projectId,
    ...options,
  });
}

export function useRelease(
  id: string,
  options?: Partial<UseQueryOptions<ReleaseDto>>,
) {
  return useQuery({
    queryKey: releaseKeys.detail(id),
    queryFn: () => getRelease(id),
    enabled: !!id,
    ...options,
  });
}

// ---- Mutations ----

export function useCreateRelease(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateReleaseBody) => createRelease(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: releaseKeys.all(projectId) });
    },
  });
}

export function useUpdateRelease(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateReleaseBody }) =>
      updateRelease(id, data),
    onSuccess: (_result, { id }) => {
      queryClient.invalidateQueries({ queryKey: releaseKeys.all(projectId) });
      queryClient.invalidateQueries({ queryKey: releaseKeys.detail(id) });
    },
  });
}

export function useDeleteRelease(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => deleteRelease(id),
    onSuccess: (_result, id) => {
      queryClient.invalidateQueries({ queryKey: releaseKeys.all(projectId) });
      queryClient.removeQueries({ queryKey: releaseKeys.detail(id) });
      // Also invalidate backlog so release badges update
      queryClient.invalidateQueries({ queryKey: ["backlog", projectId] });
    },
  });
}

export function useAssignItem(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ releaseId, workItemId }: { releaseId: string; workItemId: string }) =>
      assignItem(releaseId, workItemId),
    onSuccess: (_result, { releaseId }) => {
      queryClient.invalidateQueries({ queryKey: releaseKeys.all(projectId) });
      queryClient.invalidateQueries({ queryKey: releaseKeys.detail(releaseId) });
      queryClient.invalidateQueries({ queryKey: ["backlog", projectId] });
    },
  });
}

export function useUnassignItem(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ releaseId, workItemId }: { releaseId: string; workItemId: string }) =>
      unassignItem(releaseId, workItemId),
    onSuccess: (_result, { releaseId }) => {
      queryClient.invalidateQueries({ queryKey: releaseKeys.all(projectId) });
      queryClient.invalidateQueries({ queryKey: releaseKeys.detail(releaseId) });
      queryClient.invalidateQueries({ queryKey: ["backlog", projectId] });
    },
  });
}

// ---- Release Detail Queries & Mutations ----

export function useReleaseDetail(
  id: string,
  options?: Partial<UseQueryOptions<ReleaseDetailDto>>,
) {
  return useQuery({
    queryKey: releaseKeys.fullDetail(id),
    queryFn: () => getReleaseDetail(id),
    enabled: !!id,
    ...options,
  });
}

export function useUpdateReleaseNotes(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateReleaseNotesBody }) =>
      updateReleaseNotes(id, data),
    onSuccess: (_result, { id }) => {
      queryClient.invalidateQueries({ queryKey: releaseKeys.fullDetail(id) });
      queryClient.invalidateQueries({ queryKey: releaseKeys.detail(id) });
    },
  });
}

export function useShipRelease(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: ShipReleaseBody }) =>
      shipRelease(id, data),
    onSuccess: (_result, { id }) => {
      queryClient.invalidateQueries({ queryKey: releaseKeys.all(projectId) });
      queryClient.invalidateQueries({ queryKey: releaseKeys.fullDetail(id) });
      queryClient.invalidateQueries({ queryKey: releaseKeys.detail(id) });
    },
  });
}
