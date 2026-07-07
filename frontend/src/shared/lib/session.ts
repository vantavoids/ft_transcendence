// access token lives in localStorage (readable by the api client); a non-secret
// presence cookie lets the server-side middleware gate routes. the refresh token
// is an HttpOnly cookie owned by the backend and never touched here.

export const SESSION_ACCESS_TOKEN_KEY = "ft_transcendence_access_token";
export const SESSION_USER_ID_KEY = "ft_transcendence_user_id";
export const SESSION_COOKIE_KEY = "ft_transcendence_session";

const SESSION_COOKIE_MAX_AGE = 60 * 60 * 24 * 7;

let accessTokenCache: string | null = null;

export function getAccessToken(): string | null {
  if (accessTokenCache !== null) {
    return accessTokenCache;
  }
  if (typeof window === "undefined") {
    return null;
  }
  accessTokenCache = window.localStorage.getItem(SESSION_ACCESS_TOKEN_KEY);
  return accessTokenCache;
}

export function setAccessToken(accessToken: string): void {
  accessTokenCache = accessToken;
  if (typeof window !== "undefined") {
    window.localStorage.setItem(SESSION_ACCESS_TOKEN_KEY, accessToken);
  }
}

export function getUserId(): string | null {
  if (typeof window === "undefined") {
    return null;
  }
  return window.localStorage.getItem(SESSION_USER_ID_KEY);
}

export function hasSession(): boolean {
  return getAccessToken() !== null;
}

export function establishSession(accessToken: string, userId?: string): void {
  setAccessToken(accessToken);

  if (typeof window !== "undefined" && userId) {
    window.localStorage.setItem(SESSION_USER_ID_KEY, userId);
  }

  if (typeof document !== "undefined") {
    document.cookie = `${SESSION_COOKIE_KEY}=1; Path=/; Max-Age=${SESSION_COOKIE_MAX_AGE}; SameSite=Lax`;
  }
}

export function clearSession(): void {
  accessTokenCache = null;

  if (typeof window !== "undefined") {
    window.localStorage.removeItem(SESSION_ACCESS_TOKEN_KEY);
    window.localStorage.removeItem(SESSION_USER_ID_KEY);
  }

  if (typeof document !== "undefined") {
    document.cookie = `${SESSION_COOKIE_KEY}=; Path=/; Max-Age=0; SameSite=Lax`;
  }
}
