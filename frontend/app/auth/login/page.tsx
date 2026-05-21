'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { LogIn } from 'lucide-react';
import { AuthCard } from '../../../src/components/auth-card';
import { login } from '../../../src/shared/api/auth';
import { createFakeSession } from '../../../src/shared/lib/session';
import { validateLoginForm, type LoginFormErrors } from '../../../src/shared/lib/validators/auth';

export default function LoginPage() {
  const router = useRouter();
  const [errors, setErrors] = useState<LoginFormErrors>({});
  const [serverError, setServerError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(formData: FormData) {
    const username = String(formData.get('username') ?? '');
    const password = String(formData.get('password') ?? '');

    const nextErrors = validateLoginForm({ username, password });
    setErrors(nextErrors);
    setServerError('');

    if (Object.keys(nextErrors).length > 0) {
      return;
    }

    try {
      setIsSubmitting(true);
      await login({ username: username.trim(), password });
      createFakeSession(username.trim());
      router.push('/chat');
    } catch (error) {
      setServerError(error instanceof Error ? error.message : 'Login failed.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <AuthCard
      title="Login"
      subtitle="Reconnecte-toi au workspace pour reprendre tes conversations."
      alternateHref="/auth/register"
      alternateLabel="Create account"
    >
      <form action={handleSubmit} className="grid gap-4">
        <div className="grid gap-2">
          <label htmlFor="username" className="text-sm font-semibold text-white/70">
            Username
          </label>
          <input
            id="username"
            name="username"
            placeholder="username"
            className="h-11 w-full rounded-md border border-transparent bg-input-bg px-4 text-base text-white outline-none transition placeholder:text-input-placeholder focus:border-aqua/35"
          />
          {errors.username ? <p className="text-sm text-pink">{errors.username}</p> : null}
        </div>
        <div className="grid gap-2">
          <label htmlFor="password" className="text-sm font-semibold text-white/70">
            Password
          </label>
          <input
            id="password"
            name="password"
            type="password"
            placeholder="password"
            className="h-11 w-full rounded-md border border-transparent bg-input-bg px-4 text-base text-white outline-none transition placeholder:text-input-placeholder focus:border-aqua/35"
          />
          {errors.password ? <p className="text-sm text-pink">{errors.password}</p> : null}
        </div>
        {serverError ? (
          <p className="rounded-md border border-pink/25 bg-pink/10 px-3 py-2 text-sm text-pink">
            {serverError}
          </p>
        ) : null}
        <button
          type="submit"
          disabled={isSubmitting}
          className="mt-2 flex h-11 items-center justify-center gap-2 rounded-md bg-aqua text-sm font-bold text-primary-bg transition hover:bg-white disabled:cursor-not-allowed disabled:bg-frame disabled:text-white/25"
          aria-label="Submit login"
        >
          <LogIn className="h-4 w-4" strokeWidth={2} />
          {isSubmitting ? 'Logging in...' : 'Login'}
        </button>
      </form>
    </AuthCard>
  );
}
