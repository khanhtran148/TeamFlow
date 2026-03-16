import {
  useQuery,
  useMutation,
  useQueryClient,
  type UseQueryOptions,
} from "@tanstack/react-query";
import {
  getProjects,
  getProject,
  createProject,
  updateProject,
  archiveProject,
  deleteProject,
} from "@/lib/api/projects";
import type {
  ProjectDto,
  PaginatedResponse,
  GetProjectsParams,
  CreateProjectBody,
  UpdateProjectBody,
} from "@/lib/api/types";

// ---- Query keys ----

export const projectKeys = {
  all: ["projects"] as const,
  list: (params?: GetProjectsParams) => ["projects", "list", params] as const,
  detail: (id: string) => ["projects", id] as const,
};

// ---- Queries ----

export function useProjects(
  params?: GetProjectsParams,
  options?: Partial<UseQueryOptions<PaginatedResponse<ProjectDto>>>,
) {
  return useQuery({
    queryKey: projectKeys.list(params),
    queryFn: () => getProjects(params),
    ...options,
  });
}

export function useProject(
  id: string,
  options?: Partial<UseQueryOptions<ProjectDto>>,
) {
  return useQuery({
    queryKey: projectKeys.detail(id),
    queryFn: () => getProject(id),
    enabled: !!id,
    retry: 2,
    retryDelay: 500,
    ...options,
  });
}

// ---- Mutations ----

export function useCreateProject() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateProjectBody) => createProject(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: projectKeys.all });
    },
  });
}

export function useUpdateProject() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateProjectBody }) =>
      updateProject(id, data),
    onSuccess: (_result, { id }) => {
      queryClient.invalidateQueries({ queryKey: projectKeys.all });
      queryClient.invalidateQueries({ queryKey: projectKeys.detail(id) });
    },
  });
}

export function useArchiveProject() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => archiveProject(id),
    onSuccess: (_result, id) => {
      queryClient.invalidateQueries({ queryKey: projectKeys.all });
      queryClient.invalidateQueries({ queryKey: projectKeys.detail(id) });
    },
  });
}

export function useDeleteProject() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => deleteProject(id),
    onSuccess: (_result, id) => {
      queryClient.invalidateQueries({ queryKey: projectKeys.all });
      queryClient.removeQueries({ queryKey: projectKeys.detail(id) });
    },
  });
}
