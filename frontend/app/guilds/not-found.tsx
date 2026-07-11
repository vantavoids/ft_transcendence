import Link from 'next/link';

export default function GuildsNotFound() {
  return (
    <section className="mx-auto flex min-h-screen w-full max-w-3xl items-center px-6 py-12">
      <div className="w-full rounded-[2rem] border border-stroke bg-secondary-bg/90 p-8 shadow-2xl shadow-black/40 md:p-12">
        <p className="mono-detail text-aqua">404</p>
        <h1 className="mt-4 text-4xl font-extrabold tracking-[-0.07em] text-white md:text-5xl">
          Guilds moved to chat
        </h1>
        <p className="mt-4 max-w-2xl text-base leading-7 text-white/65">
          The `/guilds` page no longer exists. Guild management is now available as a modal inside
          `/chat`.
        </p>
        <div className="mt-8">
          <Link
            href="/chat"
            className="inline-flex rounded-full border border-aqua/50 bg-aqua/10 px-5 py-3 font-semibold text-aqua transition hover:bg-aqua/20"
          >
            Open chat
          </Link>
        </div>
      </div>
    </section>
  );
}
