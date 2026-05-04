import Link from 'next/link';

export default function GuildsPage() {
  return (
    <section className="mx-auto flex min-h-screen w-full max-w-5xl items-center px-6 py-12">
      <div className="w-full rounded-[2rem] border border-white/8 bg-secondary-bg/90 p-8 shadow-2xl shadow-black/40 md:p-12">
        <p className="mono-detail text-aqua">Guilds</p>
        <h1 className="mt-4 text-5xl font-extrabold tracking-[-0.07em] text-white md:text-6xl">
          Espace guildes prêt à brancher.
        </h1>
        <p className="mt-5 max-w-2xl text-lg text-white/65">
          La maquette chat est maintenant utilisée directement sur `/chat`. Cette route peut
          accueillir la vraie liste de guildes dès que le service est exposé.
        </p>
        <div className="mt-8 flex flex-wrap gap-4">
          <Link
            href="/chat"
            className="rounded-full border border-aqua/50 bg-aqua/10 px-5 py-3 font-semibold text-aqua transition hover:bg-aqua/20"
          >
            Ouvrir le chat
          </Link>
          <Link
            href="/profile"
            className="rounded-full border border-white/10 px-5 py-3 font-semibold text-white/70 transition hover:border-white/20 hover:text-white"
          >
            Voir le profil
          </Link>
        </div>
      </div>
    </section>
  );
}
