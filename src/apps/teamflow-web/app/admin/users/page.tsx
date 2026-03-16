"use client";

import { useQuery } from "@tanstack/react-query";
import { Users, Shield } from "lucide-react";
import { getAdminUsers } from "@/lib/api/admin";
import type { AdminUserDto } from "@/lib/api/types";

export default function AdminUsersPage() {
  const { data: users, isLoading, isError } = useQuery({
    queryKey: ["admin", "users"],
    queryFn: getAdminUsers,
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
        <Users size={18} color="var(--tf-accent)" />
        <h1
          style={{
            fontFamily: "var(--tf-font-head)",
            fontWeight: 700,
            fontSize: 22,
            color: "var(--tf-text)",
          }}
        >
          Users
        </h1>
        {!isLoading && users && (
          <span
            style={{
              fontSize: 12,
              color: "var(--tf-text3)",
              fontFamily: "var(--tf-font-mono)",
              marginLeft: "auto",
            }}
          >
            {users.length} total
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
            Loading users...
          </div>
        ) : isError ? (
          <div style={{ padding: 24, color: "var(--tf-danger)", fontSize: 13, fontFamily: "var(--tf-font-body)" }}>
            Failed to load users.
          </div>
        ) : !users?.length ? (
          <div style={{ padding: 24, color: "var(--tf-text3)", fontSize: 13, fontFamily: "var(--tf-font-body)" }}>
            No users found.
          </div>
        ) : (
          <table style={{ width: "100%", borderCollapse: "collapse" }}>
            <thead>
              <tr style={{ borderBottom: "1px solid var(--tf-border)" }}>
                <Th>Name</Th>
                <Th>Email</Th>
                <Th>Role</Th>
                <Th>Created</Th>
              </tr>
            </thead>
            <tbody>
              {users.map((user) => (
                <UserRow key={user.id} user={user} />
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}

function UserRow({ user }: { user: AdminUserDto }) {
  const isAdmin = user.systemRole === "SystemAdmin";
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
        <span style={{ fontWeight: 500, color: "var(--tf-text)" }}>{user.name}</span>
      </Td>
      <Td>
        <span style={{ color: "var(--tf-text2)", fontSize: 12 }}>{user.email}</span>
      </Td>
      <Td>
        {isAdmin ? (
          <span
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 4,
              padding: "2px 8px",
              borderRadius: 100,
              background: "var(--tf-accent-dim)",
              color: "var(--tf-accent)",
              fontSize: 11,
              fontWeight: 600,
              fontFamily: "var(--tf-font-mono)",
            }}
          >
            <Shield size={10} />
            SystemAdmin
          </span>
        ) : (
          <span
            style={{
              padding: "2px 8px",
              borderRadius: 100,
              background: "var(--tf-bg3)",
              color: "var(--tf-text3)",
              fontSize: 11,
              fontFamily: "var(--tf-font-mono)",
            }}
          >
            User
          </span>
        )}
      </Td>
      <Td>
        <span style={{ fontSize: 12, color: "var(--tf-text3)", fontFamily: "var(--tf-font-mono)" }}>
          {new Date(user.createdAt).toLocaleDateString()}
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
