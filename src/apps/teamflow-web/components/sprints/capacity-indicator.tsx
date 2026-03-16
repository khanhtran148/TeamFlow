"use client";

interface CapacityIndicatorProps {
  assignedPoints: number;
  totalCapacity: number;
  label?: string;
}

export function CapacityIndicator({
  assignedPoints,
  totalCapacity,
  label = "Capacity",
}: CapacityIndicatorProps) {
  const percentage = totalCapacity > 0 ? (assignedPoints / totalCapacity) * 100 : 0;
  const isOver = assignedPoints > totalCapacity;
  const isWarning = percentage >= 80 && !isOver;

  const barColor = isOver
    ? "var(--tf-red)"
    : isWarning
      ? "var(--tf-yellow)"
      : "var(--tf-accent)";

  const textColor = isOver
    ? "var(--tf-red)"
    : isWarning
      ? "var(--tf-yellow)"
      : "var(--tf-text2)";

  return (
    <div
      role="meter"
      aria-label={label}
      aria-valuenow={assignedPoints}
      aria-valuemin={0}
      aria-valuemax={totalCapacity}
      style={{ display: "flex", flexDirection: "column", gap: 5 }}
    >
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <span
          style={{
            fontSize: 13,
            color: "var(--tf-text3)",
            fontFamily: "var(--tf-font-mono)",
          }}
        >
          {label}
        </span>
        <span
          style={{
            fontSize: 13,
            color: textColor,
            fontFamily: "var(--tf-font-mono)",
            fontWeight: isOver ? 600 : 400,
          }}
        >
          {assignedPoints} / {totalCapacity} pts
          {isOver && " (Over!)"}
        </span>
      </div>
      <div
        style={{
          height: 6,
          borderRadius: 100,
          background: "var(--tf-bg4)",
          overflow: "hidden",
        }}
      >
        <div
          style={{
            height: "100%",
            width: `${Math.min(percentage, 100)}%`,
            borderRadius: 100,
            background: barColor,
            transition: "width 0.3s ease, background 0.3s ease",
          }}
        />
      </div>
      {isOver && (
        <p
          role="alert"
          style={{
            fontSize: 13,
            color: "var(--tf-red)",
            fontWeight: 500,
            margin: 0,
          }}
        >
          Assigned points exceed team capacity by {assignedPoints - totalCapacity} pts.
        </p>
      )}
    </div>
  );
}
