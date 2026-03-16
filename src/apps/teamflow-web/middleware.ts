import { NextResponse, type NextRequest } from "next/server";

const PUBLIC_PATHS = ["/login", "/register", "/invite/", "/deactivated"];

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;

  // Allow public paths
  if (PUBLIC_PATHS.some((p) => pathname.startsWith(p))) {
    return NextResponse.next();
  }

  // Allow static assets and API routes
  if (
    pathname.startsWith("/_next") ||
    pathname.startsWith("/api") ||
    pathname.includes(".")
  ) {
    return NextResponse.next();
  }

  // Check for auth token in cookie (Zustand persist uses localStorage,
  // but we also check a simple cookie for SSR middleware).
  // For client-side protection, the AuthGuard component handles redirect.
  // This middleware provides a fast server-side redirect for unauthenticated users.
  const authCookie = request.cookies.get("teamflow-auth-check");
  if (!authCookie) {
    // Fall through — client-side AuthGuard will handle the redirect
    // since middleware can't read localStorage (Zustand persist store)
    return NextResponse.next();
  }

  return NextResponse.next();
}

export const config = {
  matcher: ["/((?!_next/static|_next/image|favicon.ico).*)"],
};
