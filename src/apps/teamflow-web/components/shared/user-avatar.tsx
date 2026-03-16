interface UserAvatarProps {
  initials: string;
  name?: string;
  subtitle?: string;
  size?: "xs" | "sm" | "md";
  colorIndex?: number;
}

const AVATAR_GRADIENTS = [
  "linear-gradient(135deg, var(--tf-violet), var(--tf-blue))",
  "linear-gradient(135deg, var(--tf-blue), var(--tf-accent))",
  "linear-gradient(135deg, var(--tf-orange), var(--tf-yellow))",
  "linear-gradient(135deg, var(--tf-red), var(--tf-violet))",
  "linear-gradient(135deg, var(--tf-accent), var(--tf-blue))",
];

function getGradient(initials: string, index?: number): string {
  if (index !== undefined) return AVATAR_GRADIENTS[index % AVATAR_GRADIENTS.length];
  // Deterministic color based on initials
  const code = initials.charCodeAt(0) + (initials.charCodeAt(1) || 0);
  return AVATAR_GRADIENTS[code % AVATAR_GRADIENTS.length];
}

const SIZE_MAP = {
  xs: { width: 18, height: 18, fontSize: 8 },
  sm: { width: 28, height: 28, fontSize: 10 },
  md: { width: 36, height: 36, fontSize: 13 },
};

export function formatAssignedAt(isoDate: string | null): string | undefined {
  if (!isoDate) return undefined;
  return `Assigned ${new Date(isoDate).toLocaleString("en-AU", {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  })}`;
}

export function UserAvatar({ initials, name, subtitle, size = "md", colorIndex }: UserAvatarProps) {
  const dims = SIZE_MAP[size];
  const displayInitials = initials.slice(0, 2).toUpperCase();

  const tooltipText =
    name != null
      ? subtitle
        ? `${name}\n${subtitle}`
        : name
      : displayInitials;

  return (
    <div
      title={tooltipText}
      style={{
        width: dims.width,
        height: dims.height,
        borderRadius: "50%",
        background: getGradient(displayInitials, colorIndex),
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        fontSize: dims.fontSize,
        fontWeight: 700,
        color: "white",
        flexShrink: 0,
        userSelect: "none",
      }}
    >
      {displayInitials}
    </div>
  );
}
