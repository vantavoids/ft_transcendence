import Link from 'next/link';
import type { ComponentType } from 'react';
import { API_BASE_URL } from '../shared/config/env';
import { FortyTwoIcon, GitHubIcon, GoogleIcon } from './icons/brand-icons';

type AuthCardProps = {
  title: 'Login' | 'Register';
  subtitle: string;
  alternateHref: string;
  alternateLabel: string;
  children: React.ReactNode;
};

const OAUTH_PROVIDERS: { slug: string; label: string; Icon: ComponentType<{ className?: string }> }[] = [
  { slug: 'fortytwo', label: '42', Icon: FortyTwoIcon },
  { slug: 'google', label: 'Google', Icon: GoogleIcon },
  { slug: 'github', label: 'GitHub', Icon: GitHubIcon }
];

export function AuthCard({ title, subtitle, alternateHref, alternateLabel, children }: AuthCardProps) {
  return (
    <section className="mx-auto flex min-h-screen w-full max-w-6xl items-center justify-center px-4 py-6 sm:px-6 lg:px-8">
      <div className="w-full max-w-[25rem] overflow-hidden rounded-[1rem] bg-secondary-bg p-5 shadow-2xl shadow-black/40 ring-1 ring-white/5 sm:p-7">
          <div className="mb-8 flex items-center justify-between gap-4">
            <Link href="/" aria-label="Home" className="text-white">
              <FortyTwoIcon className="h-8 w-8" />
            </Link>
          </div>

          <div className="mb-7">
            <p className="font-category text-[0.72rem] uppercase tracking-[0.16em] text-category">
              Account
            </p>
            <h2 className="mt-2 text-[2rem] font-bold tracking-[-0.05em] text-white">{title}</h2>
            <p className="mt-2 text-sm leading-6 text-white/45">{subtitle}</p>
          </div>

          <div className="flex justify-center gap-3">
            {OAUTH_PROVIDERS.map(({ slug, label, Icon }) => (
              <a
                key={slug}
                href={`${API_BASE_URL}/auth/v1/oauth/${slug}`}
                aria-label={`Continue with ${label}`}
                title={`Continue with ${label}`}
                className="flex h-11 w-11 items-center justify-center rounded-md border border-aqua/30 bg-aqua/10 text-aqua transition hover:border-aqua/50 hover:bg-aqua/15 hover:text-white"
              >
                <Icon className="h-5 w-5" />
              </a>
            ))}
          </div>

          <div className="my-5 flex items-center gap-4 text-white/25">
            <div className="h-px flex-1 bg-white/10" />
            <span className="mono-detail text-xs uppercase tracking-[0.18em]">or</span>
            <div className="h-px flex-1 bg-white/10" />
          </div>

          {children}

          <p className="mt-6 text-center text-sm text-white/40">
            {title === 'Login' ? 'Need an account?' : 'Already registered?'}{' '}
            <Link
              href={alternateHref}
              className="font-semibold text-aqua underline-offset-2 transition hover:text-white hover:underline"
            >
              {alternateLabel}
            </Link>
          </p>
      </div>
    </section>
  );
}
