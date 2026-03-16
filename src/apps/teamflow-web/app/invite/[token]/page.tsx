"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { useAuthStore } from "@/lib/stores/auth-store";
import { acceptInvitation } from "@/lib/api/invitations";
import type { ApiError } from "@/lib/api/client";

export default function InviteAcceptPage() {
  const params = useParams();
  const router = useRouter();
  const token = params.token as string;
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  const [status, setStatus] = useState<"loading" | "success" | "error">("loading");
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    // If not authenticated, store the invite token and redirect to login
    if (!isAuthenticated) {
      if (typeof window !== "undefined") {
        sessionStorage.setItem("pending-invite-token", token);
      }
      router.replace(`/login?invite=${encodeURIComponent(token)}`);
      return;
    }

    // Accept the invitation
    acceptInvitation(token)
      .then((response) => {
        setStatus("success");
        // Redirect to the new org after a short delay
        setTimeout(() => {
          router.replace(`/org/${response.organizationSlug}/projects`);
        }, 2000);
      })
      .catch((err: ApiError) => {
        setStatus("error");
        setErrorMessage(err.message ?? "Failed to accept invitation.");
      });
  }, [token, isAuthenticated, router]);

  return (
    <div
      style={{
        minHeight: "100vh",
        background: "var(--tf-bg)",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        padding: 24,
      }}
    >
      <div
        style={{
          maxWidth: 400,
          width: "100%",
          background: "var(--tf-bg2)",
          border: "1px solid var(--tf-border)",
          borderRadius: "var(--tf-radius)",
          padding: 32,
          textAlign: "center",
        }}
      >
        <div
          style={{
            width: 48,
            height: 48,
            borderRadius: 12,
            background: "linear-gradient(135deg, var(--tf-accent), var(--tf-blue))",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            margin: "0 auto 16px",
            fontSize: 18,
            fontWeight: 800,
            color: "#0a0a0b",
          }}
        >
          TF
        </div>

        {status === "loading" && (
          <>
            <h1
              style={{
                fontFamily: "var(--tf-font-head)",
                fontSize: 20,
                fontWeight: 700,
                color: "var(--tf-text)",
                marginBottom: 8,
              }}
            >
              Accepting Invitation
            </h1>
            <p style={{ fontSize: 14, color: "var(--tf-text3)" }}>
              Please wait while we process your invitation...
            </p>
          </>
        )}

        {status === "success" && (
          <>
            <h1
              style={{
                fontFamily: "var(--tf-font-head)",
                fontSize: 20,
                fontWeight: 700,
                color: "var(--tf-text)",
                marginBottom: 8,
              }}
            >
              Welcome aboard!
            </h1>
            <p style={{ fontSize: 14, color: "var(--tf-text3)" }}>
              You have successfully joined the organization. Redirecting...
            </p>
          </>
        )}

        {status === "error" && (
          <>
            <h1
              style={{
                fontFamily: "var(--tf-font-head)",
                fontSize: 20,
                fontWeight: 700,
                color: "var(--tf-text)",
                marginBottom: 8,
              }}
            >
              Invitation Error
            </h1>
            <p style={{ fontSize: 14, color: "var(--tf-red)", marginBottom: 16 }}>
              {errorMessage}
            </p>
            <button
              onClick={() => router.push("/onboarding")}
              style={{
                padding: "7px 20px",
                borderRadius: 6,
                border: "1px solid var(--tf-accent)",
                background: "var(--tf-accent)",
                color: "var(--primary-foreground)",
                fontSize: 13,
                fontWeight: 600,
                cursor: "pointer",
                fontFamily: "var(--tf-font-body)",
              }}
            >
              Go to Home
            </button>
          </>
        )}
      </div>
    </div>
  );
}
