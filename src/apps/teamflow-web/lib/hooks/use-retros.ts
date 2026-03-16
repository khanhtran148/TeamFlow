import {
  useQuery,
  useMutation,
  useQueryClient,
  type UseQueryOptions,
} from "@tanstack/react-query";
import {
  listRetroSessions,
  getRetroSession,
  createRetroSession,
  updateColumnsConfig,
  startRetroSession,
  transitionRetroSession,
  closeRetroSession,
  submitRetroCard,
  castRetroVote,
  markCardDiscussed,
  createRetroActionItem,
  getPreviousActionItems,
} from "@/lib/api/retros";
import type {
  RetroSessionDto,
  RetroActionItemDto,
  ListRetroSessionsResponse,
  CreateRetroSessionBody,
  SubmitRetroCardBody,
  CastRetroVoteBody,
  TransitionRetroBody,
  CreateRetroActionItemBody,
} from "@/lib/api/types";

// ---- Query keys ----

export const retroKeys = {
  all: (projectId: string) => ["retros", projectId] as const,
  list: (projectId: string, page: number) =>
    ["retros", projectId, "list", page] as const,
  detail: (id: string) => ["retros", "detail", id] as const,
  previousActions: (projectId: string) =>
    ["retros", projectId, "previous-actions"] as const,
};

// ---- Queries ----

export function useRetroSessions(
  projectId: string,
  page = 1,
  pageSize = 20,
  options?: Partial<UseQueryOptions<ListRetroSessionsResponse>>,
) {
  return useQuery({
    queryKey: retroKeys.list(projectId, page),
    queryFn: () => listRetroSessions(projectId, page, pageSize),
    enabled: !!projectId,
    ...options,
  });
}

export function useRetroSession(
  id: string,
  options?: Partial<UseQueryOptions<RetroSessionDto>>,
) {
  return useQuery({
    queryKey: retroKeys.detail(id),
    queryFn: () => getRetroSession(id),
    enabled: !!id,
    refetchInterval: 5000, // Poll for realtime updates
    ...options,
  });
}

export function usePreviousActionItems(
  projectId: string,
  options?: Partial<UseQueryOptions<RetroActionItemDto[]>>,
) {
  return useQuery({
    queryKey: retroKeys.previousActions(projectId),
    queryFn: () => getPreviousActionItems(projectId),
    enabled: !!projectId,
    ...options,
  });
}

// ---- Mutations ----

export function useCreateRetroSession(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateRetroSessionBody) => createRetroSession(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: retroKeys.all(projectId) });
    },
  });
}

export function useStartRetroSession(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (sessionId: string) => startRetroSession(sessionId),
    onSuccess: (_result, sessionId) => {
      queryClient.invalidateQueries({ queryKey: retroKeys.all(projectId) });
      queryClient.invalidateQueries({ queryKey: retroKeys.detail(sessionId) });
    },
  });
}

export function useTransitionRetroSession(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ sessionId, data }: { sessionId: string; data: TransitionRetroBody }) =>
      transitionRetroSession(sessionId, data),
    onSuccess: (_result, { sessionId }) => {
      queryClient.invalidateQueries({ queryKey: retroKeys.all(projectId) });
      queryClient.invalidateQueries({ queryKey: retroKeys.detail(sessionId) });
    },
  });
}

export function useCloseRetroSession(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (sessionId: string) => closeRetroSession(sessionId),
    onSuccess: (_result, sessionId) => {
      queryClient.invalidateQueries({ queryKey: retroKeys.all(projectId) });
      queryClient.invalidateQueries({ queryKey: retroKeys.detail(sessionId) });
    },
  });
}

export function useSubmitRetroCard(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ sessionId, data }: { sessionId: string; data: SubmitRetroCardBody }) =>
      submitRetroCard(sessionId, data),
    onSuccess: (_result, { sessionId }) => {
      queryClient.invalidateQueries({ queryKey: retroKeys.detail(sessionId) });
    },
  });
}

export function useCastRetroVote(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      sessionId,
      cardId,
      data,
    }: {
      sessionId: string;
      cardId: string;
      data: CastRetroVoteBody;
    }) => castRetroVote(sessionId, cardId, data),
    onSuccess: (_result, { sessionId }) => {
      queryClient.invalidateQueries({ queryKey: retroKeys.detail(sessionId) });
    },
  });
}

export function useMarkCardDiscussed(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ sessionId, cardId }: { sessionId: string; cardId: string }) =>
      markCardDiscussed(sessionId, cardId),
    onSuccess: (_result, { sessionId }) => {
      queryClient.invalidateQueries({ queryKey: retroKeys.detail(sessionId) });
    },
  });
}

export function useCreateRetroActionItem(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      sessionId,
      data,
    }: {
      sessionId: string;
      data: Omit<CreateRetroActionItemBody, "sessionId">;
    }) => createRetroActionItem(sessionId, data),
    onSuccess: (_result, { sessionId }) => {
      queryClient.invalidateQueries({ queryKey: retroKeys.detail(sessionId) });
      queryClient.invalidateQueries({
        queryKey: retroKeys.previousActions(projectId),
      });
    },
  });
}

export function useUpdateColumnsConfig() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      sessionId,
      columnsConfig,
    }: {
      sessionId: string;
      columnsConfig: unknown;
    }) => updateColumnsConfig(sessionId, columnsConfig),
    onSuccess: (_result, { sessionId }) => {
      queryClient.invalidateQueries({ queryKey: retroKeys.detail(sessionId) });
    },
  });
}
