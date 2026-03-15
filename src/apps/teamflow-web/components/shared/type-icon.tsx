import type { WorkItemType } from "@/lib/api/types";

interface TypeIconProps {
  type: WorkItemType;
  size?: number;
}

const TYPE_CONFIG: Record<
  WorkItemType,
  { label: string; bg: string; color: string }
> = {
  Epic: {
    label: "E",
    bg: "var(--tf-orange-dim)",
    color: "var(--tf-orange)",
  },
  UserStory: {
    label: "S",
    bg: "var(--tf-blue-dim)",
    color: "var(--tf-blue)",
  },
  Task: {
    label: "T",
    bg: "var(--tf-accent-dim2)",
    color: "var(--tf-accent)",
  },
  Bug: {
    label: "B",
    bg: "var(--tf-red-dim)",
    color: "var(--tf-red)",
  },
  Spike: {
    label: "Sp",
    bg: "var(--tf-violet-dim)",
    color: "var(--tf-violet)",
  },
};

export function TypeIcon({ type, size = 18 }: TypeIconProps) {
  const config = TYPE_CONFIG[type];

  return (
    <div
      title={type}
      style={{
        width: size,
        height: size,
        borderRadius: 3,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        fontSize: 9,
        fontWeight: 700,
        fontFamily: "var(--tf-font-mono)",
        background: config.bg,
        color: config.color,
        flexShrink: 0,
      }}
    >
      {config.label}
    </div>
  );
}
