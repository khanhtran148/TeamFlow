"use client";

import { useQuery } from "@tanstack/react-query";
import { Building2 } from "lucide-react";
import { getAdminOrganizations } from "@/lib/api/admin";
import type { AdminOrganizationDto } from "@/lib/api/types";

export default function AdminOrganizationsPage() {
  const { data: orgs, isLoading, isError } = useQuery({
    queryKey: ["admin", "organizations"],
    queryFn: getAdminOrganizations,
  });

  return (
    <div style={{ maxWidth: 900 }}>
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 10,
          marginBottom: 24,
        }}
      >
        <Building2 size={18} color="var(--tf-accent)" />
        <h1
          style={{
            fontFamily: "var(--tf-font-head)",
            fontWeight: 700,
            fontSize: 22,
            color: "var(--tf-text)",
          }}
        >
          Organizations
        </h1>
        {!isLoading && orgs && (
          <span
            style={{
              fontSize: 12,
              color: "var(--tf-text3)",
              fontFamily: "var(--tf-font-mono)",
              marginLeft: "auto",
            }}
          >
            {orgs.length} total
          </span>
        )}
      </div>

      <div
        style={{
          background: "var(--tf-bg2)",
          border: "1px solid var(--tf-border)",
          borderRadius: "var(--tf-radius)",
          overflow: "hidden",
        }}
      >
        {isLoading ? (
          <div style={{ padding: 24, color: "var(--tf-text3)", fontSize: 13, fontFamily: "var(--tf-font-body)" }}>
            Loading organizations...
          </div>
        ) : isError ? (
          <div style={{ padding: 24, color: "var(--tf-danger)", fontSize: 13, fontFamily: "var(--tf-font-body)" }}>
            Failed to load organizations.
          </div>
        ) : !orgs?.length ? (
          <div style={{ padding: 24, color: "var(--tf-text3)", fontSize: 13, fontFamily: "var(--tf-font-body)" }}>
            No organizations found.
          </div>
        ) : (
          <table style={{ width: "100%", borderCollapse: "collapse" }}>
            <thead>
              <tr style={{ borderBottom: "1px solid var(--tf-border)" }}>
                <Th>Name</Th>
                <Th>ID</Th>
                <Th>Created</Th>
              </tr>
            </thead>
            <tbody>
              {orgs.map((org) => (
                <OrgRow key={org.id} org={org} />
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}

function OrgRow({ org }: { org: AdminOrganizationDto }) {
  return (
    <tr
      style={{
        borderBottom: "1px solid var(--tf-border)",
        transition: "background var(--tf-tr)",
      }}
      onMouseEnter={(e) => ((e.currentTarget as HTMLTableRowElement).style.background = "var(--tf-bg3)")}
      onMouseLeave={(e) => ((e.currentTarget as HTMLTableRowElement).style.background = "transparent")}
    >
      <Td>
        <span style={{ fontWeight: 500, color: "var(--tf-text)" }}>{org.name}</span>
      </Td>
      <Td>
        <span style={{ fontFamily: "var(--tf-font-mono)", fontSize: 11, color: "var(--tf-text3)" }}>
          {org.id}
        </span>
      </Td>
      <Td>
        <span style={{ fontSize: 12, color: "var(--tf-text3)", fontFamily: "var(--tf-font-mono)" }}>
          {new Date(org.createdAt).toLocaleDateString()}
        </span>
      </Td>
    </tr>
  );
}

function Th({ children }: { children: React.ReactNode }) {
  return (
    <th
      style={{
        padding: "10px 16px",
        textAlign: "left",
        fontSize: 11,
        fontFamily: "var(--tf-font-mono)",
        color: "var(--tf-text3)",
        fontWeight: 600,
        textTransform: "uppercase",
        letterSpacing: "0.05em",
      }}
    >
      {children}
    </th>
  );
}

function Td({ children }: { children: React.ReactNode }) {
  return (
    <td style={{ padding: "10px 16px", fontSize: 13, fontFamily: "var(--tf-font-body)" }}>
      {children}
    </td>
  );
}
