'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { CircleCheck } from 'lucide-react';
import { AuthCard } from '../../../src/components/auth-card';
import { register } from '../../../src/shared/api/auth';
import { SESSION_USERNAME_KEY } from '../../../src/shared/lib/session';
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
      window.localStorage.setItem(SESSION_USERNAME_KEY, username.trim());
      router.push('/chat');
    } catch (error) {
      setServerError(error instanceof Error ? error.message : 'Registration failed.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <AuthCard title="register" alternateHref="/auth/login" alternateLabel="login">
      <form action={handleSubmit} className="grid gap-6">
        <div className="grid gap-2">
          <input
            name="username"
            placeholder="cartoo"
            className="h-12 w-full rounded-lg border border-transparent bg-input-bg px-5 text-center text-[1.55rem] text-white outline-none placeholder:text-input-placeholder"
          />
          {errors.username ? <p className="text-sm text-pink">{errors.username}</p> : null}
        </div>
        <div className="grid gap-2">
          <input
            name="password"
            type="password"
            placeholder="password"
            className="h-12 w-full rounded-lg border border-transparent bg-input-bg px-5 text-center text-[1.55rem] text-white outline-none placeholder:text-input-placeholder"
          />
          {errors.password ? <p className="text-sm text-pink">{errors.password}</p> : null}
        </div>
        <div className="grid gap-2">
          <input
            name="confirm"
            type="password"
            placeholder="confirm"
            className="h-12 w-full rounded-lg border border-transparent bg-input-bg px-5 text-center text-[1.55rem] text-white outline-none placeholder:text-input-placeholder"
          />
          {errors.confirm ? <p className="text-sm text-pink">{errors.confirm}</p> : null}
        </div>
        {serverError ? <p className="text-center text-sm text-pink">{serverError}</p> : null}
        <button
          type="submit"
          disabled={isSubmitting}
          className="mt-2 flex justify-center disabled:cursor-not-allowed disabled:opacity-60"
          aria-label="Submit register"
        >
          <span className="flex h-14 w-14 items-center justify-center rounded-full border border-white/40 text-white transition hover:border-white hover:bg-white/5">
            <CircleCheck className="h-7 w-7" strokeWidth={1.3} />
          </span>
        </button>
      </form>
    </AuthCard>
  );
}
