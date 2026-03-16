"use client";

import { useState } from "react";
import { changePassword } from "@/lib/api/auth";
import { ApiError } from "@/lib/api/client";

export function ProfileSecurity() {
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const confirmMismatch =
    confirmPassword.length > 0 && newPassword !== confirmPassword;

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (newPassword !== confirmPassword) {
      setErrorMsg("New passwords do not match.");
      return;
    }
    setIsSubmitting(true);
    setSuccessMsg(null);
    setErrorMsg(null);

    try {
      await changePassword({ currentPassword, newPassword });
      setSuccessMsg("Password changed successfully.");
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
    } catch (err) {
      if (err instanceof ApiError) {
        setErrorMsg(err.message);
      } else {
        setErrorMsg("Failed to change password. Please try again.");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  const inputStyle: React.CSSProperties = {
    background: "var(--tf-bg3)",
    border: "1px solid var(--tf-border)",
    borderRadius: 6,
    padding: "8px 12px",
    fontSize: 14,
    color: "var(--tf-text)",
    outline: "none",
    width: "100%",
    maxWidth: 360,
    boxSizing: "border-box",
  };

  const labelStyle: React.CSSProperties = {
    display: "flex",
    flexDirection: "column",
    gap: 5,
  };

  const labelTextStyle: React.CSSProperties = {
    fontSize: 11,
    fontWeight: 600,
    color: "var(--tf-text3)",
    textTransform: "uppercase",
    letterSpacing: "0.05em",
  };

  return (
    <div style={{ maxWidth: 420 }}>
      <h3
        style={{
          fontFamily: "var(--tf-font-head)",
          fontWeight: 700,
          fontSize: 16,
          color: "var(--tf-text)",
          marginBottom: 20,
        }}
      >
        Change Password
      </h3>

      <form
        onSubmit={handleSubmit}
        noValidate
        style={{ display: "flex", flexDirection: "column", gap: 16 }}
      >
        <label style={labelStyle}>
          <span style={labelTextStyle}>Current password</span>
          <input
            type="password"
            aria-label="Current password"
            required
            value={currentPassword}
            onChange={(e) => setCurrentPassword(e.target.value)}
            autoComplete="current-password"
            style={inputStyle}
          />
        </label>

        <label style={labelStyle}>
          <span style={labelTextStyle}>New password</span>
          <input
            type="password"
            aria-label="New password"
            required
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            autoComplete="new-password"
            style={inputStyle}
          />
        </label>

        <label style={labelStyle}>
          <span style={labelTextStyle}>Confirm new password</span>
          <input
            type="password"
            aria-label="Confirm new password"
            required
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
            autoComplete="new-password"
            style={{
              ...inputStyle,
              borderColor: confirmMismatch ? "#ef4444" : "var(--tf-border)",
            }}
          />
          {confirmMismatch && (
            <span style={{ fontSize: 12, color: "#ef4444" }}>
              Passwords do not match.
            </span>
          )}
        </label>

        {successMsg && (
          <div
            role="status"
            aria-live="polite"
            style={{
              padding: "10px 14px",
              background: "rgba(34,197,94,0.1)",
              border: "1px solid rgba(34,197,94,0.3)",
              borderRadius: 6,
              fontSize: 13,
              color: "#22c55e",
            }}
          >
            {successMsg}
          </div>
        )}

        {errorMsg && (
          <div
            role="alert"
            style={{
              padding: "10px 14px",
              background: "rgba(239,68,68,0.08)",
              border: "1px solid rgba(239,68,68,0.3)",
              borderRadius: 6,
              fontSize: 13,
              color: "#ef4444",
            }}
          >
            {errorMsg}
          </div>
        )}

        <button
          type="submit"
          disabled={
            isSubmitting ||
            !currentPassword ||
            !newPassword ||
            !confirmPassword ||
            confirmMismatch
          }
          aria-label="Change password"
          style={{
            padding: "9px 20px",
            borderRadius: 6,
            border: "none",
            background: "var(--tf-accent)",
            color: "#fff",
            fontSize: 13,
            fontWeight: 600,
            cursor:
              isSubmitting ||
              !currentPassword ||
              !newPassword ||
              !confirmPassword ||
              confirmMismatch
                ? "not-allowed"
                : "pointer",
            opacity:
              isSubmitting ||
              !currentPassword ||
              !newPassword ||
              !confirmPassword ||
              confirmMismatch
                ? 0.6
                : 1,
            alignSelf: "flex-start",
            minHeight: 44,
            minWidth: 120,
          }}
        >
          {isSubmitting ? "Saving..." : "Change password"}
        </button>
      </form>
    </div>
  );
}
