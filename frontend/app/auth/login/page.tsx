'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { CircleCheck } from 'lucide-react';
import { AuthCard } from '../../../src/components/auth-card';
import { login } from '../../../src/shared/api/auth';
import { SESSION_USERNAME_KEY } from '../../../src/shared/lib/session';
import {
  validateLoginForm,
  type LoginFormErrors
} from '../../../src/shared/lib/validators/auth';

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
      window.localStorage.setItem(SESSION_USERNAME_KEY, username.trim());
      router.push('/chat');
    } catch (error) {
      setServerError(error instanceof Error ? error.message : 'Login failed.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <AuthCard title="login" alternateHref="/auth/register" alternateLabel="register">
      <form action={handleSubmit} className="grid gap-6">
        <div className="grid gap-2">
          <input
            name="username"
            placeholder="username"
            className="h-12 rounded-lg border border-transparent bg-input-bg px-5 text-center text-[1.55rem] text-white outline-none placeholder:text-input-placeholder"
          />
          {errors.username ? <p className="text-sm text-pink">{errors.username}</p> : null}
        </div>
        <div className="grid gap-2">
          <input
            name="password"
            type="password"
            placeholder="password"
            className="h-12 rounded-lg border border-transparent bg-input-bg px-5 text-center text-[1.55rem] text-white outline-none placeholder:text-input-placeholder"
          />
          {errors.password ? <p className="text-sm text-pink">{errors.password}</p> : null}
        </div>
        {serverError ? <p className="text-center text-sm text-pink">{serverError}</p> : null}
        <button
          type="submit"
          disabled={isSubmitting}
          className="mt-2 flex justify-center disabled:cursor-not-allowed disabled:opacity-60"
          aria-label="Submit login"
        >
          <span className="flex h-14 w-14 items-center justify-center rounded-full border border-white/40 text-white transition hover:border-white hover:bg-white/5">
            <CircleCheck className="h-7 w-7" strokeWidth={1.3} />
          </span>
        </button>
      </form>
    </AuthCard>
  );
}
