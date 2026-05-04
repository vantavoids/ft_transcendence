import Link from 'next/link';

export default function NotificationsPage() {
  return (
    <section className="mx-auto flex min-h-screen w-full max-w-5xl items-center px-6 py-12">
      <div className="w-full rounded-[2rem] border border-white/8 bg-secondary-bg/90 p-8 shadow-2xl shadow-black/40 md:p-12">
        <p className="mono-detail text-aqua">Notifications</p>
        <h1 className="mt-4 text-5xl font-extrabold tracking-[-0.07em] text-white md:text-6xl">
          Centre de notifications prêt pour les données réelles.
        </h1>
        <div className="mt-8 grid gap-4">
          {[
            'Alerte de partie gagnée',
            'Invitation de guilde',
            'Demande d’ami acceptée'
          ].map((label) => (
            <div key={label} className="rounded-2xl border border-white/8 bg-panel px-5 py-4 text-white/80">
              {label}
            </div>
          ))}
        </div>
        <div className="mt-8">
          <Link
            href="/chat"
            className="rounded-full border border-aqua/50 bg-aqua/10 px-5 py-3 font-semibold text-aqua transition hover:bg-aqua/20"
          >
            Retour au chat
          </Link>
        </div>
      </div>
    </section>
  );
}
