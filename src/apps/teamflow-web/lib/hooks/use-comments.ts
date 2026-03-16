import {
  useQuery,
  useMutation,
  useQueryClient,
  useInfiniteQuery,
  type UseQueryOptions,
} from "@tanstack/react-query";
import {
  getComments,
  createComment,
  updateComment,
  deleteComment,
} from "@/lib/api/comments";
import type {
  CommentDto,
  GetCommentsResponse,
  CreateCommentBody,
  UpdateCommentBody,
} from "@/lib/api/types";

// ---- Query keys ----

export const commentKeys = {
  all: (workItemId: string) => ["comments", workItemId] as const,
  list: (workItemId: string, page: number) =>
    ["comments", workItemId, "list", page] as const,
};

// ---- Queries ----

export function useComments(
  workItemId: string,
  page = 1,
  pageSize = 20,
  options?: Partial<UseQueryOptions<GetCommentsResponse>>,
) {
  return useQuery({
    queryKey: commentKeys.list(workItemId, page),
    queryFn: () => getComments(workItemId, page, pageSize),
    enabled: !!workItemId,
    ...options,
  });
}

export function useCommentsInfinite(workItemId: string, pageSize = 20) {
  return useInfiniteQuery({
    queryKey: commentKeys.all(workItemId),
    queryFn: ({ pageParam }) => getComments(workItemId, pageParam, pageSize),
    initialPageParam: 1,
    getNextPageParam: (lastPage) => {
      const totalPages = Math.ceil(lastPage.totalCount / lastPage.pageSize);
      return lastPage.page < totalPages ? lastPage.page + 1 : undefined;
    },
    enabled: !!workItemId,
  });
}

// ---- Mutations ----

export function useCreateComment(workItemId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateCommentBody) => createComment(workItemId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: commentKeys.all(workItemId) });
    },
  });
}

export function useUpdateComment(workItemId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateCommentBody }) =>
      updateComment(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: commentKeys.all(workItemId) });
    },
  });
}

export function useDeleteComment(workItemId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => deleteComment(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: commentKeys.all(workItemId) });
    },
  });
}
