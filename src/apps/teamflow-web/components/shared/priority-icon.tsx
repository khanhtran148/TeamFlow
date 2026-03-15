import type { Priority } from "@/lib/api/types";

interface PriorityIconProps {
  priority: Priority | null | undefined;
  size?: number;
  showLabel?: boolean;
}

const PRIORITY_CONFIG: Record<
  Priority,
  { symbol: string; color: string; label: string }
> = {
  Critical: { symbol: "↑↑", color: "var(--tf-red)", label: "Critical" },
  High: { symbol: "↑", color: "var(--tf-orange)", label: "High" },
  Medium: { symbol: "→", color: "var(--tf-yellow)", label: "Medium" },
  Low: { symbol: "↓", color: "var(--tf-text3)", label: "Low" },
};

export function PriorityIcon({ priority, showLabel = false }: PriorityIconProps) {
  if (!priority) return null;

  const config = PRIORITY_CONFIG[priority];

  return (
    <span
      title={config.label}
      style={{
        color: config.color,
        fontSize: 11,
        fontFamily: "var(--tf-font-body)",
        display: "inline-flex",
        alignItems: "center",
        gap: 3,
      }}
    >
      {config.symbol}
      {showLabel && (
        <span style={{ fontSize: 10 }}>{config.label}</span>
      )}
    </span>
  );
}
