'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { LogIn } from 'lucide-react';
import { AuthCard } from '../../../src/components/auth-card';
import { login } from '../../../src/shared/api/auth';
import { establishSession } from '../../../src/shared/lib/session';
import { describeLoginError } from '../../../src/shared/lib/auth-errors';
import { useGuilds } from '../../../src/shared/guilds/guild-store';
import { validateLoginForm, type LoginFormErrors } from '../../../src/shared/lib/validators/auth';
import { useToast } from '../../../src/shared/ui/toast';

export default function LoginPage() {
  const router = useRouter();
  const { refreshGuilds } = useGuilds();
  const [errors, setErrors] = useState<LoginFormErrors>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const { pushToast } = useToast();

  async function handleSubmit(formData: FormData) {
    const email = String(formData.get('email') ?? '');
    const password = String(formData.get('password') ?? '');

    const nextErrors = validateLoginForm({ email, password });
    setErrors(nextErrors);

    if (Object.keys(nextErrors).length > 0) {
      return;
    }

    try {
      setIsSubmitting(true);
      const { access_token, user_id } = await login({ email: email.trim(), password });
      establishSession(access_token, user_id);
      void refreshGuilds();
      router.push('/chat');
    } catch (error) {
      pushToast({
        title: 'Login failed',
        description: describeLoginError(error),
        tone: 'error'
      });
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <AuthCard
      title="Login"
      subtitle="Reconnect to the workspace and pick up your conversations."
      alternateHref="/auth/register"
      alternateLabel="Create account"
    >
      <form action={handleSubmit} className="grid gap-4">
        <div className="grid gap-2">
          <label htmlFor="email" className="text-sm font-semibold text-white/70">
            Email
          </label>
          <input
            id="email"
            name="email"
            type="email"
            autoComplete="email"
            placeholder="all@42.fr"
            className="h-11 w-full rounded-md border border-transparent bg-input-bg px-4 text-base text-white outline-none transition placeholder:text-input-placeholder focus:border-aqua/35"
          />
          {errors.email ? <p className="text-sm text-pink">{errors.email}</p> : null}
        </div>
        <div className="grid gap-2">
          <label htmlFor="password" className="text-sm font-semibold text-white/70">
            Password
          </label>
          <input
            id="password"
            name="password"
            type="password"
            autoComplete="current-password"
            placeholder="password"
            className="h-11 w-full rounded-md border border-transparent bg-input-bg px-4 text-base text-white outline-none transition placeholder:text-input-placeholder focus:border-aqua/35"
          />
          {errors.password ? <p className="text-sm text-pink">{errors.password}</p> : null}
        </div>
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
