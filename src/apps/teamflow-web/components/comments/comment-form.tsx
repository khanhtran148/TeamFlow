"use client";

import { useState, useRef, useCallback } from "react";
import { Send } from "lucide-react";
import { MentionAutocomplete } from "./mention-autocomplete";

interface CommentFormProps {
  onSubmit: (content: string) => Promise<void>;
  placeholder?: string;
  isPending?: boolean;
  projectId: string;
}

export function CommentForm({
  onSubmit,
  placeholder = "Write a comment... Use @ to mention someone",
  isPending = false,
  projectId,
}: CommentFormProps) {
  const [content, setContent] = useState("");
  const [showMentions, setShowMentions] = useState(false);
  const [mentionQuery, setMentionQuery] = useState("");
  const [cursorPos, setCursorPos] = useState(0);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  const handleChange = useCallback(
    (e: React.ChangeEvent<HTMLTextAreaElement>) => {
      const val = e.target.value;
      const pos = e.target.selectionStart ?? 0;
      setContent(val);
      setCursorPos(pos);

      // Check if we're typing an @mention
      const textBeforeCursor = val.slice(0, pos);
      const mentionMatch = textBeforeCursor.match(/@([a-zA-Z0-9._-]*)$/);
      if (mentionMatch) {
        setMentionQuery(mentionMatch[1]);
        setShowMentions(true);
      } else {
        setShowMentions(false);
      }
    },
    [],
  );

  function handleMentionSelect(userName: string) {
    const textBeforeCursor = content.slice(0, cursorPos);
    const mentionMatch = textBeforeCursor.match(/@([a-zA-Z0-9._-]*)$/);
    if (mentionMatch) {
      const beforeMention = textBeforeCursor.slice(
        0,
        mentionMatch.index! + 1,
      );
      const afterCursor = content.slice(cursorPos);
      setContent(`${beforeMention}${userName} ${afterCursor}`);
    }
    setShowMentions(false);
    textareaRef.current?.focus();
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!content.trim() || isPending) return;
    await onSubmit(content.trim());
    setContent("");
  }

  return (
    <form onSubmit={handleSubmit} style={{ position: "relative" }}>
      <div
        style={{
          background: "var(--tf-bg4)",
          border: "1px solid var(--tf-border)",
          borderRadius: 8,
          overflow: "hidden",
          transition: "border-color var(--tf-tr)",
        }}
      >
        <textarea
          ref={textareaRef}
          value={content}
          onChange={handleChange}
          placeholder={placeholder}
          rows={3}
          style={{
            width: "100%",
            padding: "10px 12px",
            background: "transparent",
            border: "none",
            outline: "none",
            fontSize: 13,
            color: "var(--tf-text)",
            fontFamily: "var(--tf-font-body)",
            resize: "vertical",
            minHeight: 60,
          }}
        />
        <div
          style={{
            display: "flex",
            justifyContent: "flex-end",
            padding: "6px 8px",
            borderTop: "1px solid var(--tf-border)",
          }}
        >
          <button
            type="submit"
            disabled={!content.trim() || isPending}
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 5,
              padding: "5px 14px",
              borderRadius: 6,
              fontSize: 12,
              fontWeight: 600,
              border: "none",
              background:
                content.trim() && !isPending
                  ? "var(--tf-accent)"
                  : "var(--tf-bg3)",
              color:
                content.trim() && !isPending
                  ? "var(--tf-bg)"
                  : "var(--tf-text3)",
              cursor:
                content.trim() && !isPending ? "pointer" : "not-allowed",
              fontFamily: "var(--tf-font-body)",
              transition: "all var(--tf-tr)",
            }}
          >
            <Send size={12} />
            {isPending ? "Posting..." : "Comment"}
          </button>
        </div>
      </div>

      {showMentions && (
        <MentionAutocomplete
          projectId={projectId}
          query={mentionQuery}
          onSelect={handleMentionSelect}
          onClose={() => setShowMentions(false)}
        />
      )}
    </form>
  );
}
