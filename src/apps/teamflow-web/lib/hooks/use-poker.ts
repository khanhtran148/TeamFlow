import {
  useQuery,
  useMutation,
  useQueryClient,
  type UseQueryOptions,
} from "@tanstack/react-query";
import {
  createPokerSession,
  getPokerSession,
  getPokerSessionByWorkItem,
  castPokerVote,
  revealPokerVotes,
  confirmPokerEstimate,
} from "@/lib/api/poker";
import type {
  PokerSessionDto,
  CreatePokerSessionBody,
  CastPokerVoteBody,
  ConfirmPokerEstimateBody,
} from "@/lib/api/types";

// ---- Query keys ----

export const pokerKeys = {
  detail: (id: string) => ["poker", "detail", id] as const,
  byWorkItem: (workItemId: string) =>
    ["poker", "by-workitem", workItemId] as const,
};

// ---- Queries ----

export function usePokerSession(
  id: string,
  options?: Partial<UseQueryOptions<PokerSessionDto>>,
) {
  return useQuery({
    queryKey: pokerKeys.detail(id),
    queryFn: () => getPokerSession(id),
    enabled: !!id,
    refetchInterval: 3000, // Poll for vote updates
    ...options,
  });
}

export function usePokerSessionByWorkItem(
  workItemId: string,
  options?: Partial<UseQueryOptions<PokerSessionDto>>,
) {
  return useQuery({
    queryKey: pokerKeys.byWorkItem(workItemId),
    queryFn: () => getPokerSessionByWorkItem(workItemId),
    enabled: !!workItemId,
    retry: false, // 404 is expected when no session exists
    ...options,
  });
}

// ---- Mutations ----

export function useCreatePokerSession() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreatePokerSessionBody) => createPokerSession(data),
    onSuccess: (result) => {
      queryClient.invalidateQueries({
        queryKey: pokerKeys.byWorkItem(result.workItemId),
      });
    },
  });
}

export function useCastPokerVote(sessionId: string, workItemId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CastPokerVoteBody) => castPokerVote(sessionId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: pokerKeys.detail(sessionId) });
      queryClient.invalidateQueries({
        queryKey: pokerKeys.byWorkItem(workItemId),
      });
    },
  });
}

export function useRevealPokerVotes(sessionId: string, workItemId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => revealPokerVotes(sessionId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: pokerKeys.detail(sessionId) });
      queryClient.invalidateQueries({
        queryKey: pokerKeys.byWorkItem(workItemId),
      });
    },
  });
}

export function useConfirmPokerEstimate(sessionId: string, workItemId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: ConfirmPokerEstimateBody) =>
      confirmPokerEstimate(sessionId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: pokerKeys.detail(sessionId) });
      queryClient.invalidateQueries({
        queryKey: pokerKeys.byWorkItem(workItemId),
      });
      // Also invalidate work item detail since estimation changes
      queryClient.invalidateQueries({ queryKey: ["work-items", "detail"] });
    },
  });
}
