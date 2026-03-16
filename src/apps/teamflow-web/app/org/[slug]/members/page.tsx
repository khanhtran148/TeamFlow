"use client";

import { useParams } from "next/navigation";
import { Users } from "lucide-react";
import { TopBar } from "@/components/layout/top-bar";
import { Breadcrumb } from "@/components/layout/breadcrumb";
import { OrgSwitcher } from "@/components/layout/org-switcher";
import { MemberList } from "@/components/org-members/member-list";
import { LoadingSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { useOrgContext } from "@/lib/contexts/org-context";
import { useOrgMembers } from "@/lib/hooks/use-org-members";
import { useAuthStore } from "@/lib/stores/auth-store";

export default function OrgMembersPage() {
  const params = useParams();
  const slug = params.slug as string;
  const { org, myOrg } = useOrgContext();
  const user = useAuthStore((s) => s.user);

  const { data: members, isLoading, isError } = useOrgMembers(org.id);

  const breadcrumb = (
    <Breadcrumb
      segments={[
        { label: org.name, href: `/org/${slug}/projects` },
        { label: "Members", bold: true },
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
        <div style={{ maxWidth: 860, margin: "0 auto" }}>
          <div
            style={{
              display: "flex",
              alignItems: "center",
              justifyContent: "space-between",
              marginBottom: 20,
              gap: 12,
            }}
          >
            <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
              <h1
                style={{
                  fontFamily: "var(--tf-font-head)",
                  fontWeight: 700,
                  fontSize: 22,
                  color: "var(--tf-text)",
                }}
              >
                Members
              </h1>
              {!isLoading && members && (
                <span
                  style={{
                    fontSize: 13,
                    color: "var(--tf-text3)",
                    fontFamily: "var(--tf-font-mono)",
                    background: "var(--tf-bg3)",
                    border: "1px solid var(--tf-border)",
                    borderRadius: 100,
                    padding: "2px 8px",
                  }}
                >
                  {members.length}
                </span>
              )}
            </div>

            {myOrg?.role && (
              <div
                style={{
                  fontSize: 12,
                  color: "var(--tf-text3)",
                  fontFamily: "var(--tf-font-mono)",
                  padding: "4px 10px",
                  borderRadius: 100,
                  background: "var(--tf-bg3)",
                  border: "1px solid var(--tf-border)",
                }}
              >
                Your role: <strong style={{ color: "var(--tf-text2)" }}>{myOrg.role}</strong>
              </div>
            )}
          </div>

          {isLoading ? (
            <div
              style={{
                background: "var(--tf-bg2)",
                border: "1px solid var(--tf-border)",
                borderRadius: "var(--tf-radius)",
                padding: 12,
              }}
            >
              <LoadingSkeleton rows={5} />
            </div>
          ) : isError ? (
            <EmptyState
              title="Failed to load members"
              description="Check your connection and try again."
            />
          ) : !members || members.length === 0 ? (
            <EmptyState
              icon={Users}
              title="No members yet"
              description="This organization has no members."
            />
          ) : (
            <MemberList
              orgId={org.id}
              members={members}
              currentUserId={user?.id ?? ""}
              currentUserRole={myOrg?.role}
            />
          )}
        </div>
      </main>
    </div>
  );
}
