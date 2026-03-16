"use client";

import { useState, useRef, useEffect } from "react";
import Link from "next/link";
import { ChevronDown, Building2, Plus } from "lucide-react";
import { useMyOrganizations } from "@/lib/hooks/use-organizations";
import { useAuthStore } from "@/lib/stores/auth-store";

interface OrgSwitcherProps {
  currentSlug: string;
}

export function OrgSwitcher({ currentSlug }: OrgSwitcherProps) {
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const { data: orgs, isLoading } = useMyOrganizations();
  const user = useAuthStore((s) => s.user);

  const currentOrg = orgs?.find((o) => o.slug === currentSlug);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  if (isLoading) return null;

  return (
    <div ref={containerRef} style={{ position: "relative" }}>
      <button
        onClick={() => setOpen((v) => !v)}
        style={{
          display: "flex",
          alignItems: "center",
          gap: 6,
          padding: "5px 10px",
          borderRadius: 6,
          border: "1px solid var(--tf-border)",
          background: open ? "var(--tf-bg3)" : "transparent",
          color: "var(--tf-text2)",
          fontSize: 13,
          fontWeight: 500,
          cursor: "pointer",
          fontFamily: "var(--tf-font-body)",
          transition: "all var(--tf-tr)",
        }}
        onMouseEnter={(e) => {
          if (!open) (e.currentTarget as HTMLButtonElement).style.background = "var(--tf-bg3)";
        }}
        onMouseLeave={(e) => {
          if (!open) (e.currentTarget as HTMLButtonElement).style.background = "transparent";
        }}
      >
        <Building2 size={13} color="var(--tf-accent)" />
        <span style={{ maxWidth: 120, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
          {currentOrg?.name ?? currentSlug}
        </span>
        <ChevronDown
          size={11}
          style={{ transform: open ? "rotate(180deg)" : "none", transition: "transform var(--tf-tr)" }}
        />
      </button>

      {open && (
        <div
          style={{
            position: "absolute",
            top: "calc(100% + 4px)",
            right: 0,
            minWidth: 200,
            background: "var(--tf-bg2)",
            border: "1px solid var(--tf-border)",
            borderRadius: "var(--tf-radius)",
            boxShadow: "var(--tf-shadow)",
            zIndex: 100,
            overflow: "hidden",
          }}
        >
          <div
            style={{
              padding: "8px 12px 6px",
              fontSize: 11,
              fontWeight: 600,
              color: "var(--tf-text3)",
              fontFamily: "var(--tf-font-mono)",
              letterSpacing: "0.05em",
              textTransform: "uppercase",
            }}
          >
            Your Organizations
          </div>

          {orgs?.map((org) => {
            const isActive = org.slug === currentSlug;
            return (
              <Link
                key={org.id}
                href={`/org/${org.slug}/projects`}
                onClick={() => setOpen(false)}
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: 8,
                  padding: "8px 12px",
                  fontSize: 13,
                  color: isActive ? "var(--tf-accent)" : "var(--tf-text2)",
                  fontWeight: isActive ? 600 : 400,
                  background: isActive ? "var(--tf-accent-dim)" : "transparent",
                  textDecoration: "none",
                  transition: "background var(--tf-tr)",
                }}
                onMouseEnter={(e) => {
                  if (!isActive) (e.currentTarget as HTMLAnchorElement).style.background = "var(--tf-bg3)";
                }}
                onMouseLeave={(e) => {
                  if (!isActive) (e.currentTarget as HTMLAnchorElement).style.background = "transparent";
                }}
              >
                <Building2 size={13} />
                <span style={{ flex: 1, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
                  {org.name}
                </span>
                {isActive && (
                  <span
                    style={{
                      width: 6,
                      height: 6,
                      borderRadius: "50%",
                      background: "var(--tf-accent)",
                      flexShrink: 0,
                    }}
                  />
                )}
              </Link>
            );
          })}

          {!orgs?.length && (
            <div
              style={{
                padding: "8px 12px",
                fontSize: 13,
                color: "var(--tf-text3)",
                fontStyle: "italic",
              }}
            >
              No organizations
            </div>
          )}

          {user?.systemRole === "SystemAdmin" && (
            <>
              <div
                style={{
                  height: 1,
                  background: "var(--tf-border)",
                  margin: "4px 0",
                }}
              />
              <Link
                href="/admin/organizations"
                onClick={() => setOpen(false)}
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: 8,
                  padding: "8px 12px",
                  fontSize: 13,
                  color: "var(--tf-text3)",
                  textDecoration: "none",
                  transition: "background var(--tf-tr)",
                }}
                onMouseEnter={(e) => {
                  (e.currentTarget as HTMLAnchorElement).style.background = "var(--tf-bg3)";
                }}
                onMouseLeave={(e) => {
                  (e.currentTarget as HTMLAnchorElement).style.background = "transparent";
                }}
              >
                <Plus size={13} />
                Create Organization
              </Link>
            </>
          )}

          <div style={{ height: 1, background: "var(--tf-border)", margin: "4px 0" }} />
          <Link
            href="/onboarding/pick-org"
            onClick={() => setOpen(false)}
            style={{
              display: "flex",
              alignItems: "center",
              gap: 8,
              padding: "8px 12px",
              fontSize: 12,
              color: "var(--tf-text3)",
              textDecoration: "none",
              transition: "background var(--tf-tr)",
            }}
            onMouseEnter={(e) => {
              (e.currentTarget as HTMLAnchorElement).style.background = "var(--tf-bg3)";
            }}
            onMouseLeave={(e) => {
              (e.currentTarget as HTMLAnchorElement).style.background = "transparent";
            }}
          >
            All organizations
          </Link>
        </div>
      )}
    </div>
  );
}
