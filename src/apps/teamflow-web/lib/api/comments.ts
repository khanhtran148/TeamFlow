import { apiClient } from "./client";
import type {
  CommentDto,
  GetCommentsResponse,
  CreateCommentBody,
  UpdateCommentBody,
} from "./types";

export async function getComments(
  workItemId: string,
  page = 1,
  pageSize = 20,
): Promise<GetCommentsResponse> {
  const response = await apiClient.get<GetCommentsResponse>(
    `/workitems/${workItemId}/comments`,
    { params: { page, pageSize } },
  );
  return response.data;
}

export async function createComment(
  workItemId: string,
  data: CreateCommentBody,
): Promise<CommentDto> {
  const response = await apiClient.post<CommentDto>(
    `/workitems/${workItemId}/comments`,
    data,
  );
  return response.data;
}

export async function updateComment(
  commentId: string,
  data: UpdateCommentBody,
): Promise<CommentDto> {
  const response = await apiClient.put<CommentDto>(
    `/comments/${commentId}`,
    data,
  );
  return response.data;
}

export async function deleteComment(commentId: string): Promise<void> {
  await apiClient.delete(`/comments/${commentId}`);
}
