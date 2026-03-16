"use client";

import { useState } from "react";
import { Users, Shield, AlertCircle, Key } from "lucide-react";
import { useAdminUsers, useResetUserPassword, useChangeUserStatus } from "@/lib/hooks/use-admin";
import { useDebounce } from "@/lib/hooks/use-debounce";
import { SearchInput } from "@/components/admin/search-input";
import { PaginationControls } from "@/components/admin/pagination-controls";
import { ResetPasswordDialog } from "@/components/admin/reset-password-dialog";
import { UserStatusToggle } from "@/components/admin/user-status-toggle";
import type { AdminUserDto } from "@/lib/api/types";

const PAGE_SIZE = 20;

export default function AdminUsersPage() {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [resetTarget, setResetTarget] = useState<AdminUserDto | null>(null);

  const debouncedSearch = useDebounce(search, 300);

  const { data, isLoading, isError } = useAdminUsers({
    search: debouncedSearch || undefined,
    page,
    pageSize: PAGE_SIZE,
  });

  const resetPassword = useResetUserPassword();
  const changeStatus = useChangeUserStatus();

  function handleSearchChange(value: string) {
    setSearch(value);
    setPage(1);
  }

  async function handleResetPassword(userId: string, newPassword: string) {
    await resetPassword.mutateAsync({ userId, body: { newPassword } });
  }

  async function handleToggleStatus(userId: string, isActive: boolean) {
    await changeStatus.mutateAsync({ userId, body: { isActive } });
  }

  return (
    <div style={{ maxWidth: 1000 }}>
      {/* Header */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 10,
          marginBottom: 20,
          flexWrap: "wrap",
        }}
      >
        <Users size={18} color="var(--tf-accent)" />
        <h1
          style={{
            fontFamily: "var(--tf-font-head)",
            fontWeight: 700,
            fontSize: 22,
            color: "var(--tf-text)",
            flex: 1,
          }}
        >
          Users
        </h1>
        {!isLoading && data && (
          <span
            style={{
              fontSize: 12,
              color: "var(--tf-text3)",
              fontFamily: "var(--tf-font-mono)",
            }}
          >
            {data.totalCount} total
          </span>
        )}
      </div>

      {/* Toolbar */}
      <div style={{ marginBottom: 16 }}>
        <SearchInput
          value={search}
          onChange={handleSearchChange}
          placeholder="Search by name or email..."
        />
      </div>

      {/* Table */}
      <div
        style={{
          background: "var(--tf-bg2)",
          border: "1px solid var(--tf-border)",
          borderRadius: "var(--tf-radius)",
          overflow: "hidden",
        }}
      >
        {isLoading ? (
          <div
            style={{
              padding: 24,
              color: "var(--tf-text3)",
              fontSize: 13,
              fontFamily: "var(--tf-font-body)",
            }}
          >
            Loading users...
          </div>
        ) : isError ? (
          <div
            style={{
              padding: 24,
              color: "var(--tf-red)",
              fontSize: 13,
              fontFamily: "var(--tf-font-body)",
            }}
          >
            Failed to load users.
          </div>
        ) : !data?.items?.length ? (
          <div
            style={{
              padding: 24,
              color: "var(--tf-text3)",
              fontSize: 13,
              fontFamily: "var(--tf-font-body)",
            }}
          >
            {debouncedSearch ? `No users found matching "${debouncedSearch}".` : "No users found."}
          </div>
        ) : (
          <>
            <div style={{ overflowX: "auto" }}>
              <table style={{ width: "100%", borderCollapse: "collapse" }}>
                <thead>
                  <tr style={{ borderBottom: "1px solid var(--tf-border)" }}>
                    <Th>Name</Th>
                    <Th>Email</Th>
                    <Th>Role</Th>
                    <Th>Status</Th>
                    <Th>Flags</Th>
                    <Th>Created</Th>
                    <Th>Actions</Th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((user) => (
                    <UserRow
                      key={user.id}
                      user={user}
                      onResetPassword={() => setResetTarget(user)}
                      onToggleStatus={handleToggleStatus}
                    />
                  ))}
                </tbody>
              </table>
            </div>

            <PaginationControls
              page={data.page}
              totalPages={data.totalPages}
              hasNextPage={data.hasNextPage}
              hasPreviousPage={data.hasPreviousPage}
              totalCount={data.totalCount}
              pageSize={data.pageSize}
              onPageChange={setPage}
            />
          </>
        )}
      </div>

      {/* Reset Password Dialog */}
      {resetTarget && (
        <ResetPasswordDialog
          userId={resetTarget.id}
          userName={resetTarget.name}
          onConfirm={handleResetPassword}
          onClose={() => setResetTarget(null)}
        />
      )}
    </div>
  );
}

function UserRow({
  user,
  onResetPassword,
  onToggleStatus,
}: {
  user: AdminUserDto;
  onResetPassword: () => void;
  onToggleStatus: (userId: string, isActive: boolean) => Promise<void>;
}) {
  const isAdmin = user.systemRole === "SystemAdmin";

  return (
    <tr
      style={{
        borderBottom: "1px solid var(--tf-border)",
        transition: "background var(--tf-tr)",
      }}
      onMouseEnter={(e) =>
        ((e.currentTarget as HTMLTableRowElement).style.background =
          "var(--tf-bg3)")
      }
      onMouseLeave={(e) =>
        ((e.currentTarget as HTMLTableRowElement).style.background =
          "transparent")
      }
    >
      <Td>
        <span style={{ fontWeight: 500, color: "var(--tf-text)" }}>
          {user.name}
        </span>
      </Td>
      <Td>
        <span style={{ color: "var(--tf-text2)", fontSize: 12 }}>
          {user.email}
        </span>
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
        <UserStatusToggle
          userId={user.id}
          userName={user.name}
          isActive={user.isActive}
          onToggle={onToggleStatus}
        />
      </Td>
      <Td>
        {user.mustChangePassword && (
          <span
            title="Must change password on next login"
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 4,
              padding: "2px 8px",
              borderRadius: 100,
              background: "var(--tf-orange-dim)",
              color: "var(--tf-orange)",
              fontSize: 11,
              fontFamily: "var(--tf-font-mono)",
            }}
          >
            <AlertCircle size={10} />
            Pwd Change
          </span>
        )}
      </Td>
      <Td>
        <span
          style={{
            fontSize: 12,
            color: "var(--tf-text3)",
            fontFamily: "var(--tf-font-mono)",
          }}
        >
          {new Date(user.createdAt).toLocaleDateString()}
        </span>
      </Td>
      <Td>
        <button
          type="button"
          onClick={onResetPassword}
          aria-label={`Reset password for ${user.name}`}
          title="Reset password"
          style={{
            display: "inline-flex",
            alignItems: "center",
            gap: 4,
            padding: "4px 8px",
            borderRadius: 6,
            border: "1px solid var(--tf-border)",
            background: "transparent",
            color: "var(--tf-text3)",
            fontSize: 11,
            fontFamily: "var(--tf-font-body)",
            cursor: "pointer",
            transition: "color 0.15s, border-color 0.15s",
            minHeight: 28,
          }}
          onMouseEnter={(e) => {
            (e.currentTarget as HTMLButtonElement).style.color =
              "var(--tf-orange)";
            (e.currentTarget as HTMLButtonElement).style.borderColor =
              "var(--tf-orange)";
          }}
          onMouseLeave={(e) => {
            (e.currentTarget as HTMLButtonElement).style.color =
              "var(--tf-text3)";
            (e.currentTarget as HTMLButtonElement).style.borderColor =
              "var(--tf-border)";
          }}
        >
          <Key size={11} />
          Reset Pwd
        </button>
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
        whiteSpace: "nowrap",
      }}
    >
      {children}
    </th>
  );
}

function Td({ children }: { children: React.ReactNode }) {
  return (
    <td
      style={{
        padding: "10px 16px",
        fontSize: 13,
        fontFamily: "var(--tf-font-body)",
        verticalAlign: "middle",
      }}
    >
      {children}
    </td>
  );
}
