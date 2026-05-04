import Link from 'next/link';

type AuthCardProps = {
  title: 'login' | 'register';
  alternateHref: string;
  alternateLabel: string;
  children: React.ReactNode;
};

export function AuthCard({ title, alternateHref, alternateLabel, children }: AuthCardProps) {
  return (
    <div className="flex min-h-screen items-center justify-center px-6 py-10">
      <div className="relative w-full max-w-[26rem]">
        <div className="absolute inset-x-[-2rem] inset-y-8 rounded-[2rem] bg-transparent shadow-glow" />
        <div className="absolute left-[-18rem] top-[-8rem] hidden lg:block">
          <Link
            href="/"
            className="mono-detail text-[2.2rem] font-bold tracking-[-0.06em] text-white"
          >
            Logo<span className="text-aqua">_</span>
          </Link>
        </div>
        <div className="relative rounded-xl bg-secondary-bg px-8 py-10 shadow-2xl shadow-black/40 ring-1 ring-white/5 sm:px-12 sm:py-14">
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
