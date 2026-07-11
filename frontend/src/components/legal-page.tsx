import Link from 'next/link';
import { ArrowLeft } from 'lucide-react';

type LegalPageProps = {
  eyebrow: string;
  title: string;
  lastUpdated: string;
  children: React.ReactNode;
};

export function LegalPage({ eyebrow, title, lastUpdated, children }: LegalPageProps) {
  return (
    <div className="h-screen overflow-y-auto">
      <div className="mx-auto w-full max-w-3xl px-6 py-12 md:py-16">
        <Link
          href="/"
          className="inline-flex items-center gap-2 text-sm font-semibold text-grey-link transition hover:text-white"
        >
          <ArrowLeft className="h-4 w-4" strokeWidth={2} />
          Back to home
        </Link>
        <header className="mt-8">
          <p className="mono-detail text-aqua">{eyebrow}</p>
          <h1 className="mt-3 text-4xl font-extrabold tracking-[-0.05em] text-white md:text-5xl">
            {title}
          </h1>
          <p className="mt-3 text-sm text-white/45">Last updated: {lastUpdated}</p>
        </header>
        <article className="legal-prose mt-10 rounded-[2rem] border border-stroke bg-secondary-bg/90 p-8 shadow-2xl shadow-black/40 backdrop-blur md:p-12">
          {children}
        </article>
      </div>
    </div>
  );
}
