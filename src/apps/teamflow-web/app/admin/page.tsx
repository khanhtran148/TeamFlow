"use client";

import Link from "next/link";
import { Building2, Users, Shield } from "lucide-react";
import { useAdminOrganizations, useAdminUsers } from "@/lib/hooks/use-admin";

export default function AdminDashboardPage() {
  const { data: orgsData, isLoading: orgsLoading } = useAdminOrganizations({
    page: 1,
    pageSize: 1,
  });

  const { data: usersData, isLoading: usersLoading } = useAdminUsers({
    page: 1,
    pageSize: 1,
  });

  // Fetch a small page just to get total count of admins
  const { data: adminData, isLoading: adminLoading } = useAdminUsers({
    search: undefined,
    page: 1,
    pageSize: 100,
  });

  const adminCount =
    adminData?.items?.filter((u) => u.systemRole === "SystemAdmin").length ?? 0;

  return (
    <div style={{ maxWidth: 900 }}>
      <h1
        style={{
          fontFamily: "var(--tf-font-head)",
          fontWeight: 700,
          fontSize: 22,
          color: "var(--tf-text)",
          marginBottom: 24,
        }}
      >
        Admin Dashboard
      </h1>

      {/* Stats grid */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "repeat(auto-fill, minmax(220px, 1fr))",
          gap: 16,
          marginBottom: 32,
        }}
      >
        <StatCard
          icon={<Building2 size={18} color="var(--tf-accent)" />}
          label="Total Organizations"
          value={
            orgsLoading ? "..." : (orgsData?.totalCount ?? 0).toString()
          }
          href="/admin/organizations"
        />
        <StatCard
          icon={<Users size={18} color="var(--tf-accent)" />}
          label="Total Users"
          value={
            usersLoading ? "..." : (usersData?.totalCount ?? 0).toString()
          }
          href="/admin/users"
        />
        <StatCard
          icon={<Shield size={18} color="var(--tf-accent)" />}
          label="System Admins"
          value={adminLoading ? "..." : adminCount.toString()}
          href="/admin/users"
        />
      </div>

      {/* Quick links */}
      <div
        style={{
          background: "var(--tf-bg2)",
          border: "1px solid var(--tf-border)",
          borderRadius: "var(--tf-radius)",
          padding: "20px 24px",
        }}
      >
        <h2
          style={{
            fontFamily: "var(--tf-font-head)",
            fontWeight: 600,
            fontSize: 15,
            color: "var(--tf-text)",
            marginBottom: 12,
          }}
        >
          Quick Actions
        </h2>
        <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
          <Link
            href="/admin/organizations"
            style={{
              fontSize: 13,
              color: "var(--tf-accent)",
              textDecoration: "none",
              fontFamily: "var(--tf-font-body)",
            }}
          >
            View all organizations
          </Link>
          <Link
            href="/admin/users"
            style={{
              fontSize: 13,
              color: "var(--tf-accent)",
              textDecoration: "none",
              fontFamily: "var(--tf-font-body)",
            }}
          >
            View all users
          </Link>
        </div>
      </div>
    </div>
  );
}

function StatCard({
  icon,
  label,
  value,
  href,
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
  href: string;
}) {
  return (
    <Link
      href={href}
      style={{
        display: "flex",
        flexDirection: "column",
        gap: 10,
        padding: "16px 20px",
        background: "var(--tf-bg2)",
        border: "1px solid var(--tf-border)",
        borderRadius: "var(--tf-radius)",
        textDecoration: "none",
        transition: "border-color var(--tf-tr)",
      }}
    >
      <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
        {icon}
        <span
          style={{
            fontSize: 12,
            fontFamily: "var(--tf-font-mono)",
            color: "var(--tf-text3)",
          }}
        >
          {label}
        </span>
      </div>
      <span
        style={{
          fontFamily: "var(--tf-font-head)",
          fontWeight: 700,
          fontSize: 28,
          color: "var(--tf-text)",
        }}
      >
        {value}
      </span>
    </Link>
  );
}
