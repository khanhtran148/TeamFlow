"use client";

import { CommentItem } from "./comment-item";
import type { CommentDto } from "@/lib/api/types";

interface CommentThreadProps {
  comment: CommentDto;
  currentUserId: string | undefined;
  onEdit: (id: string, content: string) => Promise<void>;
  onDelete: (id: string) => Promise<void>;
  onReply: (parentId: string) => void;
  editPending: boolean;
  deletePending: boolean;
}

export function CommentThread({
  comment,
  currentUserId,
  onEdit,
  onDelete,
  onReply,
  editPending,
  deletePending,
}: CommentThreadProps) {
  return (
    <div>
      {/* Parent comment */}
      <CommentItem
        comment={comment}
        currentUserId={currentUserId}
        onEdit={onEdit}
        onDelete={onDelete}
        onReply={onReply}
        isEditing={false}
        editPending={editPending}
        deletePending={deletePending}
      />

      {/* Replies */}
      {comment.replies.length > 0 && (
        <div
          style={{
            marginLeft: 38,
            borderLeft: "2px solid var(--tf-border)",
            paddingLeft: 14,
          }}
        >
          {comment.replies.map((reply) => (
            <CommentItem
              key={reply.id}
              comment={reply}
              currentUserId={currentUserId}
              onEdit={onEdit}
              onDelete={onDelete}
              onReply={onReply}
              isEditing={false}
              editPending={editPending}
              deletePending={deletePending}
            />
          ))}
        </div>
      )}
    </div>
  );
}
