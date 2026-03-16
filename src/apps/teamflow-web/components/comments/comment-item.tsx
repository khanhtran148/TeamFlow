"use client";

import { useState } from "react";
import { Pencil, Trash2, CornerDownRight } from "lucide-react";
import { UserAvatar } from "@/components/shared/user-avatar";
import type { CommentDto } from "@/lib/api/types";

interface CommentItemProps {
  comment: CommentDto;
  currentUserId: string | undefined;
  onEdit: (id: string, content: string) => Promise<void>;
  onDelete: (id: string) => Promise<void>;
  onReply: (parentId: string) => void;
  isEditing: boolean;
  editPending: boolean;
  deletePending: boolean;
}

function formatRelativeTime(dateStr: string): string {
  const date = new Date(dateStr);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMins = Math.floor(diffMs / 60000);

  if (diffMins < 1) return "just now";
  if (diffMins < 60) return `${diffMins}m ago`;
  const diffHours = Math.floor(diffMins / 60);
  if (diffHours < 24) return `${diffHours}h ago`;
  const diffDays = Math.floor(diffHours / 24);
  if (diffDays < 7) return `${diffDays}d ago`;
  return date.toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
  });
}

export function CommentItem({
  comment,
  currentUserId,
  onEdit,
  onDelete,
  onReply,
  isEditing,
  editPending,
  deletePending,
}: CommentItemProps) {
  const [editContent, setEditContent] = useState(comment.content);
  const [isEditMode, setIsEditMode] = useState(false);
  const isOwn = currentUserId === comment.authorId;

  async function handleSaveEdit() {
    if (!editContent.trim()) return;
    await onEdit(comment.id, editContent.trim());
    setIsEditMode(false);
  }

  // Render @mentions as styled spans
  function renderContent(text: string) {
    const parts = text.split(/(@[a-zA-Z0-9._-]+)/g);
    return parts.map((part, i) => {
      if (part.startsWith("@")) {
        return (
          <span
            key={i}
            style={{
              color: "var(--tf-accent)",
              fontWeight: 600,
              background: "var(--tf-accent-dim2)",
              borderRadius: 3,
              padding: "0 3px",
            }}
          >
            {part}
          </span>
        );
      }
      return <span key={i}>{part}</span>;
    });
  }

  return (
    <div
      style={{
        display: "flex",
        gap: 10,
        padding: "12px 0",
        opacity: deletePending ? 0.5 : 1,
      }}
    >
      <UserAvatar
        initials={comment.authorName.slice(0, 2).toUpperCase()}
        name={comment.authorName}
        size="sm"
      />

      <div style={{ flex: 1, minWidth: 0 }}>
        {/* Header */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 8,
            marginBottom: 4,
          }}
        >
          <span
            style={{
              fontSize: 12,
              fontWeight: 600,
              color: "var(--tf-text)",
            }}
          >
            {comment.authorName}
          </span>
          <span
            style={{
              fontSize: 11,
              color: "var(--tf-text3)",
              fontFamily: "var(--tf-font-mono)",
            }}
          >
            {formatRelativeTime(comment.createdAt)}
          </span>
          {comment.editedAt && (
            <span
              style={{
                fontSize: 10,
                color: "var(--tf-text3)",
                fontStyle: "italic",
              }}
            >
              (edited)
            </span>
          )}
        </div>

        {/* Content */}
        {isEditMode ? (
          <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
            <textarea
              value={editContent}
              onChange={(e) => setEditContent(e.target.value)}
              rows={3}
              style={{
                width: "100%",
                padding: "8px 10px",
                background: "var(--tf-bg4)",
                border: "1px solid var(--tf-border)",
                borderRadius: 6,
                fontSize: 13,
                color: "var(--tf-text)",
                fontFamily: "var(--tf-font-body)",
                outline: "none",
                resize: "vertical",
              }}
            />
            <div style={{ display: "flex", gap: 6 }}>
              <button
                onClick={handleSaveEdit}
                disabled={editPending || !editContent.trim()}
                style={{
                  padding: "4px 12px",
                  borderRadius: 5,
                  fontSize: 11,
                  fontWeight: 600,
                  border: "none",
                  background: "var(--tf-accent)",
                  color: "var(--tf-bg)",
                  cursor: "pointer",
                  fontFamily: "var(--tf-font-body)",
                }}
              >
                {editPending ? "Saving..." : "Save"}
              </button>
              <button
                onClick={() => {
                  setIsEditMode(false);
                  setEditContent(comment.content);
                }}
                style={{
                  padding: "4px 12px",
                  borderRadius: 5,
                  fontSize: 11,
                  border: "1px solid var(--tf-border)",
                  background: "transparent",
                  color: "var(--tf-text2)",
                  cursor: "pointer",
                  fontFamily: "var(--tf-font-body)",
                }}
              >
                Cancel
              </button>
            </div>
          </div>
        ) : (
          <p
            style={{
              fontSize: 13,
              color: "var(--tf-text2)",
              lineHeight: 1.5,
              margin: 0,
              whiteSpace: "pre-wrap",
              wordBreak: "break-word",
            }}
          >
            {renderContent(comment.content)}
          </p>
        )}

        {/* Actions */}
        {!isEditMode && (
          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: 10,
              marginTop: 6,
            }}
          >
            <button
              onClick={() => onReply(comment.id)}
              style={{
                display: "inline-flex",
                alignItems: "center",
                gap: 4,
                padding: 0,
                background: "transparent",
                border: "none",
                cursor: "pointer",
                fontSize: 11,
                color: "var(--tf-text3)",
                fontFamily: "var(--tf-font-body)",
                transition: "color var(--tf-tr)",
              }}
              onMouseEnter={(e) => {
                (e.currentTarget as HTMLButtonElement).style.color =
                  "var(--tf-accent)";
              }}
              onMouseLeave={(e) => {
                (e.currentTarget as HTMLButtonElement).style.color =
                  "var(--tf-text3)";
              }}
            >
              <CornerDownRight size={11} />
              Reply
            </button>

            {isOwn && (
              <>
                <button
                  onClick={() => setIsEditMode(true)}
                  style={{
                    display: "inline-flex",
                    alignItems: "center",
                    gap: 4,
                    padding: 0,
                    background: "transparent",
                    border: "none",
                    cursor: "pointer",
                    fontSize: 11,
                    color: "var(--tf-text3)",
                    fontFamily: "var(--tf-font-body)",
                    transition: "color var(--tf-tr)",
                  }}
                  onMouseEnter={(e) => {
                    (e.currentTarget as HTMLButtonElement).style.color =
                      "var(--tf-text)";
                  }}
                  onMouseLeave={(e) => {
                    (e.currentTarget as HTMLButtonElement).style.color =
                      "var(--tf-text3)";
                  }}
                >
                  <Pencil size={11} />
                  Edit
                </button>
                <button
                  onClick={() => onDelete(comment.id)}
                  disabled={deletePending}
                  style={{
                    display: "inline-flex",
                    alignItems: "center",
                    gap: 4,
                    padding: 0,
                    background: "transparent",
                    border: "none",
                    cursor: deletePending ? "not-allowed" : "pointer",
                    fontSize: 11,
                    color: "var(--tf-text3)",
                    fontFamily: "var(--tf-font-body)",
                    transition: "color var(--tf-tr)",
                  }}
                  onMouseEnter={(e) => {
                    if (!deletePending) {
                      (e.currentTarget as HTMLButtonElement).style.color =
                        "var(--tf-red)";
                    }
                  }}
                  onMouseLeave={(e) => {
                    (e.currentTarget as HTMLButtonElement).style.color =
                      "var(--tf-text3)";
                  }}
                >
                  <Trash2 size={11} />
                  Delete
                </button>
              </>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
