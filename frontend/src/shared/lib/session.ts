export const SESSION_USERNAME_KEY = 'ft_transcendence_username';
export const SESSION_COOKIE_KEY = 'ft_transcendence_session';

export function createFakeSession(username: string) {
  if (typeof window !== 'undefined') {
    window.localStorage.setItem(SESSION_USERNAME_KEY, username);
  }

  if (typeof document !== 'undefined') {
    document.cookie = `${SESSION_COOKIE_KEY}=${encodeURIComponent(username)}; Path=/; Max-Age=${60 * 60 * 24 * 7}; SameSite=Lax`;
  }
}

export function clearFakeSession() {
  if (typeof window !== 'undefined') {
    window.localStorage.removeItem(SESSION_USERNAME_KEY);
  }

  if (typeof document !== 'undefined') {
    document.cookie = `${SESSION_COOKIE_KEY}=; Path=/; Max-Age=0; SameSite=Lax`;
  }
}
