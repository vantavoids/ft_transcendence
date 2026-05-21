export const SESSION_USERNAME_KEY = 'ft_transcendence_username';
export const SESSION_COOKIE_KEY = 'ft_transcendence_session';
export const SESSION_ACCESS_TOKEN_KEY = 'ft_transcendence_access_token';

export function getAccessToken() {
  if (typeof window === 'undefined') {
    return null;
  }

  return window.localStorage.getItem(SESSION_ACCESS_TOKEN_KEY);
}

export function setAccessToken(accessToken: string) {
  if (typeof window !== 'undefined') {
    window.localStorage.setItem(SESSION_ACCESS_TOKEN_KEY, accessToken);
  }
}

export function createFakeSession(username: string, accessToken?: string) {
  if (typeof window !== 'undefined') {
    window.localStorage.setItem(SESSION_USERNAME_KEY, username);

    if (accessToken) {
      window.localStorage.setItem(SESSION_ACCESS_TOKEN_KEY, accessToken);
    }
  }

  if (typeof document !== 'undefined') {
    document.cookie = `${SESSION_COOKIE_KEY}=${encodeURIComponent(username)}; Path=/; Max-Age=${60 * 60 * 24 * 7}; SameSite=Lax`;
  }
}

export function clearFakeSession() {
  if (typeof window !== 'undefined') {
    window.localStorage.removeItem(SESSION_USERNAME_KEY);
    window.localStorage.removeItem(SESSION_ACCESS_TOKEN_KEY);
  }

  if (typeof document !== 'undefined') {
    document.cookie = `${SESSION_COOKIE_KEY}=; Path=/; Max-Age=0; SameSite=Lax`;
  }
}
