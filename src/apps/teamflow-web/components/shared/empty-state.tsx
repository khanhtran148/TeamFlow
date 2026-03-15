import type { ReactNode } from "react";
import type { LucideIcon } from "lucide-react";

interface EmptyStateProps {
  icon?: LucideIcon;
  title: string;
  description?: string;
  action?: ReactNode;
}

export function EmptyState({ icon: Icon, title, description, action }: EmptyStateProps) {
  return (
    <div
      style={{
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        padding: "48px 24px",
        gap: 12,
        color: "var(--tf-text3)",
        textAlign: "center",
      }}
    >
      {Icon && (
        <div
          style={{
            width: 48,
            height: 48,
            borderRadius: 12,
            background: "var(--tf-bg3)",
            border: "1px solid var(--tf-border)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
          }}
        >
          <Icon size={22} strokeWidth={1.5} color="var(--tf-text3)" />
        </div>
      )}
      <div>
        <p
          style={{
            fontSize: 13,
            fontWeight: 500,
            color: "var(--tf-text2)",
            marginBottom: 4,
          }}
        >
          {title}
        </p>
        {description && (
          <p style={{ fontSize: 12, color: "var(--tf-text3)" }}>
            {description}
          </p>
        )}
      </div>
      {action && <div>{action}</div>}
    </div>
  );
}
