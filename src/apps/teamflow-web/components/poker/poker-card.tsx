"use client";

interface PokerCardProps {
  value: number;
  isSelected: boolean;
  onClick: () => void;
  disabled?: boolean;
}

export function PokerCard({
  value,
  isSelected,
  onClick,
  disabled = false,
}: PokerCardProps) {
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      style={{
        width: 56,
        height: 76,
        borderRadius: 8,
        border: `2px solid ${isSelected ? "var(--tf-accent)" : "var(--tf-border)"}`,
        background: isSelected ? "var(--tf-accent-dim2)" : "var(--tf-bg2)",
        color: isSelected ? "var(--tf-accent)" : "var(--tf-text)",
        fontSize: 18,
        fontWeight: 700,
        fontFamily: "var(--tf-font-mono)",
        cursor: disabled ? "not-allowed" : "pointer",
        opacity: disabled ? 0.4 : 1,
        transition: "all var(--tf-tr)",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        boxShadow: isSelected ? "0 2px 8px rgba(0,0,0,0.1)" : "none",
        transform: isSelected ? "translateY(-4px)" : "none",
      }}
      onMouseEnter={(e) => {
        if (!disabled && !isSelected) {
          (e.currentTarget as HTMLButtonElement).style.borderColor =
            "var(--tf-accent)";
          (e.currentTarget as HTMLButtonElement).style.transform =
            "translateY(-2px)";
        }
      }}
      onMouseLeave={(e) => {
        if (!disabled && !isSelected) {
          (e.currentTarget as HTMLButtonElement).style.borderColor =
            "var(--tf-border)";
          (e.currentTarget as HTMLButtonElement).style.transform = "none";
        }
      }}
    >
      {value}
    </button>
  );
}
