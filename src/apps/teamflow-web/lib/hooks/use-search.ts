import {
  useQuery,
  useMutation,
  useQueryClient,
  type UseQueryOptions,
} from "@tanstack/react-query";
import {
  fullTextSearch,
  getSavedFilters,
  saveFilter,
  updateSavedFilter,
  deleteSavedFilter,
} from "@/lib/api/search";
import type {
  PaginatedResponse,
  WorkItemDto,
  SavedFilterDto,
  SaveFilterBody,
  UpdateSavedFilterBody,
  SearchParams,
} from "@/lib/api/types";

export const searchKeys = {
  all: (projectId: string) => ["search", projectId] as const,
  results: (params: SearchParams) => ["search", params.projectId, params] as const,
  savedFilters: (projectId: string) =>
    ["saved-filters", projectId] as const,
};

export function useFullTextSearch(
  params: SearchParams,
  options?: Partial<UseQueryOptions<PaginatedResponse<WorkItemDto>>>,
) {
  return useQuery({
    queryKey: searchKeys.results(params),
    queryFn: () => fullTextSearch(params),
    enabled: !!params.projectId,
    ...options,
  });
}

export function useSavedFilters(
  projectId: string,
  options?: Partial<UseQueryOptions<SavedFilterDto[]>>,
) {
  return useQuery({
    queryKey: searchKeys.savedFilters(projectId),
    queryFn: () => getSavedFilters(projectId),
    enabled: !!projectId,
    ...options,
  });
}

export function useSaveFilter(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (body: SaveFilterBody) => saveFilter(projectId, body),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: searchKeys.savedFilters(projectId),
      });
    },
  });
}

export function useUpdateSavedFilter(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      filterId,
      body,
    }: {
      filterId: string;
      body: UpdateSavedFilterBody;
    }) => updateSavedFilter(projectId, filterId, body),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: searchKeys.savedFilters(projectId),
      });
    },
  });
}

export function useDeleteSavedFilter(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (filterId: string) => deleteSavedFilter(projectId, filterId),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: searchKeys.savedFilters(projectId),
      });
    },
  });
}
