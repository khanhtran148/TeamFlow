"use client";

import { useState } from "react";
import { Pencil } from "lucide-react";
import { ApiError } from "@/lib/api/client";
import type { AdminOrganizationDto, AdminUpdateOrgRequest } from "@/lib/api/types";

interface EditOrgDialogProps {
  org: AdminOrganizationDto;
  onConfirm: (orgId: string, body: AdminUpdateOrgRequest) => Promise<void>;
  onClose: () => void;
}

export function EditOrgDialog({ org, onConfirm, onClose }: EditOrgDialogProps) {
  const [name, setName] = useState(org.name);
  const [slug, setSlug] = useState(org.slug);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const slugPattern = /^[a-z0-9-]+$/;

  function handleSlugChange(value: string) {
    // Auto-lowercase and sanitize
    setSlug(value.toLowerCase().replace(/[^a-z0-9-]/g, ""));
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);

    if (!name.trim()) {
      setError("Name is required.");
      return;
    }
    if (name.trim().length > 100) {
      setError("Name must be at most 100 characters.");
      return;
    }
    if (!slug.trim()) {
      setError("Slug is required.");
      return;
    }
    if (slug.length > 50) {
      setError("Slug must be at most 50 characters.");
      return;
    }
    if (!slugPattern.test(slug)) {
      setError("Slug can only contain lowercase letters, digits, and hyphens.");
      return;
    }

    setLoading(true);
    try {
      await onConfirm(org.id, { name: name.trim(), slug });
      onClose();
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.problem.detail ?? err.problem.title);
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("An unexpected error occurred.");
      }
    } finally {
      setLoading(false);
    }
  }

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="edit-org-title"
      style={{
        position: "fixed",
        inset: 0,
        zIndex: 1000,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        padding: 24,
      }}
    >
      {/* Backdrop */}
      <div
        onClick={onClose}
        aria-hidden="true"
        style={{
          position: "absolute",
          inset: 0,
          background: "rgba(0,0,0,0.6)",
          backdropFilter: "blur(2px)",
        }}
      />

      {/* Dialog */}
      <div
        style={{
          position: "relative",
          width: "100%",
          maxWidth: 420,
          background: "var(--tf-bg2)",
          border: "1px solid var(--tf-border)",
          borderRadius: 12,
          padding: 24,
          zIndex: 1,
        }}
      >
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 10,
            marginBottom: 20,
          }}
        >
          <div
            style={{
              width: 32,
              height: 32,
              borderRadius: 8,
              background: "var(--tf-accent-dim)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
            }}
          >
            <Pencil size={15} color="var(--tf-accent)" />
          </div>
          <h2
            id="edit-org-title"
            style={{
              fontSize: 15,
              fontWeight: 600,
              color: "var(--tf-text)",
              fontFamily: "var(--tf-font-head)",
              margin: 0,
            }}
          >
            Edit Organization
          </h2>
        </div>

        <form
          onSubmit={handleSubmit}
          style={{ display: "flex", flexDirection: "column", gap: 14 }}
        >
          <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
            <label
              htmlFor="edit-org-name"
              style={{
                fontSize: 12,
                fontWeight: 500,
                color: "var(--tf-text2)",
                fontFamily: "var(--tf-font-body)",
              }}
            >
              Name
            </label>
            <input
              id="edit-org-name"
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Organization name"
              required
              autoFocus
              maxLength={100}
              style={{
                padding: "9px 12px",
                borderRadius: 8,
                border: "1px solid var(--tf-border)",
                background: "var(--tf-bg3)",
                color: "var(--tf-text)",
                fontSize: 14,
                fontFamily: "var(--tf-font-body)",
                outline: "none",
                transition: "border-color 0.15s",
              }}
              onFocus={(e) => {
                e.currentTarget.style.borderColor = "var(--tf-accent)";
              }}
              onBlur={(e) => {
                e.currentTarget.style.borderColor = "var(--tf-border)";
              }}
            />
          </div>

          <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
            <label
              htmlFor="edit-org-slug"
              style={{
                fontSize: 12,
                fontWeight: 500,
                color: "var(--tf-text2)",
                fontFamily: "var(--tf-font-body)",
              }}
            >
              Slug
            </label>
            <input
              id="edit-org-slug"
              type="text"
              value={slug}
              onChange={(e) => handleSlugChange(e.target.value)}
              placeholder="e.g. my-org"
              required
              maxLength={50}
              style={{
                padding: "9px 12px",
                borderRadius: 8,
                border: "1px solid var(--tf-border)",
                background: "var(--tf-bg3)",
                color: "var(--tf-text)",
                fontSize: 14,
                fontFamily: "var(--tf-font-mono)",
                outline: "none",
                transition: "border-color 0.15s",
              }}
              onFocus={(e) => {
                e.currentTarget.style.borderColor = "var(--tf-accent)";
              }}
              onBlur={(e) => {
                e.currentTarget.style.borderColor = "var(--tf-border)";
              }}
            />
            <span
              style={{
                fontSize: 11,
                color: "var(--tf-text3)",
                fontFamily: "var(--tf-font-mono)",
              }}
            >
              Lowercase letters, digits, and hyphens only
            </span>
          </div>

          {error && (
            <div
              role="alert"
              style={{
                padding: "8px 12px",
                borderRadius: 6,
                background: "rgba(248, 113, 113, 0.1)",
                border: "1px solid rgba(248, 113, 113, 0.3)",
                color: "var(--tf-red)",
                fontSize: 12,
                fontFamily: "var(--tf-font-body)",
              }}
            >
              {error}
            </div>
          )}

          <div style={{ display: "flex", gap: 8, marginTop: 4 }}>
            <button
              type="button"
              onClick={onClose}
              disabled={loading}
              style={{
                flex: 1,
                padding: "8px 0",
                borderRadius: 6,
                border: "1px solid var(--tf-border)",
                background: "transparent",
                color: "var(--tf-text2)",
                fontSize: 13,
                fontFamily: "var(--tf-font-body)",
                cursor: loading ? "not-allowed" : "pointer",
                minHeight: 36,
              }}
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={loading}
              style={{
                flex: 1,
                padding: "8px 0",
                borderRadius: 6,
                border: "none",
                background:
                  "linear-gradient(135deg, var(--tf-accent), var(--tf-blue))",
                color: "#0a0a0b",
                fontSize: 13,
                fontWeight: 600,
                fontFamily: "var(--tf-font-body)",
                cursor: loading ? "not-allowed" : "pointer",
                opacity: loading ? 0.7 : 1,
                minHeight: 36,
              }}
            >
              {loading ? "Saving..." : "Save Changes"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
