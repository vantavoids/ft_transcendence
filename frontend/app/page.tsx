import Link from 'next/link';

export default function HomePage() {
  return (
    <section className="mx-auto flex min-h-screen w-full max-w-6xl items-center px-6 py-12">
      <div className="w-full rounded-[2rem] border border-white/8 bg-secondary-bg/90 p-8 shadow-2xl shadow-black/40 backdrop-blur md:p-12">
        <p className="mono-detail text-aqua">ft_transcendence</p>
        <h1 className="mt-4 max-w-3xl text-5xl font-extrabold tracking-[-0.07em] text-white md:text-7xl">
          Interface utilisable construite à partir des maquettes.
        </h1>
        <p className="mt-5 max-w-2xl text-lg text-white/65">
          Auth interactive, chat responsive sur route réelle, et structure de pages prête à
          raccorder aux services backend.
        </p>
        <div className="mt-10 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          {[
            { href: '/auth/register', title: 'Register', text: 'Création de compte avec validation' },
            { href: '/auth/login', title: 'Login', text: 'Connexion branchée à l’API auth' },
            { href: '/chat', title: 'Chat', text: 'Page responsive utilisable immédiatement' },
            { href: '/guilds', title: 'Guilds', text: 'Point d’entrée guildes' }
          ].map((item) => (
            <Link
              key={item.href}
              href={item.href}
              className="rounded-2xl border border-white/8 bg-panel p-5 transition hover:border-aqua/50 hover:bg-frame"
            >
              <h2 className="text-2xl font-bold tracking-[-0.05em] text-white">{item.title}</h2>
              <p className="mt-2 text-white/55">{item.text}</p>
            </Link>
          ))}
        </div>
      </div>
    </section>
  );
}
