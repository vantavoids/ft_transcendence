import { apiFetch } from "./client";

export type Credentials = {
  email: string;
  password: string;
};

export type AuthTokenResponse = {
  access_token: string;
  user_id: string;
};

export type AuthIdentity = {
  id: string;
  email: string | null;
  email_verified: boolean;
  oauth_providers: string[];
  created_at: string;
  updated_at: string;
};

export type UpdateIdentityPayload = {
  email?: string;
  current_password?: string;
  new_password?: string;
};

export function register(payload: Credentials) {
  return apiFetch<AuthTokenResponse>("auth", "/register", {
    method: "POST",
    body: payload,
    skipAuthRefresh: true,
  });
}

export function login(payload: Credentials) {
  return apiFetch<AuthTokenResponse>("auth", "/login", {
    method: "POST",
    body: payload,
    skipAuthRefresh: true,
  });
}

export function refresh() {
  return apiFetch<{ access_token: string }>("auth", "/refresh", {
    method: "POST",
    skipAuthRefresh: true,
  });
}

export function logout() {
  return apiFetch<void>("auth", "/logout", { method: "POST" });
}

export function getIdentity() {
  return apiFetch<AuthIdentity>("auth", "/me");
}

export function updateIdentity(payload: UpdateIdentityPayload) {
  return apiFetch<AuthIdentity>("auth", "/me", {
    method: "PATCH",
    body: payload,
  });
}

export function deleteAccount() {
  return apiFetch<void>("auth", "/me", { method: "DELETE" });
}
