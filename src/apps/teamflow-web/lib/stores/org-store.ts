"use client";

import { create } from "zustand";
import type { MyOrganizationDto } from "@/lib/api/types";

interface OrgState {
  currentSlug: string | null;
  myOrgs: MyOrganizationDto[];
  setCurrentSlug: (slug: string | null) => void;
  setMyOrgs: (orgs: MyOrganizationDto[]) => void;
  getCurrentOrg: () => MyOrganizationDto | undefined;
}

export const useOrgStore = create<OrgState>((set, get) => ({
  currentSlug: null,
  myOrgs: [],

  setCurrentSlug: (slug) => set({ currentSlug: slug }),

  setMyOrgs: (orgs) => set({ myOrgs: orgs }),

  getCurrentOrg: () => {
    const { currentSlug, myOrgs } = get();
    if (!currentSlug) return undefined;
    return myOrgs.find((o) => o.slug === currentSlug);
  },
}));
