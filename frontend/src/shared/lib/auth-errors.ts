import { ApiError } from "../api";

const FALLBACK = "Something went wrong. Please try again.";

export function describeLoginError(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.status === 401) return "Invalid email or password.";
    if (error.status === 400) return error.message || "Enter your email and password.";
    return error.message || FALLBACK;
  }
  return FALLBACK;
}

export function describeRegisterError(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.status === 409) return "That email is already registered.";
    if (error.status === 400) return error.message || "Invalid email or weak password.";
    return error.message || FALLBACK;
  }
  return FALLBACK;
}

export function describeAccountUpdateError(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.status === 401) return "Your current password is incorrect.";
    if (error.status === 403) return "This account is managed by an OAuth provider and has no password.";
    if (error.status === 409) return "That email is already in use.";
    if (error.status === 400) return error.message || "Check your email and password.";
    return error.message || FALLBACK;
  }
  return FALLBACK;
}

export function describeDeleteAccountError(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.status === 409)
      return "You still own one or more guilds. Transfer ownership or delete them first.";
    return error.message || FALLBACK;
  }
  return FALLBACK;
}
