"use client";

import { type ReactNode, useEffect } from "react";
import { useOrganizationBySlug, useMyOrganizations } from "@/lib/hooks/use-organizations";
import { OrgProvider } from "@/lib/contexts/org-context";
import { useOrgStore } from "@/lib/stores/org-store";
import { LoadingSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import Link from "next/link";

interface OrgLayoutClientProps {
  slug: string;
  children: ReactNode;
}

export function OrgLayoutClient({ slug, children }: OrgLayoutClientProps) {
  const { data: org, isLoading, isError } = useOrganizationBySlug(slug);
  const { data: myOrgs } = useMyOrganizations();
  const setCurrentSlug = useOrgStore((s) => s.setCurrentSlug);
  const setMyOrgs = useOrgStore((s) => s.setMyOrgs);

  useEffect(() => {
    setCurrentSlug(slug);
    return () => setCurrentSlug(null);
  }, [slug, setCurrentSlug]);

  useEffect(() => {
    if (myOrgs) {
      setMyOrgs(myOrgs);
    }
  }, [myOrgs, setMyOrgs]);

  if (isLoading) {
    return (
      <div
        style={{
          display: "flex",
          flexDirection: "column",
          height: "100vh",
          overflow: "hidden",
          background: "var(--tf-bg)",
          padding: 20,
        }}
      >
        <LoadingSkeleton rows={5} type="list-row" />
      </div>
    );
  }

  if (isError || !org) {
    return (
      <div
        style={{
          display: "flex",
          flexDirection: "column",
          height: "100vh",
          overflow: "hidden",
          background: "var(--tf-bg)",
          alignItems: "center",
          justifyContent: "center",
        }}
      >
        <EmptyState
          title="Organization not found"
          description="This organization does not exist or you don't have access."
          action={
            <Link
              href="/onboarding"
              style={{ fontSize: 13, color: "var(--tf-accent)", textDecoration: "none" }}
            >
              Back to Home
            </Link>
          }
        />
      </div>
    );
  }

  const myOrg = myOrgs?.find((o) => o.slug === slug);

  return (
    <OrgProvider org={org} myOrg={myOrg}>
      {children}
    </OrgProvider>
  );
}
