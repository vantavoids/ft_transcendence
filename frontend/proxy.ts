import type { NextRequest } from 'next/server';
import { NextResponse } from 'next/server';
import { SESSION_COOKIE_KEY } from './src/shared/lib/session';

const AUTH_PATHS = new Set(['/auth/login', '/auth/register']);

function isProtectedPath(pathname: string) {
  if (pathname.startsWith('/_next') || pathname === '/favicon.ico') {
    return false;
  }

  return !pathname.startsWith('/auth');
}

export function proxy(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const session = request.cookies.get(SESSION_COOKIE_KEY)?.value;

  if (!session && isProtectedPath(pathname)) {
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
