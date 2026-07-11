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
    title: 'Guilds and channels',
    text: 'Create your guild, organize it into categories and channels, and keep every topic in its place. Roles, permissions, and moderation are built in.'
  },
  {
    icon: MessageCircle,
    accent: 'text-lavender',
    title: 'Direct messages and friends',
    text: 'Chat in real time with friends, manage requests and blocks, and pick up your conversations exactly where you left them.'
  },
  {
    icon: Video,
    accent: 'text-pink',
    title: 'Voice and video calls',
    text: 'Start an audio or video call in one click, straight from a conversation, with peer-to-peer WebRTC.'
  },
  {
    icon: Bell,
    accent: 'text-yellow',
    title: 'Real-time notifications',
    text: 'Mentions, friend requests, and invites arrive instantly over WebSocket without reloading the page.'
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
            Log in
          </Link>
        )}
      </header>

      <section className="mx-auto w-full max-w-6xl px-6 pb-20 pt-14 text-center md:pt-24">
        <p className="mono-detail text-aqua">ft_transcendence · 42</p>
        <h1 className="mx-auto mt-5 max-w-4xl text-5xl font-extrabold uppercase tracking-[-0.05em] text-white md:text-8xl">
          A place to talk and reconnect
        </h1>
        <p className="mx-auto mt-6 max-w-2xl text-lg text-white/65">
          ft_discord is a real-time communication platform: guilds, channels, direct messages,
          friends, and calls. It is a student project, but it is still a real place for your
          community.
        </p>
        <div className="mt-10 flex flex-col items-center justify-center gap-4 sm:flex-row">
          {isLoggedIn ? (
            <Link
              href="/chat"
            className="inline-flex items-center gap-2 rounded-full bg-aqua px-7 py-3.5 text-base font-bold text-primary-bg shadow-glow transition hover:bg-white"
          >
              Open ft_discord in the browser
              <ArrowRight className="h-4 w-4" strokeWidth={2.5} />
            </Link>
          ) : (
            <Link
              href="/auth/register"
            className="inline-flex items-center gap-2 rounded-full bg-aqua px-7 py-3.5 text-base font-bold text-primary-bg shadow-glow transition hover:bg-white"
          >
              Create an account
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
              className="rounded-[2rem] border border-stroke bg-secondary-bg/90 p-8 shadow-2xl shadow-black/40 backdrop-blur md:p-10"
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

      <footer className="border-t border-stroke bg-secondary-bg/70">
        <div className="mx-auto flex w-full max-w-6xl flex-col items-center justify-between gap-3 px-6 py-6 text-sm text-white/40 sm:flex-row">
          <p>© 2026 ft_discord - student project, 42</p>
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
