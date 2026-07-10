import Link from 'next/link';
import { cookies } from 'next/headers';
import { ArrowRight, Bell, Hash, MessageCircle, Video } from 'lucide-react';
import { OAuthHandoff } from '../src/components/oauth-handoff';
import { LogoutButton } from '../src/components/logout-button';
import { SESSION_COOKIE_KEY } from '../src/shared/lib/session';

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

export default async function HomePage() {
  const isLoggedIn = (await cookies()).has(SESSION_COOKIE_KEY);

  return (
    <div className="h-screen overflow-y-auto">
      <OAuthHandoff />

      <header className="mx-auto flex w-full max-w-6xl items-center justify-between px-6 py-6">
        <Link href="/" className="text-xl font-extrabold tracking-[-0.05em] text-white">
          ft_discord
        </Link>
        {isLoggedIn ? (
          <LogoutButton />
        ) : (
          <Link
            href="/auth/login"
            className="rounded-full bg-white px-5 py-2 text-sm font-bold text-primary-bg transition hover:bg-aqua"
          >
            Login
          </Link>
        )}
      </header>

      <section className="mx-auto w-full max-w-6xl px-6 pb-20 pt-14 text-center md:pt-24">
        <p className="mono-detail text-aqua">ft_transcendence · École 42</p>
        <h1 className="mx-auto mt-5 max-w-4xl text-5xl font-extrabold uppercase tracking-[-0.05em] text-white md:text-8xl">
          Un endroit pour échanger et se retrouver
        </h1>
        <p className="mx-auto mt-6 max-w-2xl text-lg text-white/65">
          ft_discord est une plateforme de communication en temps réel : guildes, salons, messages
          directs, amis et appels. Un projet étudiant, mais une vraie place pour ta communauté.
        </p>
        <div className="mt-10 flex flex-col items-center justify-center gap-4 sm:flex-row">
          {isLoggedIn ? (
            <Link
              href="/chat"
              className="inline-flex items-center gap-2 rounded-full bg-aqua px-7 py-3.5 text-base font-bold text-primary-bg shadow-glow transition hover:bg-white"
            >
              Ouvrir ft_discord dans le navigateur
              <ArrowRight className="h-4 w-4" strokeWidth={2.5} />
            </Link>
          ) : (
            <Link
              href="/auth/register"
              className="inline-flex items-center gap-2 rounded-full bg-aqua px-7 py-3.5 text-base font-bold text-primary-bg shadow-glow transition hover:bg-white"
            >
              Créer un compte
              <ArrowRight className="h-4 w-4" strokeWidth={2.5} />
            </Link>
          )}
        </div>
      </section>

      <section className="mx-auto w-full max-w-6xl px-6 pb-24">
        <div className="grid gap-4 md:grid-cols-2">
          {features.map((feature) => (
            <div
              key={feature.title}
              className="rounded-[2rem] border border-white/8 bg-secondary-bg/90 p-8 shadow-2xl shadow-black/40 backdrop-blur md:p-10"
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

      <footer className="border-t border-white/8 bg-secondary-bg/70">
        <div className="mx-auto flex w-full max-w-6xl flex-col items-center justify-between gap-3 px-6 py-6 text-sm text-white/40 sm:flex-row">
          <p>© 2026 ft_discord - projet étudiant, École 42</p>
          <div className="flex gap-5">
            <Link href="/terms" className="transition hover:text-white">
              Terms of Service
            </Link>
            <Link href="/privacy" className="transition hover:text-white">
              Privacy Policy
            </Link>
          </div>
        </div>
      </footer>
    </div>
  );
}
