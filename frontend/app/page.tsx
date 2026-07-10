import Link from 'next/link';
import { ArrowRight, Bell, Hash, MessageCircle, Video } from 'lucide-react';
import { OAuthHandoff } from '../src/components/oauth-handoff';

const features = [
  {
    icon: Hash,
    accent: 'text-aqua',
    title: 'Des guildes et des salons',
    text: 'Crée ta guilde, organise-la en catégories et salons, et garde chaque sujet à sa place. Rôles, permissions et modération intégrés.'
  },
  {
    icon: MessageCircle,
    accent: 'text-lavender',
    title: 'Messages directs et amis',
    text: 'Discute en temps réel avec tes amis, gère tes demandes et tes blocages, et retrouve tes conversations où tu les as laissées.'
  },
  {
    icon: Video,
    accent: 'text-pink',
    title: 'Appels voix et vidéo',
    text: 'Passe en vocal ou en vidéo en un clic, directement depuis une conversation, grâce au WebRTC pair-à-pair.'
  },
  {
    icon: Bell,
    accent: 'text-yellow',
    title: 'Notifications en temps réel',
    text: 'Mentions, demandes d’amis, invitations : tout arrive instantanément via WebSocket, sans recharger la page.'
  }
];

const footerColumns = [
  {
    heading: 'Produit',
    links: [
      { href: '/chat', label: 'Chat' },
      { href: '/guilds', label: 'Guilds' }
    ]
  },
  {
    heading: 'Compte',
    links: [
      { href: '/auth/login', label: 'Login' },
      { href: '/auth/register', label: 'Register' }
    ]
  },
  {
    heading: 'Légal',
    links: [
      { href: '/terms', label: 'Conditions d’utilisation' },
      { href: '/privacy', label: 'Politique de confidentialité' }
    ]
  }
];

export default function HomePage() {
  return (
    <div className="h-screen overflow-y-auto">
      <OAuthHandoff />

      <header className="mx-auto flex w-full max-w-6xl items-center justify-between px-6 py-6">
        <Link href="/" className="text-xl font-extrabold tracking-[-0.05em] text-white">
          ft_discord
        </Link>
        <Link
          href="/auth/login"
          className="rounded-full bg-white px-5 py-2 text-sm font-bold text-primary-bg transition hover:bg-aqua"
        >
          Login
        </Link>
      </header>

      <section className="mx-auto w-full max-w-6xl px-6 pb-20 pt-14 text-center md:pt-24">
        <p className="mono-detail text-aqua">ft_transcendence · École 42</p>
        <h1 className="mx-auto mt-5 max-w-4xl text-5xl font-extrabold uppercase tracking-[-0.05em] text-white md:text-8xl">
          Un endroit pour parler et se retrouver
        </h1>
        <p className="mx-auto mt-6 max-w-2xl text-lg text-white/65">
          ft_discord est une plateforme de communication en temps réel : guildes, salons, messages
          directs, amis et appels. Un projet étudiant, mais une vraie place pour ta communauté.
        </p>
        <div className="mt-10 flex flex-col items-center justify-center gap-4 sm:flex-row">
          <Link
            href="/auth/register"
            className="inline-flex items-center gap-2 rounded-full bg-aqua px-7 py-3.5 text-base font-bold text-primary-bg shadow-glow transition hover:bg-white"
          >
            Créer un compte
            <ArrowRight className="h-4 w-4" strokeWidth={2.5} />
          </Link>
          <Link
            href="/chat"
            className="inline-flex items-center gap-2 rounded-full border border-white/15 bg-panel px-7 py-3.5 text-base font-bold text-white transition hover:border-aqua/50 hover:bg-frame"
          >
            Ouvrir ft_discord dans le navigateur
          </Link>
        </div>
      </section>

      <section className="mx-auto w-full max-w-6xl px-6 pb-24">
        <div className="grid gap-4 md:grid-cols-2">
          {features.map((feature) => (
            <div
              key={feature.title}
              className="rounded-[2rem] border border-white/8 bg-secondary-bg/90 p-8 shadow-2xl shadow-black/40 backdrop-blur transition hover:border-white/15 md:p-10"
            >
              <feature.icon className={`h-8 w-8 ${feature.accent}`} strokeWidth={1.75} />
              <h2 className="mt-5 text-2xl font-bold tracking-[-0.04em] text-white md:text-3xl">
                {feature.title}
              </h2>
              <p className="mt-3 text-base leading-relaxed text-white/60">{feature.text}</p>
            </div>
          ))}
        </div>
      </section>

      <section className="mx-auto w-full max-w-6xl px-6 pb-24">
        <div className="rounded-[2rem] border border-white/8 bg-secondary-bg/90 p-10 text-center shadow-2xl shadow-black/40 backdrop-blur md:p-14">
          <h2 className="text-3xl font-extrabold tracking-[-0.05em] text-white md:text-5xl">
            Prêt à rejoindre la conversation ?
          </h2>
          <p className="mx-auto mt-4 max-w-xl text-base text-white/60">
            Inscris-toi avec un email, ou connecte-toi via GitHub, Google ou l’intranet 42.
          </p>
          <Link
            href="/auth/register"
            className="mt-8 inline-flex items-center gap-2 rounded-full bg-lavender px-7 py-3.5 text-base font-bold text-primary-bg transition hover:bg-white"
          >
            Commencer maintenant
            <ArrowRight className="h-4 w-4" strokeWidth={2.5} />
          </Link>
        </div>
      </section>

      <footer className="border-t border-white/8 bg-secondary-bg/70">
        <div className="mx-auto grid w-full max-w-6xl gap-10 px-6 py-14 md:grid-cols-[2fr_1fr_1fr_1fr]">
          <div>
            <p className="text-2xl font-extrabold tracking-[-0.05em] text-aqua">ft_discord</p>
            <p className="mt-3 max-w-xs text-sm leading-relaxed text-white/50">
              Projet étudiant réalisé dans le cadre du cursus ft_transcendence de l’École 42. Aucune
              garantie de service — voir les conditions d’utilisation.
            </p>
            <a
              href="mailto:yandry@student.42.fr"
              className="mt-4 inline-block text-sm text-grey-link transition hover:text-white"
            >
              yandry@student.42.fr
            </a>
          </div>
          {footerColumns.map((column) => (
            <nav key={column.heading} aria-label={column.heading}>
              <p className="font-category text-sm font-semibold uppercase tracking-wider text-category">
                {column.heading}
              </p>
              <ul className="mt-4 grid gap-2.5">
                {column.links.map((link) => (
                  <li key={link.href}>
                    <Link
                      href={link.href}
                      className="text-sm text-grey-link transition hover:text-white"
                    >
                      {link.label}
                    </Link>
                  </li>
                ))}
              </ul>
            </nav>
          ))}
        </div>
        <div className="border-t border-white/8">
          <div className="mx-auto flex w-full max-w-6xl flex-col items-center justify-between gap-3 px-6 py-6 text-sm text-white/40 sm:flex-row">
            <p>© 2026 ft_discord — projet étudiant, École 42</p>
            <div className="flex gap-5">
              <Link href="/terms" className="transition hover:text-white">
                Terms of Service
              </Link>
              <Link href="/privacy" className="transition hover:text-white">
                Privacy Policy
              </Link>
            </div>
          </div>
        </div>
      </footer>
    </div>
  );
}
