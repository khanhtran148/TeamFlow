"use client";

import { type InputHTMLAttributes, useId } from "react";

interface AuthInputProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string;
}

export function AuthInput({ label, ...props }: AuthInputProps) {
  const id = useId();

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
      <label
        htmlFor={id}
        style={{
          fontSize: 13,
          fontWeight: 500,
          color: "var(--tf-text2)",
        }}
      >
        {label}
      </label>
      <input
        id={id}
        {...props}
        style={{
          padding: "9px 12px",
          borderRadius: 8,
          border: "1px solid var(--tf-border)",
          background: "var(--tf-bg3)",
          color: "var(--tf-text)",
          fontSize: 14,
          outline: "none",
          transition: "border-color 0.2s",
          ...props.style,
        }}
        onFocus={(e) => {
          e.currentTarget.style.borderColor = "var(--tf-accent)";
          props.onFocus?.(e);
        }}
        onBlur={(e) => {
          e.currentTarget.style.borderColor = "var(--tf-border)";
          props.onBlur?.(e);
        }}
      />
    </div>
  );
}
