"use client";

import { useEffect, useState, type ReactNode } from "react";
import { useRouter, usePathname } from "next/navigation";
import { useAuthStore } from "@/lib/stores/auth-store";

const PUBLIC_PATHS = ["/login", "/register", "/invite/"];

interface AuthGuardProps {
  children: ReactNode;
}

export function AuthGuard({ children }: AuthGuardProps) {
  const router = useRouter();
  const pathname = usePathname();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const user = useAuthStore((s) => s.user);
  const [checked, setChecked] = useState(false);

  useEffect(() => {
    // Wait for Zustand to rehydrate from localStorage
    const unsub = useAuthStore.persist.onFinishHydration(() => {
      setChecked(true);
    });

    // If already hydrated
    if (useAuthStore.persist.hasHydrated()) {
      setChecked(true);
    }

    return unsub;
  }, []);

  useEffect(() => {
    if (!checked) return;

    const isPublic = PUBLIC_PATHS.some((p) => pathname.startsWith(p));

    if (!isAuthenticated && !isPublic) {
      router.replace("/login");
    }

    if (isAuthenticated && isPublic) {
      router.replace(user?.systemRole === "SystemAdmin" ? "/admin" : "/onboarding");
    }
  }, [checked, isAuthenticated, user, pathname, router]);

  // Don't render protected content until we've checked auth state
  if (!checked) {
    return null;
  }

  const isPublic = PUBLIC_PATHS.some((p) => pathname.startsWith(p));
  if (!isAuthenticated && !isPublic) {
    return null;
  }

  return <>{children}</>;
}
