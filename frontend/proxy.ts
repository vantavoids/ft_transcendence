import type { NextRequest } from 'next/server';
import { NextResponse } from 'next/server';
import { SESSION_COOKIE_KEY } from './src/shared/lib/session';

const AUTH_PATHS = new Set(['/auth/login', '/auth/register']);

// landing and legal pages must stay readable without an account
const PUBLIC_PATHS = new Set(['/', '/terms', '/privacy']);

function isProtectedPath(pathname: string) {
  if (pathname.startsWith('/_next') || pathname === '/favicon.ico') {
    return false;
  }

  if (PUBLIC_PATHS.has(pathname)) {
    return false;
  }

  return !pathname.startsWith('/auth');
}

export function proxy(request: NextRequest) {
  const { pathname, searchParams } = request.nextUrl;
  const session = request.cookies.get(SESSION_COOKIE_KEY)?.value;

  // oauth token handoff lands on `/?access_token=...` before the presence
  // cookie exists; let it through so the client can capture the token
  const isOAuthHandoff = pathname === '/' && searchParams.has('access_token');

  if (!session && isProtectedPath(pathname) && !isOAuthHandoff) {
    const loginUrl = new URL('/auth/login', request.url);
    return NextResponse.redirect(loginUrl);
  }

  if (session && AUTH_PATHS.has(pathname)) {
    const appUrl = new URL('/chat', request.url);
    return NextResponse.redirect(appUrl);
  }

  return NextResponse.next();
}

export const config = {
  matcher: ['/((?!api|_next/static|_next/image).*)']
};
