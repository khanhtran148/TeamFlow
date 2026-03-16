"use client";

import { createContext, useContext, type ReactNode } from "react";
import type { OrganizationDto, MyOrganizationDto } from "@/lib/api/types";

interface OrgContextValue {
  org: OrganizationDto;
  myOrg?: MyOrganizationDto;
}

const OrgContext = createContext<OrgContextValue | null>(null);

export function OrgProvider({
  org,
  myOrg,
  children,
}: {
  org: OrganizationDto;
  myOrg?: MyOrganizationDto;
  children: ReactNode;
}) {
  return (
    <OrgContext.Provider value={{ org, myOrg }}>
      {children}
    </OrgContext.Provider>
  );
}

export function useOrgContext(): OrgContextValue {
  const ctx = useContext(OrgContext);
  if (!ctx) {
    throw new Error("useOrgContext must be used within an OrgProvider");
  }
  return ctx;
}
