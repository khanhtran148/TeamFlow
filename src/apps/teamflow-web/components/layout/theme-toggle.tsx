"use client";

import { Moon, Sun } from "lucide-react";
import { useThemeStore } from "@/lib/stores/theme-store";

export function ThemeToggle() {
  const { theme, toggleTheme } = useThemeStore();

  return (
    <button
      data-testid="theme-toggle"
      onClick={toggleTheme}
      aria-label={`Switch to ${theme === "dark" ? "light" : "dark"} mode`}
      style={{
        width: 32,
        height: 32,
        borderRadius: 7,
        border: "1px solid var(--tf-border)",
        background: "var(--tf-bg3)",
        color: "var(--tf-text2)",
        cursor: "pointer",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        transition: "all var(--tf-tr)",
        flexShrink: 0,
      }}
      onMouseEnter={(e) => {
        const el = e.currentTarget;
        el.style.borderColor = "var(--tf-border2)";
        el.style.color = "var(--tf-text)";
      }}
      onMouseLeave={(e) => {
        const el = e.currentTarget;
        el.style.borderColor = "var(--tf-border)";
        el.style.color = "var(--tf-text2)";
      }}
    >
      {theme === "dark" ? (
        <Sun size={15} strokeWidth={1.5} />
      ) : (
        <Moon size={15} strokeWidth={1.5} />
      )}
    </button>
  );
}
