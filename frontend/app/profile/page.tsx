'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { CalendarDays, Gamepad2, Hash, LogOut, Mic2, Settings, Users2 } from 'lucide-react';
import { clearFakeSession, SESSION_USERNAME_KEY } from '../../src/shared/lib/session';

const roles = [
  { name: 'Admin', tone: 'bg-pink/18 text-pink ring-pink/25' },
  { name: 'Frontend', tone: 'bg-aqua/18 text-aqua ring-aqua/25' },
  { name: 'Ranked', tone: 'bg-yellow/18 text-yellow ring-yellow/25' },
  { name: 'Playtester', tone: 'bg-lime/18 text-lime ring-lime/25' }
];

const mutualServers = ['ft_transcendence', 'matchmaking-lab', 'pong-balance', 'ui-scrims'];

const activity = [
  {
    icon: Hash,
    title: 'Posted in #general',
    detail: 'Pushed the auth flow polish and profile refresh.'
  },
  {
    icon: Mic2,
    title: 'Joined Mutinerie',
    detail: 'Stayed in voice for 1h 24m during playtest review.'
  },
  {
    icon: Gamepad2,
    title: 'Queued ranked pong',
    detail: '3 matches played tonight with 2 wins.'
  }
];

export default function ProfilePage() {
  const router = useRouter();
  const [username, setUsername] = useState('player');

  useEffect(() => {
    const storedUsername = window.localStorage.getItem(SESSION_USERNAME_KEY);
    if (storedUsername) {
      setUsername(storedUsername);
    }
  }, []);

  function handleFakeDisconnect() {
    clearFakeSession();
    router.push('/auth/login');
    router.refresh();
  }

  return (
    <section className="mx-auto min-h-screen w-full max-w-7xl px-4 py-6 sm:px-6 lg:px-8">
      <div className="mb-6 flex items-center justify-between">
        <Link href="/" className="mono-detail text-[1.8rem] font-bold tracking-[-0.06em] text-white">
          Logo<span className="text-aqua">_</span>
        </Link>
        <div className="flex items-center gap-3 text-white/50">
          <Link href="/chat" className="rounded-lg border border-white/8 bg-panel px-3 py-2 transition hover:text-white">
            Chat
          </Link>
        </div>
      </div>

      <div className="overflow-hidden rounded-[1.75rem] border border-white/8 bg-secondary-bg shadow-2xl shadow-black/35">
        <div className="h-44 bg-[radial-gradient(circle_at_12%_18%,rgba(255,216,102,0.24),transparent_18%),radial-gradient(circle_at_80%_22%,rgba(120,220,232,0.28),transparent_22%),radial-gradient(circle_at_65%_78%,rgba(255,97,136,0.22),transparent_24%),linear-gradient(135deg,#24324a_0%,#1b1f34_45%,#121318_100%)]" />

        <div className="px-5 pb-5 sm:px-7">
          <div className="-mt-16 grid gap-6 lg:grid-cols-[minmax(0,1fr)_21rem]">
            <div className="min-w-0 rounded-[1.5rem] border border-white/8 bg-panel/95 p-5 shadow-[0_18px_45px_rgba(0,0,0,0.32)] sm:p-6">
              <div className="flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between">
                <div className="flex min-w-0 items-end gap-4">
                  <div className="relative h-28 w-28 shrink-0 rounded-[1.6rem] border-[6px] border-panel bg-[linear-gradient(135deg,#78dce8,#ab9df2,#ff6188)] shadow-xl shadow-black/35">
                    <div className="absolute bottom-2 right-2 h-5 w-5 rounded-full border-4 border-panel bg-lime" />
                  </div>
                  <div className="min-w-0 pb-1">
                    <h1 className="truncate text-[2.25rem] font-extrabold tracking-[-0.05em] text-white">
                      {username}
                    </h1>
                    <p className="mono-detail mt-1 text-white/40">{username}#4242</p>
                  </div>
                </div>

                <div className="flex flex-wrap gap-3">
                  <button
                    type="button"
                    className="rounded-xl bg-aqua px-4 py-2.5 font-semibold text-primary-bg transition hover:brightness-105"
                  >
                    Add Friend
                  </button>
                  <button
                    type="button"
                    className="rounded-xl border border-white/10 bg-white/5 px-4 py-2.5 font-semibold text-white transition hover:bg-white/10"
                  >
                    Message
                  </button>
                  <button
                    type="button"
                    className="flex items-center gap-2 rounded-xl border border-white/10 bg-white/5 px-4 py-2.5 font-semibold text-white transition hover:bg-white/10"
                  >
                    <Settings className="h-4 w-4" strokeWidth={1.8} />
                    Edit
                  </button>
                  <button
                    type="button"
                    onClick={handleFakeDisconnect}
                    className="flex items-center gap-2 rounded-xl border border-pink/25 bg-pink/10 px-4 py-2.5 font-semibold text-pink transition hover:border-pink/40 hover:bg-pink/15"
                  >
                    <LogOut className="h-4 w-4" strokeWidth={1.8} />
                    Disconnect
                  </button>
                </div>
              </div>

              <div className="mt-6 grid gap-5 xl:grid-cols-[minmax(0,1fr)_16rem]">
                <div className="rounded-[1.2rem] border border-white/8 bg-secondary-bg/80 p-5">
                  <p className="font-category text-[0.82rem] uppercase tracking-[0.18em] text-white/35">
                    About Me
                  </p>
                  <p className="mt-3 text-white/72">
                    Frontend engineer on ft_transcendence. I build auth, profile UI, realtime
                    screens and interface polish for the game client.
                  </p>

                  <div className="mt-5 border-t border-white/8 pt-5">
                    <p className="font-category text-[0.82rem] uppercase tracking-[0.18em] text-white/35">
                      Member Since
                    </p>
                    <div className="mt-3 flex items-center gap-3 text-white/72">
                      <CalendarDays className="h-4 w-4 text-aqua" strokeWidth={1.8} />
                      <span>Jun 12, 2024</span>
                    </div>
                  </div>

                  <div className="mt-5 border-t border-white/8 pt-5">
                    <p className="font-category text-[0.82rem] uppercase tracking-[0.18em] text-white/35">
                      Note
                    </p>
                    <div className="mt-3 rounded-xl border border-white/8 bg-panel px-4 py-3 text-white/45">
                      Click to add a note
                    </div>
                  </div>
                </div>

                <div className="rounded-[1.2rem] border border-white/8 bg-secondary-bg/80 p-5">
                  <p className="font-category text-[0.82rem] uppercase tracking-[0.18em] text-white/35">
                    Roles
                  </p>
                  <div className="mt-4 flex flex-wrap gap-2">
                    {roles.map((role) => (
                      <span
                        key={role.name}
                        className={`rounded-lg px-3 py-2 text-sm font-semibold ring-1 ${role.tone}`}
                      >
                        {role.name}
                      </span>
                    ))}
                  </div>

                  <div className="mt-6 border-t border-white/8 pt-5">
                    <p className="font-category text-[0.82rem] uppercase tracking-[0.18em] text-white/35">
                      Connections
                    </p>
                    <div className="mt-4 grid grid-cols-3 gap-3">
                      {['GH', '42', 'TW'].map((item) => (
                        <div
                          key={item}
                          className="flex h-12 items-center justify-center rounded-xl border border-white/8 bg-panel font-bold text-white/72"
                        >
                          {item}
                        </div>
                      ))}
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <aside className="space-y-5">
              <div className="rounded-[1.5rem] border border-white/8 bg-panel/95 p-5 shadow-[0_18px_45px_rgba(0,0,0,0.28)]">
                <div className="flex items-center gap-3">
                  <Users2 className="h-5 w-5 text-aqua" strokeWidth={1.8} />
                  <h2 className="text-[1.25rem] font-bold tracking-[-0.03em] text-white">
                    Mutual Servers
                  </h2>
                </div>
                <div className="mt-4 space-y-3">
                  {mutualServers.map((server) => (
                    <div
                      key={server}
                      className="flex items-center gap-3 rounded-xl border border-white/8 bg-secondary-bg/80 px-4 py-3"
                    >
                      <div className="h-10 w-10 rounded-xl bg-[linear-gradient(135deg,#78dce8,#ab9df2)]" />
                      <div className="min-w-0">
                        <p className="truncate font-semibold text-white">{server}</p>
                        <p className="text-sm text-white/45">online now</p>
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              <div className="rounded-[1.5rem] border border-white/8 bg-panel/95 p-5 shadow-[0_18px_45px_rgba(0,0,0,0.28)]">
                <h2 className="text-[1.25rem] font-bold tracking-[-0.03em] text-white">
                  Activity
                </h2>
                <div className="mt-4 space-y-3">
                  {activity.map((item) => {
                    const Icon = item.icon;
                    return (
                      <div
                        key={item.title}
                        className="rounded-xl border border-white/8 bg-secondary-bg/80 px-4 py-4"
                      >
                        <div className="flex items-start gap-3">
                          <div className="mt-0.5 rounded-lg bg-aqua/12 p-2 text-aqua">
                            <Icon className="h-4 w-4" strokeWidth={1.8} />
                          </div>
                          <div>
                            <p className="font-semibold text-white">{item.title}</p>
                            <p className="mt-1 text-sm text-white/52">{item.detail}</p>
                          </div>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            </aside>
          </div>
        </div>
      </div>
    </section>
  );
}
