"use client";

import { Search, X } from "lucide-react";

interface SearchInputProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
}

export function SearchInput({
  value,
  onChange,
  placeholder = "Search...",
}: SearchInputProps) {
  return (
    <div
      style={{
        position: "relative",
        display: "flex",
        alignItems: "center",
      }}
    >
      <Search
        size={14}
        color="var(--tf-text3)"
        style={{
          position: "absolute",
          left: 10,
          pointerEvents: "none",
        }}
      />
      <input
        type="search"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        aria-label={placeholder}
        style={{
          padding: "7px 32px 7px 32px",
          borderRadius: 6,
          border: "1px solid var(--tf-border)",
          background: "var(--tf-bg3)",
          color: "var(--tf-text)",
          fontSize: 13,
          fontFamily: "var(--tf-font-body)",
          outline: "none",
          width: 220,
          transition: "border-color 0.15s",
        }}
        onFocus={(e) => {
          e.currentTarget.style.borderColor = "var(--tf-accent)";
        }}
        onBlur={(e) => {
          e.currentTarget.style.borderColor = "var(--tf-border)";
        }}
      />
      {value && (
        <button
          type="button"
          onClick={() => onChange("")}
          aria-label="Clear search"
          style={{
            position: "absolute",
            right: 8,
            background: "transparent",
            border: "none",
            cursor: "pointer",
            padding: 2,
            display: "flex",
            alignItems: "center",
            color: "var(--tf-text3)",
          }}
        >
          <X size={12} />
        </button>
      )}
    </div>
  );
}
