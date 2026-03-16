"use client";

import { useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { toast } from "sonner";
import { TopBar } from "@/components/layout/top-bar";
import { Breadcrumb } from "@/components/layout/breadcrumb";
import { OrgSwitcher } from "@/components/layout/org-switcher";
import { useOrgContext } from "@/lib/contexts/org-context";
import { useUpdateOrganization } from "@/lib/hooks/use-organizations";
import type { ApiError } from "@/lib/api/client";

export default function OrgSettingsPage() {
  const params = useParams();
  const router = useRouter();
  const slug = params.slug as string;
  const { org } = useOrgContext();

  const [name, setName] = useState(org.name);
  const [orgSlug, setOrgSlug] = useState(org.slug);
  const [nameError, setNameError] = useState("");
  const [slugError, setSlugError] = useState("");

  const { mutate: updateOrg, isPending } = useUpdateOrganization();

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();

    let hasError = false;
    const trimmedName = name.trim();
    const trimmedSlug = orgSlug.trim();

    if (!trimmedName) {
      setNameError("Name is required.");
      hasError = true;
    }
    if (!trimmedSlug) {
      setSlugError("Slug is required.");
      hasError = true;
    } else if (!/^[a-z0-9-]+$/.test(trimmedSlug)) {
      setSlugError("Slug can only contain lowercase letters, numbers, and hyphens.");
      hasError = true;
    }
    if (hasError) return;

    updateOrg(
      { id: org.id, data: { name: trimmedName, slug: trimmedSlug } },
      {
        onSuccess: () => {
          toast.success("Organization settings saved.");
          if (trimmedSlug !== slug) {
            router.replace(`/org/${trimmedSlug}/settings`);
          }
        },
        onError: (err) => {
          const apiErr = err as ApiError;
          toast.error(apiErr.message ?? "Failed to save settings.");
        },
      },
    );
  }

  const breadcrumb = (
    <Breadcrumb
      segments={[
        { label: org.name, href: `/org/${slug}/projects` },
        { label: "Settings", bold: true },
      ]}
    />
  );

  return (
    <div
      style={{
        display: "flex",
        flexDirection: "column",
        height: "100vh",
        overflow: "hidden",
        background: "var(--tf-bg)",
      }}
    >
      <TopBar breadcrumb={breadcrumb} actions={<OrgSwitcher currentSlug={slug} />} />

      <main style={{ flex: 1, overflow: "auto", padding: "24px 20px" }}>
        <div style={{ maxWidth: 480, margin: "0 auto" }}>
          <h1
            style={{
              fontFamily: "var(--tf-font-head)",
              fontWeight: 700,
              fontSize: 22,
              color: "var(--tf-text)",
              marginBottom: 24,
            }}
          >
            Organization Settings
          </h1>

          <div
            style={{
              background: "var(--tf-bg2)",
              border: "1px solid var(--tf-border)",
              borderRadius: "var(--tf-radius)",
              padding: 20,
            }}
          >
            <form onSubmit={handleSubmit} style={{ display: "flex", flexDirection: "column", gap: 16 }}>
              <div style={{ display: "flex", flexDirection: "column", gap: 5 }}>
                <label
                  htmlFor="org-name"
                  style={{ fontSize: 13, fontWeight: 500, color: "var(--tf-text2)" }}
                >
                  Name <span style={{ color: "var(--tf-red)" }}>*</span>
                </label>
                <input
                  id="org-name"
                  type="text"
                  value={name}
                  onChange={(e) => {
                    setName(e.target.value);
                    if (nameError) setNameError("");
                  }}
                  style={{
                    padding: "7px 10px",
                    borderRadius: 6,
                    border: `1px solid ${nameError ? "var(--tf-red)" : "var(--tf-border)"}`,
                    background: "var(--tf-bg3)",
                    color: "var(--tf-text)",
                    fontSize: 13,
                    outline: "none",
                    fontFamily: "var(--tf-font-body)",
                  }}
                  onFocus={(e) => {
                    if (!nameError) e.currentTarget.style.borderColor = "var(--tf-accent)";
                  }}
                  onBlur={(e) => {
                    if (!nameError) e.currentTarget.style.borderColor = "var(--tf-border)";
                  }}
                />
                {nameError && (
                  <span style={{ fontSize: 12, color: "var(--tf-red)" }}>{nameError}</span>
                )}
              </div>

              <div style={{ display: "flex", flexDirection: "column", gap: 5 }}>
                <label
                  htmlFor="org-slug"
                  style={{ fontSize: 13, fontWeight: 500, color: "var(--tf-text2)" }}
                >
                  Slug <span style={{ color: "var(--tf-red)" }}>*</span>
                </label>
                <input
                  id="org-slug"
                  type="text"
                  value={orgSlug}
                  onChange={(e) => {
                    setOrgSlug(e.target.value.toLowerCase());
                    if (slugError) setSlugError("");
                  }}
                  style={{
                    padding: "7px 10px",
                    borderRadius: 6,
                    border: `1px solid ${slugError ? "var(--tf-red)" : "var(--tf-border)"}`,
                    background: "var(--tf-bg3)",
                    color: "var(--tf-text)",
                    fontSize: 13,
                    outline: "none",
                    fontFamily: "var(--tf-font-mono)",
                  }}
                  onFocus={(e) => {
                    if (!slugError) e.currentTarget.style.borderColor = "var(--tf-accent)";
                  }}
                  onBlur={(e) => {
                    if (!slugError) e.currentTarget.style.borderColor = "var(--tf-border)";
                  }}
                />
                <span style={{ fontSize: 12, color: "var(--tf-text3)" }}>
                  Used in URLs: /org/<strong>{orgSlug || "your-org"}</strong>/projects
                </span>
                {slugError && (
                  <span style={{ fontSize: 12, color: "var(--tf-red)" }}>{slugError}</span>
                )}
              </div>

              <div style={{ display: "flex", justifyContent: "flex-end", marginTop: 4 }}>
                <button
                  type="submit"
                  disabled={isPending}
                  style={{
                    padding: "7px 20px",
                    borderRadius: 6,
                    border: "1px solid var(--tf-accent)",
                    background: "var(--tf-accent)",
                    color: "var(--primary-foreground)",
                    fontSize: 13,
                    fontWeight: 600,
                    cursor: isPending ? "not-allowed" : "pointer",
                    opacity: isPending ? 0.7 : 1,
                    fontFamily: "var(--tf-font-body)",
                  }}
                >
                  {isPending ? "Saving..." : "Save Changes"}
                </button>
              </div>
            </form>
          </div>
        </div>
      </main>
    </div>
  );
}
