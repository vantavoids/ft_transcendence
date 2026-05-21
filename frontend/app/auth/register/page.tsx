'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { UserPlus } from 'lucide-react';
import { AuthCard } from '../../../src/components/auth-card';
import { register } from '../../../src/shared/api/auth';
import { createFakeSession } from '../../../src/shared/lib/session';
import {
  validateRegisterForm,
  type RegisterFormErrors
} from '../../../src/shared/lib/validators/auth';

export default function RegisterPage() {
  const router = useRouter();
  const [errors, setErrors] = useState<RegisterFormErrors>({});
  const [serverError, setServerError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(formData: FormData) {
    const username = String(formData.get('username') ?? '');
    const password = String(formData.get('password') ?? '');
    const confirm = String(formData.get('confirm') ?? '');
    const nextErrors = validateRegisterForm({
      username,
      password,
      confirm
    });

    setErrors(nextErrors);
    setServerError('');

    if (Object.keys(nextErrors).length > 0) {
      return;
    }

    try {
      setIsSubmitting(true);
      await register({
        username: username.trim(),
        password,
        confirm
      });
      createFakeSession(username.trim());
      router.push('/chat');
    } catch (error) {
      setServerError(error instanceof Error ? error.message : 'Registration failed.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <AuthCard
      title="Register"
      subtitle="Cree ton profil local pour tester le chat et les guildes."
      alternateHref="/auth/login"
      alternateLabel="Login"
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
        <div className="grid gap-2">
          <label htmlFor="confirm" className="text-sm font-semibold text-white/70">
            Confirm password
          </label>
          <input
            id="confirm"
            name="confirm"
            type="password"
            placeholder="confirm"
            className="h-11 w-full rounded-md border border-transparent bg-input-bg px-4 text-base text-white outline-none transition placeholder:text-input-placeholder focus:border-aqua/35"
          />
          {errors.confirm ? <p className="text-sm text-pink">{errors.confirm}</p> : null}
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
          aria-label="Submit register"
        >
          <UserPlus className="h-4 w-4" strokeWidth={2} />
          {isSubmitting ? 'Creating account...' : 'Create account'}
        </button>
      </form>
    </AuthCard>
  );
}
