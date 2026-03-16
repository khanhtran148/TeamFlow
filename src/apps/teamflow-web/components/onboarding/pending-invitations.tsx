"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Mail, Check } from "lucide-react";
import { toast } from "sonner";
import { usePendingInvitations, useAcceptInvitation } from "@/lib/hooks/use-invitations";
import { LoadingSkeleton } from "@/components/shared/loading-skeleton";
import type { ApiError } from "@/lib/api/client";

export function PendingInvitations() {
  const router = useRouter();
  const { data: invitations, isLoading, isError } = usePendingInvitations();
  const { mutate: acceptInvitation, isPending: accepting } = useAcceptInvitation();
  const [acceptingId, setAcceptingId] = useState<string | null>(null);

  if (isLoading) return <LoadingSkeleton rows={2} />;
  if (isError) return null;
  if (!invitations || invitations.length === 0) return null;

  function handleAccept(invitationId: string, token?: string) {
    // Token is not available in list view — redirect to invite link instead
    toast.info("Use your invitation link to accept this invitation.");
  }

  return (
    <div
      style={{
        background: "var(--tf-bg2)",
        border: "1px solid var(--tf-border)",
        borderRadius: "var(--tf-radius)",
        overflow: "hidden",
      }}
    >
      <div
        style={{
          padding: "12px 16px",
          borderBottom: "1px solid var(--tf-border)",
          display: "flex",
          alignItems: "center",
          gap: 8,
        }}
      >
        <Mail size={14} color="var(--tf-accent)" />
        <span
          style={{
            fontSize: 13,
            fontWeight: 600,
            color: "var(--tf-text)",
          }}
        >
          Pending Invitations ({invitations.length})
        </span>
      </div>

      <div>
        {invitations.map((inv) => (
          <div
            key={inv.id}
            style={{
              display: "flex",
              alignItems: "center",
              gap: 12,
              padding: "12px 16px",
              borderBottom: "1px solid var(--tf-border)",
            }}
          >
            <div style={{ flex: 1 }}>
              <div
                style={{
                  fontSize: 13,
                  color: "var(--tf-text)",
                  fontWeight: 500,
                }}
              >
                Organization invite
              </div>
              <div
                style={{
                  fontSize: 12,
                  color: "var(--tf-text3)",
                  marginTop: 2,
                  fontFamily: "var(--tf-font-mono)",
                }}
              >
                Role: {inv.role} &bull; Expires:{" "}
                {new Date(inv.expiresAt).toLocaleDateString()}
              </div>
            </div>

            <div
              style={{
                fontSize: 11,
                padding: "3px 8px",
                borderRadius: 100,
                background: "var(--tf-accent-dim)",
                color: "var(--tf-accent)",
                fontWeight: 600,
                fontFamily: "var(--tf-font-mono)",
              }}
            >
              Pending
            </div>
          </div>
        ))}
      </div>

      <div
        style={{
          padding: "10px 16px",
          fontSize: 12,
          color: "var(--tf-text3)",
          background: "var(--tf-bg3)",
        }}
      >
        To accept an invitation, use the invite link shared with you.
      </div>
    </div>
  );
}
