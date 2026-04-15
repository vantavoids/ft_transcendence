'use client';

import { useState, type FormEvent } from 'react';
import Link from 'next/link';
import Form from 'next/form';
import { CircleCheck } from 'lucide-react';
import {
  validateRegisterForm,
  type RegisterFormErrors
} from '../../../src/shared/lib/validators/auth';

export default function RegisterPage() {
  const [errors, setErrors] = useState<RegisterFormErrors>({});

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    const formData = new FormData(event.currentTarget);

    const nextErrors = validateRegisterForm({
      username: String(formData.get('username') ?? ''),
      password: String(formData.get('password') ?? ''),
      confirm: String(formData.get('confirm') ?? '')
    });

    setErrors(nextErrors);

    if (Object.keys(nextErrors).length > 0) {
      event.preventDefault();
    }
  }

  return (
    <div className="w-120 h-140 bg-secondary-bg rounded-md flex flex-col justify-center items-center gap-4">
      <h1 className="text-white text-6xl font-bold">register</h1>
      <Link href="/auth/login" className="text-grey-link underline underline-offset-8">
        login
      </Link>
      <Form action="/auth/register" className="mt-6 grid gap-6" onSubmit={handleSubmit}>
        <div className="grid gap-2">
          <input
            name="username"
            placeholder="username"
            className="rounded-xl border border-black/10 px-4 py-3 text-sm bg-input-bg text-center"
          />
          {errors.username ? <p className="text-sm text-red-400">{errors.username}</p> : null}
        </div>
        <div className="grid gap-2">
          <input
            name="password"
            type="password"
            placeholder="password"
            className="rounded-xl border border-black/10 px-4 py-3 text-sm bg-input-bg text-center"
          />
          {errors.password ? <p className="text-sm text-red-400">{errors.password}</p> : null}
        </div>
        <div className="grid gap-2">
          <input
            name="confirm"
            type="password"
            placeholder="confirm"
            className="rounded-xl border border-black/10 px-4 py-3 text-sm bg-input-bg text-center"
          />
          {errors.confirm ? <p className="text-sm text-red-400">{errors.confirm}</p> : null}
        </div>
        <button type="submit" className="flex justify-center">
          <div className="w-12 h-12">
            <CircleCheck className="size-full" color="white" strokeWidth="0.25" />
          </div>
        </button>
      </Form>
    </div>
  );
}
