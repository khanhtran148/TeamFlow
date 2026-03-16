"use client";

import { CheckCircle2 } from "lucide-react";

interface ReadyBadgeProps {
  isReady: boolean;
  onClick?: () => void;
  interactive?: boolean;
}

export function ReadyBadge({
  isReady,
  onClick,
  interactive = false,
}: ReadyBadgeProps) {
  if (!isReady && !interactive) return null;

  const Component = interactive ? "button" : "span";

  return (
    <Component
      onClick={interactive ? onClick : undefined}
      title={isReady ? "Ready for sprint" : "Mark as ready for sprint"}
      style={{
        display: "inline-flex",
        alignItems: "center",
        gap: 3,
        padding: "2px 8px",
        borderRadius: 100,
        fontSize: 13,
        fontWeight: 600,
        fontFamily: "var(--tf-font-body)",
        background: isReady ? "var(--tf-accent-dim2)" : "var(--tf-bg4)",
        color: isReady ? "var(--tf-accent)" : "var(--tf-text3)",
        border: `1px solid ${isReady ? "var(--tf-accent)" : "var(--tf-border)"}`,
        cursor: interactive ? "pointer" : "default",
        transition: "all var(--tf-tr)",
        whiteSpace: "nowrap" as const,
      }}
    >
      <CheckCircle2 size={10} />
      Ready
    </Component>
  );
}
