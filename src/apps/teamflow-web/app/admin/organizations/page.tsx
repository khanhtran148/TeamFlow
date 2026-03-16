"use client";

import { useState } from "react";
import { Building2, Pencil, UserCheck } from "lucide-react";
import {
  useAdminOrganizations,
  useChangeOrgStatus,
  useUpdateAdminOrg,
  useTransferOrgOwnership,
} from "@/lib/hooks/use-admin";
import { useDebounce } from "@/lib/hooks/use-debounce";
import { SearchInput } from "@/components/admin/search-input";
import { PaginationControls } from "@/components/admin/pagination-controls";
import { ConfirmDialog } from "@/components/admin/confirm-dialog";
import { EditOrgDialog } from "@/components/admin/edit-org-dialog";
import { TransferOwnershipDialog } from "@/components/admin/transfer-ownership-dialog";
import type {
  AdminOrganizationDto,
  AdminUpdateOrgRequest,
} from "@/lib/api/types";

const PAGE_SIZE = 20;

export default function AdminOrganizationsPage() {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [editTarget, setEditTarget] = useState<AdminOrganizationDto | null>(
    null,
  );
  const [transferTarget, setTransferTarget] =
    useState<AdminOrganizationDto | null>(null);

  const debouncedSearch = useDebounce(search, 300);

  const { data, isLoading, isError } = useAdminOrganizations({
    search: debouncedSearch || undefined,
    page,
    pageSize: PAGE_SIZE,
  });

  const changeOrgStatus = useChangeOrgStatus();
  const updateOrg = useUpdateAdminOrg();
  const transferOwnership = useTransferOrgOwnership();

  function handleSearchChange(value: string) {
    setSearch(value);
    setPage(1);
  }

  async function handleToggleOrgStatus(orgId: string, isActive: boolean) {
    await changeOrgStatus.mutateAsync({ orgId, body: { isActive } });
  }

  async function handleUpdateOrg(
    orgId: string,
    body: AdminUpdateOrgRequest,
  ) {
    await updateOrg.mutateAsync({ orgId, body });
  }

  async function handleTransferOwnership(
    orgId: string,
    newOwnerUserId: string,
  ) {
    await transferOwnership.mutateAsync({ orgId, body: { newOwnerUserId } });
  }

  return (
    <div style={{ maxWidth: 1100 }}>
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
        <Building2 size={18} color="var(--tf-accent)" />
        <h1
          style={{
            fontFamily: "var(--tf-font-head)",
            fontWeight: 700,
            fontSize: 22,
            color: "var(--tf-text)",
            flex: 1,
          }}
        >
          Organizations
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
          placeholder="Search by name or slug..."
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
            Loading organizations...
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
            Failed to load organizations.
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
            {debouncedSearch
              ? `No organizations found matching "${debouncedSearch}".`
              : "No organizations found."}
          </div>
        ) : (
          <>
            <div style={{ overflowX: "auto" }}>
              <table style={{ width: "100%", borderCollapse: "collapse" }}>
                <thead>
                  <tr style={{ borderBottom: "1px solid var(--tf-border)" }}>
                    <Th>Name</Th>
                    <Th>Slug</Th>
                    <Th>Members</Th>
                    <Th>Status</Th>
                    <Th>Created</Th>
                    <Th>Actions</Th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((org) => (
                    <OrgRow
                      key={org.id}
                      org={org}
                      onEdit={() => setEditTarget(org)}
                      onTransfer={() => setTransferTarget(org)}
                      onToggleStatus={handleToggleOrgStatus}
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

      {/* Edit Org Dialog */}
      {editTarget && (
        <EditOrgDialog
          org={editTarget}
          onConfirm={handleUpdateOrg}
          onClose={() => setEditTarget(null)}
        />
      )}

      {/* Transfer Ownership Dialog */}
      {transferTarget && (
        <TransferOwnershipDialog
          org={transferTarget}
          onConfirm={handleTransferOwnership}
          onClose={() => setTransferTarget(null)}
        />
      )}
    </div>
  );
}

function OrgRow({
  org,
  onEdit,
  onTransfer,
  onToggleStatus,
}: {
  org: AdminOrganizationDto;
  onEdit: () => void;
  onTransfer: () => void;
  onToggleStatus: (orgId: string, isActive: boolean) => Promise<void>;
}) {
  const [statusLoading, setStatusLoading] = useState(false);
  const [statusError, setStatusError] = useState<string | null>(null);
  const [showConfirm, setShowConfirm] = useState(false);

  async function handleConfirmedToggle() {
    setStatusError(null);
    setStatusLoading(true);
    try {
      await onToggleStatus(org.id, !org.isActive);
      setShowConfirm(false);
    } catch (err) {
      const message =
        err instanceof Error ? err.message : "Failed to update status.";
      setStatusError(message);
      setShowConfirm(false);
    } finally {
      setStatusLoading(false);
    }
  }

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
          {org.name}
        </span>
      </Td>
      <Td>
        <span
          style={{
            fontFamily: "var(--tf-font-mono)",
            fontSize: 12,
            color: "var(--tf-text3)",
            background: "var(--tf-bg3)",
            padding: "2px 6px",
            borderRadius: 4,
          }}
        >
          {org.slug}
        </span>
      </Td>
      <Td>
        <span
          style={{
            fontSize: 13,
            color: "var(--tf-text2)",
            fontFamily: "var(--tf-font-mono)",
          }}
        >
          {org.memberCount}
        </span>
      </Td>
      <Td>
        <div style={{ position: "relative", display: "inline-flex" }}>
          <button
            type="button"
            onClick={() => setShowConfirm(true)}
            disabled={statusLoading}
            aria-label={`${org.isActive ? "Deactivate" : "Activate"} ${org.name}`}
            title={`${org.isActive ? "Deactivate" : "Activate"} ${org.name}`}
            style={{
              padding: "3px 10px",
              borderRadius: 100,
              border: org.isActive
                ? "1px solid rgba(110, 231, 183, 0.3)"
                : "1px solid rgba(248, 113, 113, 0.3)",
              background: org.isActive
                ? "rgba(110, 231, 183, 0.08)"
                : "rgba(248, 113, 113, 0.08)",
              color: org.isActive ? "var(--tf-accent)" : "var(--tf-red)",
              fontSize: 11,
              fontWeight: 600,
              fontFamily: "var(--tf-font-mono)",
              cursor: statusLoading ? "not-allowed" : "pointer",
              opacity: statusLoading ? 0.6 : 1,
              transition: "opacity 0.15s",
              minHeight: 24,
              minWidth: 72,
            }}
          >
            {statusLoading ? "..." : org.isActive ? "Active" : "Inactive"}
          </button>
          {statusError && (
            <div
              role="alert"
              style={{
                position: "absolute",
                top: "100%",
                left: 0,
                marginTop: 4,
                padding: "4px 8px",
                borderRadius: 4,
                background: "var(--tf-bg3)",
                border: "1px solid rgba(248, 113, 113, 0.3)",
                color: "var(--tf-red)",
                fontSize: 11,
                fontFamily: "var(--tf-font-body)",
                whiteSpace: "nowrap",
                zIndex: 10,
              }}
            >
              {statusError}
            </div>
          )}
          {showConfirm && (
            <ConfirmDialog
              title={org.isActive ? "Deactivate Organization" : "Activate Organization"}
              message={
                org.isActive
                  ? `Are you sure you want to deactivate "${org.name}"? All members will lose access until reactivated.`
                  : `Are you sure you want to activate "${org.name}"?`
              }
              confirmLabel={org.isActive ? "Deactivate" : "Activate"}
              confirmVariant={org.isActive ? "danger" : "default"}
              onConfirm={handleConfirmedToggle}
              onCancel={() => setShowConfirm(false)}
              loading={statusLoading}
            />
          )}
        </div>
      </Td>
      <Td>
        <span
          style={{
            fontSize: 12,
            color: "var(--tf-text3)",
            fontFamily: "var(--tf-font-mono)",
          }}
        >
          {new Date(org.createdAt).toLocaleDateString()}
        </span>
      </Td>
      <Td>
        <div style={{ display: "flex", gap: 6 }}>
          <button
            type="button"
            onClick={onEdit}
            aria-label={`Edit ${org.name}`}
            title="Edit organization"
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
                "var(--tf-accent)";
              (e.currentTarget as HTMLButtonElement).style.borderColor =
                "var(--tf-accent)";
            }}
            onMouseLeave={(e) => {
              (e.currentTarget as HTMLButtonElement).style.color =
                "var(--tf-text3)";
              (e.currentTarget as HTMLButtonElement).style.borderColor =
                "var(--tf-border)";
            }}
          >
            <Pencil size={11} />
            Edit
          </button>

          <button
            type="button"
            onClick={onTransfer}
            aria-label={`Transfer ownership of ${org.name}`}
            title="Transfer ownership"
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
                "var(--tf-blue)";
              (e.currentTarget as HTMLButtonElement).style.borderColor =
                "var(--tf-blue)";
            }}
            onMouseLeave={(e) => {
              (e.currentTarget as HTMLButtonElement).style.color =
                "var(--tf-text3)";
              (e.currentTarget as HTMLButtonElement).style.borderColor =
                "var(--tf-border)";
            }}
          >
            <UserCheck size={11} />
            Transfer
          </button>
        </div>
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
