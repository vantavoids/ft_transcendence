import Link from 'next/link';

export default function ProfilePage() {
  return (
    <section className="mx-auto flex min-h-screen w-full max-w-5xl items-center px-6 py-12">
      <div className="grid w-full gap-6 md:grid-cols-[18rem_1fr]">
        <div className="rounded-[2rem] border border-white/8 bg-secondary-bg/90 p-6 shadow-2xl shadow-black/40">
          <div className="h-36 rounded-[1.5rem] bg-[linear-gradient(135deg,#78dce8,#ab9df2,#ff6188)]" />
          <div className="mt-5">
            <p className="mono-detail text-aqua">Profile</p>
            <h1 className="mt-2 text-4xl font-extrabold tracking-[-0.06em] text-white">cartoone</h1>
            <p className="mt-2 text-white/55">Compte frontend prêt à relier au service user.</p>
          </div>
        </div>
        <div className="rounded-[2rem] border border-white/8 bg-secondary-bg/90 p-8 shadow-2xl shadow-black/40">
          <h2 className="text-3xl font-bold tracking-[-0.05em] text-white">Actions rapides</h2>
          <div className="mt-6 grid gap-4 md:grid-cols-2">
            {[
              { href: '/chat', title: 'Chat', text: 'Revenir dans la conversation' },
              { href: '/guilds', title: 'Guilds', text: 'Préparer la liste des serveurs' },
              { href: '/notifications', title: 'Notifications', text: 'Consulter les alertes' },
              { href: '/auth/login', title: 'Session', text: 'Retourner à la connexion' }
            ].map((item) => (
              <Link
                key={item.href}
                href={item.href}
                className="rounded-2xl border border-white/8 bg-panel p-5 transition hover:border-aqua/50 hover:bg-frame"
              >
                <h3 className="text-2xl font-bold tracking-[-0.05em] text-white">{item.title}</h3>
                <p className="mt-2 text-white/55">{item.text}</p>
              </Link>
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}
