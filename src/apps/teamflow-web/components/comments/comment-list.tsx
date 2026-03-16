"use client";

import { useState } from "react";
import { MessageSquare } from "lucide-react";
import { toast } from "sonner";
import { CommentThread } from "./comment-thread";
import { CommentForm } from "./comment-form";
import { LoadingSkeleton } from "@/components/shared/loading-skeleton";
import {
  useComments,
  useCreateComment,
  useUpdateComment,
  useDeleteComment,
} from "@/lib/hooks/use-comments";
import { useAuthStore } from "@/lib/stores/auth-store";
import type { ApiError } from "@/lib/api/client";

interface CommentListProps {
  workItemId: string;
  projectId: string;
}

export function CommentList({ workItemId, projectId }: CommentListProps) {
  const [page, setPage] = useState(1);
  const [replyTo, setReplyTo] = useState<string | null>(null);
  const currentUserId = useAuthStore((s) => s.user?.id);

  const { data, isLoading } = useComments(workItemId, page, 20);
  const createMutation = useCreateComment(workItemId);
  const updateMutation = useUpdateComment(workItemId);
  const deleteMutation = useDeleteComment(workItemId);

  async function handleCreate(content: string) {
    try {
      await createMutation.mutateAsync({
        content,
        parentCommentId: replyTo ?? undefined,
      });
      setReplyTo(null);
      toast.success("Comment posted");
    } catch (err) {
      const apiErr = err as ApiError;
      toast.error(apiErr.message ?? "Failed to post comment");
    }
  }

  async function handleEdit(id: string, content: string) {
    try {
      await updateMutation.mutateAsync({ id, data: { content } });
      toast.success("Comment updated");
    } catch (err) {
      const apiErr = err as ApiError;
      toast.error(apiErr.message ?? "Failed to update comment");
    }
  }

  async function handleDelete(id: string) {
    try {
      await deleteMutation.mutateAsync(id);
      toast.success("Comment deleted");
    } catch (err) {
      const apiErr = err as ApiError;
      toast.error(apiErr.message ?? "Failed to delete comment");
    }
  }

  const comments = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;
  const totalPages = data ? Math.ceil(data.totalCount / data.pageSize) : 1;

  return (
    <div>
      {/* Header */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 8,
          marginBottom: 16,
        }}
      >
        <MessageSquare size={15} style={{ color: "var(--tf-text3)" }} />
        <span
          style={{
            fontFamily: "var(--tf-font-head)",
            fontWeight: 600,
            fontSize: 14,
            color: "var(--tf-text)",
          }}
        >
          Comments
        </span>
        {totalCount > 0 && (
          <span
            style={{
              padding: "1px 7px",
              borderRadius: 100,
              background: "var(--tf-bg4)",
              color: "var(--tf-text3)",
              fontSize: 13,
              fontFamily: "var(--tf-font-mono)",
            }}
          >
            {totalCount}
          </span>
        )}
      </div>

      {/* Comment form */}
      <div style={{ marginBottom: 16 }}>
        {replyTo && (
          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: 6,
              marginBottom: 6,
              fontSize: 13,
              color: "var(--tf-text3)",
            }}
          >
            Replying to a comment
            <button
              onClick={() => setReplyTo(null)}
              style={{
                background: "none",
                border: "none",
                color: "var(--tf-accent)",
                cursor: "pointer",
                fontSize: 13,
                fontFamily: "var(--tf-font-body)",
                padding: 0,
              }}
            >
              Cancel
            </button>
          </div>
        )}
        <CommentForm
          onSubmit={handleCreate}
          isPending={createMutation.isPending}
          projectId={projectId}
          placeholder={
            replyTo
              ? "Write a reply..."
              : "Write a comment... Use @ to mention someone"
          }
        />
      </div>

      {/* Comments list */}
      {isLoading ? (
        <LoadingSkeleton rows={3} type="list-row" />
      ) : comments.length === 0 ? (
        <div
          style={{
            padding: "24px 0",
            textAlign: "center",
            color: "var(--tf-text3)",
            fontSize: 13,
          }}
        >
          No comments yet. Be the first to comment.
        </div>
      ) : (
        <div>
          {comments.map((comment) => (
            <div
              key={comment.id}
              style={{
                borderBottom: "1px solid var(--tf-border)",
              }}
            >
              <CommentThread
                comment={comment}
                currentUserId={currentUserId ?? undefined}
                onEdit={handleEdit}
                onDelete={handleDelete}
                onReply={(parentId) => setReplyTo(parentId)}
                editPending={updateMutation.isPending}
                deletePending={deleteMutation.isPending}
              />
            </div>
          ))}

          {/* Pagination */}
          {totalPages > 1 && (
            <div
              style={{
                display: "flex",
                justifyContent: "center",
                gap: 8,
                padding: "12px 0",
              }}
            >
              <button
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page === 1}
                style={{
                  padding: "4px 10px",
                  borderRadius: 5,
                  border: "1px solid var(--tf-border)",
                  background: "transparent",
                  color: page === 1 ? "var(--tf-text3)" : "var(--tf-text2)",
                  fontSize: 13,
                  cursor: page === 1 ? "not-allowed" : "pointer",
                  fontFamily: "var(--tf-font-body)",
                }}
              >
                Previous
              </button>
              <span
                style={{
                  fontSize: 13,
                  color: "var(--tf-text3)",
                  display: "flex",
                  alignItems: "center",
                  fontFamily: "var(--tf-font-mono)",
                }}
              >
                {page} / {totalPages}
              </span>
              <button
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                disabled={page === totalPages}
                style={{
                  padding: "4px 10px",
                  borderRadius: 5,
                  border: "1px solid var(--tf-border)",
                  background: "transparent",
                  color:
                    page === totalPages
                      ? "var(--tf-text3)"
                      : "var(--tf-text2)",
                  fontSize: 13,
                  cursor: page === totalPages ? "not-allowed" : "pointer",
                  fontFamily: "var(--tf-font-body)",
                }}
              >
                Next
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
