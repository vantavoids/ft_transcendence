import Link from 'next/link';
import Form from 'next/form';
import { CircleCheck } from 'lucide-react';

export default function RegisterPage() {
  return (
    <div className="w-120 h-140 bg-secondary-bg rounded-md flex flex-col justify-center items-center gap-4">
      <h1 className="text-white text-6xl font-bold">register</h1>
      <Link href="/auth/login" className="text-grey-link underline underline-offset-8">
        login
      </Link>
      <Form action="/auth/register" className="mt-6 grid gap-6">
        <input
          name="username"
          placeholder="username"
          className="rounded-xl border border-black/10 px-4 py-3 text-sm bg-input-bg text-center"
        />
        <input
          name="password"
          placeholder="password"
          className="rounded-xl border border-black/10 px-4 py-3 text-sm bg-input-bg text-center"
        />
        <input
          name="confirm"
          placeholder="confirm"
          className="rounded-xl border border-black/10 px-4 py-3 text-sm bg-input-bg text-center"
        />
        <button type="submit" className="flex justify-center">
          <div className="w-12 h-12">
            <CircleCheck className="size-full" color="white" strokeWidth="0.25" />
          </div>
        </button>
      </Form>
    </div>
  );
}
