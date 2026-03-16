import Link from "next/link";
import { ShieldOff } from "lucide-react";

export default function DeactivatedPage() {
  return (
    <div
      style={{
        minHeight: "100vh",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        background: "var(--tf-bg)",
        padding: 24,
      }}
    >
      <div
        style={{
          width: "100%",
          maxWidth: 440,
          padding: 40,
          borderRadius: 12,
          background: "var(--tf-bg2)",
          border: "1px solid var(--tf-border)",
          textAlign: "center",
        }}
      >
        <div
          style={{
            display: "inline-flex",
            alignItems: "center",
            justifyContent: "center",
            width: 56,
            height: 56,
            borderRadius: 14,
            background: "rgba(248, 113, 113, 0.1)",
            border: "1px solid rgba(248, 113, 113, 0.2)",
            marginBottom: 20,
          }}
        >
          <ShieldOff size={26} color="var(--tf-red)" />
        </div>

        <h1
          style={{
            fontSize: 22,
            fontWeight: 700,
            color: "var(--tf-text)",
            fontFamily: "var(--tf-font-head)",
            marginBottom: 10,
          }}
        >
          Account Deactivated
        </h1>

        <p
          style={{
            fontSize: 14,
            color: "var(--tf-text2)",
            lineHeight: 1.6,
            marginBottom: 28,
            fontFamily: "var(--tf-font-body)",
          }}
        >
          Your account has been deactivated. Please contact your system
          administrator if you believe this is a mistake.
        </p>

        <Link
          href="/login"
          style={{
            display: "inline-flex",
            alignItems: "center",
            justifyContent: "center",
            padding: "10px 24px",
            borderRadius: 8,
            border: "1px solid var(--tf-border)",
            background: "transparent",
            color: "var(--tf-text2)",
            fontSize: 13,
            fontFamily: "var(--tf-font-body)",
            textDecoration: "none",
            transition: "border-color 0.15s, color 0.15s",
          }}
        >
          Back to Sign In
        </Link>
      </div>
    </div>
  );
}
