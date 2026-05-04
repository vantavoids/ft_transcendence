import { apiFetch } from './client';

export type AuthPayload = {
  username: string;
  password: string;
};

export type RegisterPayload = AuthPayload & {
  confirm: string;
};

export async function login(payload: AuthPayload) {
  return apiFetch('auth', '/login', {
    method: 'POST',
    body: payload
  });
}

export async function register(payload: RegisterPayload) {
  return apiFetch('auth', '/register', {
    method: 'POST',
    body: payload
  });
}
