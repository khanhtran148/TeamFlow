"use client";

import Link from "next/link";
import { Building2 } from "lucide-react";
import type { MyOrganizationDto } from "@/lib/api/types";

interface OrgPickerCardProps {
  org: MyOrganizationDto;
}

export function OrgPickerCard({ org }: OrgPickerCardProps) {
  return (
    <Link
      href={`/org/${org.slug}/projects`}
      style={{
        display: "flex",
        flexDirection: "column",
        gap: 10,
        padding: 20,
        borderRadius: "var(--tf-radius)",
        border: "1px solid var(--tf-border)",
        background: "var(--tf-bg2)",
        textDecoration: "none",
        transition: "border-color var(--tf-tr), box-shadow var(--tf-tr)",
        cursor: "pointer",
      }}
      onMouseEnter={(e) => {
        const el = e.currentTarget as HTMLAnchorElement;
        el.style.borderColor = "var(--tf-accent)";
        el.style.boxShadow = "0 0 0 1px var(--tf-accent)";
      }}
      onMouseLeave={(e) => {
        const el = e.currentTarget as HTMLAnchorElement;
        el.style.borderColor = "var(--tf-border)";
        el.style.boxShadow = "none";
      }}
    >
      <div
        style={{
          width: 40,
          height: 40,
          borderRadius: 10,
          background: "var(--tf-accent-dim)",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
      >
        <Building2 size={20} color="var(--tf-accent)" />
      </div>

      <div>
        <div
          style={{
            fontSize: 15,
            fontWeight: 600,
            color: "var(--tf-text)",
            fontFamily: "var(--tf-font-head)",
          }}
        >
          {org.name}
        </div>
        <div
          style={{
            fontSize: 12,
            color: "var(--tf-text3)",
            fontFamily: "var(--tf-font-mono)",
            marginTop: 2,
          }}
        >
          /{org.slug}
        </div>
      </div>

      <div
        style={{
          marginTop: "auto",
          fontSize: 12,
          color: "var(--tf-text3)",
          textTransform: "capitalize",
        }}
      >
        {org.role}
      </div>
    </Link>
  );
}
