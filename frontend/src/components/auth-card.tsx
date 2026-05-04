import Link from 'next/link';

type AuthCardProps = {
  title: 'login' | 'register';
  alternateHref: string;
  alternateLabel: string;
  children: React.ReactNode;
};

export function AuthCard({ title, alternateHref, alternateLabel, children }: AuthCardProps) {
  return (
    <div className="relative flex min-h-screen w-full items-center justify-center px-4 py-6 sm:px-6 sm:py-10">
      <Link
        href="/"
        className="mono-detail absolute left-6 top-6 text-[2rem] font-bold tracking-[-0.06em] text-white sm:left-8 sm:top-8"
      >
        Logo<span className="text-aqua">_</span>
      </Link>
      <div className="w-full max-w-[26rem]">
        <div className="relative w-full rounded-xl bg-secondary-bg px-6 py-10 shadow-glow ring-1 ring-white/5 sm:px-12 sm:py-14">
          <div className="mb-10 flex flex-col items-center">
            <h1 className="font-display text-[4rem] font-extrabold leading-none tracking-[-0.07em] text-white sm:text-[4.5rem]">
              {title}
            </h1>
            <Link
              href={alternateHref}
              className="mt-4 border-b border-grey-link/70 pb-1 text-[1.55rem] text-grey-link transition hover:text-white"
            >
              {alternateLabel}
            </Link>
          </div>
          {children}
        </div>
      </div>
    </div>
  );
}
