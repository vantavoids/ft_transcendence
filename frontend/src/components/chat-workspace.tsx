'use client';

import { useEffect, useMemo, useState } from 'react';
import Link from 'next/link';
import {
  ArrowLeft,
  ArrowRight,
  CircleEllipsis,
  Headphones,
  MessageCircle,
  MicOff,
  Plus,
  Search,
  Settings,
  Smile,
  UserRound,
  Volume2
} from 'lucide-react';
import { SESSION_USERNAME_KEY } from '../shared/lib/session';

type Channel = {
  id: string;
  name: string;
  type: 'text' | 'voice';
};

type ChatMessage = {
  id: string;
  author: string;
  accent: 'aqua' | 'yellow' | 'lime' | 'lavender' | 'pink';
  content: string[];
  timestamp: string;
};

const textChannels: Channel[] = [
  { id: 'general', name: 'general', type: 'text' },
  { id: 'idk', name: 'idk', type: 'text' },
  { id: 'ideas_are_tough', name: 'ideas_are_tough', type: 'text' }
];

const voiceChannels: Channel[] = [
  { id: 'voice-general', name: 'General', type: 'voice' },
  { id: 'voice-mutinerie', name: 'Mutinerie', type: 'voice' }
];

const initialMessages: Record<string, ChatMessage[]> = {
  general: [
    {
      id: '1',
      author: 'um4ss',
      accent: 'lime',
      content: ['Lorem ipsum dolor sit amet, consectetur adipiscing elit.'],
      timestamp: '20:01'
    },
    {
      id: '2',
      author: 'add',
      accent: 'aqua',
      content: ['Lorem ipsum dolor sit amet, consectetur adipiscing elit.'],
      timestamp: '20:03'
    },
    {
      id: '3',
      author: 'SkyDogzz',
      accent: 'yellow',
      content: [
        'Lorem ipsum dolor sit amet, consectetur adipiscing elit.',
        'Lorem ipsum dolor sit amet, consectetur adipiscing elit.',
        'Lorem ipsum dolor sit amet, consectetur adipiscing elit.'
      ],
      timestamp: '20:08'
    },
    {
      id: '4',
      author: 'Vanta',
      accent: 'lavender',
      content: ['Lorem ipsum dolor sit amet, consectetur adipiscing elit.'],
      timestamp: '20:11'
    }
  ],
  idk: [
    {
      id: '5',
      author: 'Cartoone',
      accent: 'pink',
      content: ['Canal de test prêt pour les messages locaux.'],
      timestamp: '20:15'
    }
  ],
  ideas_are_tough: [
    {
      id: '6',
      author: 'um4ss',
      accent: 'lime',
      content: ['Brainstorm ici.'],
      timestamp: '20:17'
    }
  ]
};

const emojiOptions = ['😀', '😅', '🤣', '😂', '🙂', '🙃', '🤔', '😎', '🥳', '😍', '😘', '😉'];

function getAccentClasses(accent: ChatMessage['accent']) {
  switch (accent) {
    case 'lime':
      return 'bg-lime text-primary-bg';
    case 'aqua':
      return 'bg-aqua text-primary-bg';
    case 'yellow':
      return 'bg-yellow text-primary-bg';
    case 'lavender':
      return 'bg-lavender text-primary-bg';
    default:
      return 'bg-pink text-primary-bg';
  }
}

function getAccentText(accent: ChatMessage['accent']) {
  switch (accent) {
    case 'lime':
      return 'text-lime';
    case 'aqua':
      return 'text-aqua';
    case 'yellow':
      return 'text-yellow';
    case 'lavender':
      return 'text-lavender';
    default:
      return 'text-pink';
  }
}

export function ChatWorkspace() {
  const [activeChannel, setActiveChannel] = useState('general');
  const [search, setSearch] = useState('');
  const [draft, setDraft] = useState('');
  const [isEmojiOpen, setIsEmojiOpen] = useState(false);
  const [mobilePane, setMobilePane] = useState<'channels' | 'messages'>('messages');
  const [username, setUsername] = useState('cartoone');
  const [messagesByChannel, setMessagesByChannel] = useState(initialMessages);

  useEffect(() => {
    const storedUsername = window.localStorage.getItem(SESSION_USERNAME_KEY);
    if (storedUsername) {
      setUsername(storedUsername);
    }
  }, []);

  const filteredChannels = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) {
      return textChannels;
    }
    return textChannels.filter((channel) => channel.name.toLowerCase().includes(term));
  }, [search]);

  const activeMessages = messagesByChannel[activeChannel] ?? [];

  function handleSelectChannel(channelId: string) {
    setActiveChannel(channelId);
    setMobilePane('messages');
  }

  function handleSubmitMessage() {
    const content = draft.trim();
    if (!content) {
      return;
    }

    const nextMessage: ChatMessage = {
      id: `${activeChannel}-${Date.now()}`,
      author: username,
      accent: 'pink',
      content: [content],
      timestamp: new Date().toLocaleTimeString('fr-FR', {
        hour: '2-digit',
        minute: '2-digit'
      })
    };

    setMessagesByChannel((current) => ({
      ...current,
      [activeChannel]: [...(current[activeChannel] ?? []), nextMessage]
    }));
    setDraft('');
    setIsEmojiOpen(false);
  }

  function appendEmoji(emoji: string) {
    setDraft((current) => `${current}${emoji}`);
  }

  return (
    <div className="mx-auto flex min-h-screen w-full max-w-[88rem] gap-4 px-3 py-4 md:px-5 md:py-9">
      <aside className="hidden w-[7.25rem] flex-col rounded-[1rem] bg-secondary-bg px-5 py-6 ring-1 ring-white/5 md:flex">
        <Link href="/" className="mono-detail text-[2rem] font-bold tracking-[-0.06em] text-white">
          Logo<span className="text-aqua">_</span>
        </Link>
        <div className="mx-1 mt-5 border-t border-white/10" />
        <div className="mt-6 flex flex-1 flex-col gap-4">
          {[0, 1, 2, 3].map((index) => (
            <button
              key={index}
              type="button"
              className={`h-[4.9rem] rounded-xl border transition ${
                index === 1
                  ? 'border-aqua shadow-[0_0_0_1px_rgba(120,220,232,0.2)]'
                  : 'border-frame'
              }`}
              aria-label={`Server ${index + 1}`}
            />
          ))}
          <button
            type="button"
            className="flex h-[4.9rem] items-center justify-center rounded-xl bg-panel text-[#535353] transition hover:text-white"
            aria-label="Add server"
          >
            <Plus className="h-8 w-8" strokeWidth={1.5} />
          </button>
        </div>
        <div className="flex justify-center pt-5 text-3xl tracking-[0.4em] text-[#9f9f9f]">...</div>
      </aside>

      <div
        className={`${
          mobilePane === 'channels' ? 'flex' : 'hidden'
        } min-h-0 flex-1 flex-col rounded-[1rem] bg-secondary-bg ring-1 ring-white/5 md:flex md:max-w-[25rem]`}
      >
        <div className="px-4 pb-5 pt-4 sm:px-6">
          <div className="flex items-center justify-between">
            <Link
              href="/"
              className="mono-detail text-[2rem] font-bold tracking-[-0.06em] text-white md:hidden"
            >
              Logo<span className="text-aqua">_</span>
            </Link>
            <h2 className="font-display text-[2rem] font-medium tracking-[-0.05em] text-aqua sm:text-[2.2rem]">
              server_name
            </h2>
            <div className="flex items-center gap-3 text-[#8c8c90]">
              <UserRound className="h-5 w-5" strokeWidth={1.8} />
              <CircleEllipsis className="h-5 w-5" strokeWidth={1.8} />
            </div>
          </div>
          <label className="mt-6 flex h-11 items-center gap-3 rounded-md bg-panel px-4 text-muted">
            <Search className="h-4 w-4" strokeWidth={1.75} />
            <input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Search"
              className="mono-detail w-full bg-transparent text-xl text-white outline-none placeholder:text-muted"
            />
          </label>
          <div className="mt-7 space-y-7">
            <div>
              <p className="font-category text-[0.95rem] uppercase tracking-[0.14em] text-category">
                Text Channels
              </p>
              <div className="mt-4 space-y-3">
                {filteredChannels.map((channel) => {
                  const isActive = channel.id === activeChannel;
                  return (
                    <button
                      key={channel.id}
                      type="button"
                      onClick={() => handleSelectChannel(channel.id)}
                      className={`mono-detail flex h-11 w-full items-center rounded-md px-4 text-left text-[1.05rem] transition ${
                        isActive ? 'bg-frame text-white' : 'text-grey-link hover:bg-frame/60'
                      }`}
                    >
                      <span className="mr-3 text-[#8a8a96]">#</span>
                      <span className={isActive ? 'font-bold' : 'font-normal'}>{channel.name}</span>
                    </button>
                  );
                })}
              </div>
            </div>
            <div>
              <p className="font-category text-[0.95rem] uppercase tracking-[0.14em] text-category">
                Voice Channels
              </p>
              <div className="mt-4 space-y-3">
                {voiceChannels.map((channel) => (
                  <button
                    key={channel.id}
                    type="button"
                    className="flex items-center gap-3 text-grey-link transition hover:text-white"
                  >
                    <Volume2 className="h-4 w-4" strokeWidth={1.8} />
                    <span className="text-[1.9rem] leading-none tracking-[-0.05em]">{channel.name}</span>
                  </button>
                ))}
              </div>
            </div>
          </div>
        </div>
        <div className="mt-auto flex items-center justify-between border-t border-white/8 px-4 py-4">
          <div className="flex items-center gap-3">
            <div className="h-12 w-12 overflow-hidden rounded-md bg-[linear-gradient(135deg,#6e7f9d,#d9e2f0)]" />
            <span className="mono-detail text-[2rem] font-medium tracking-[-0.06em] text-white">
              {username}
            </span>
          </div>
          <div className="flex items-center gap-3 text-pink">
            <MicOff className="h-6 w-6" strokeWidth={1.8} />
            <Headphones className="h-6 w-6 text-[#8b8b8f]" strokeWidth={1.8} />
            <Link href="/profile" className="text-[#8b8b8f] transition hover:text-white">
              <Settings className="h-6 w-6" strokeWidth={1.8} />
            </Link>
          </div>
        </div>
      </div>

      <section
        className={`${
          mobilePane === 'messages' ? 'flex' : 'hidden'
        } min-h-0 flex-1 flex-col rounded-[1rem] bg-secondary-bg ring-1 ring-white/5 md:flex`}
      >
        <div className="flex h-[4.9rem] items-center justify-between border-b border-white/8 px-5 sm:px-7">
          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={() => setMobilePane('channels')}
              className="flex h-11 w-11 items-center justify-center rounded-xl border border-frame text-[#7e7e82] md:hidden"
              aria-label="Show channels"
            >
              <ArrowLeft className="h-5 w-5" strokeWidth={1.9} />
            </button>
            <h2 className="mono-detail text-[1.85rem] font-bold tracking-[-0.05em] text-white">
              # {activeChannel}
            </h2>
          </div>
          <div className="flex items-center gap-4 text-[#8c8c90]">
            <UserRound className="h-5 w-5" strokeWidth={1.8} />
            <CircleEllipsis className="h-5 w-5" strokeWidth={1.8} />
          </div>
        </div>

        <div className="min-h-0 flex-1 overflow-auto px-5 py-7 sm:px-7">
          <div className="space-y-7">
            {activeMessages.map((message) => (
              <article key={message.id} className="flex gap-4">
                <div
                  className={`flex h-12 w-12 shrink-0 items-center justify-center rounded-full text-xl font-semibold ${getAccentClasses(
                    message.accent
                  )}`}
                >
                  {message.author.slice(0, 1).toUpperCase()}
                </div>
                <div className="min-w-0">
                  <div className="flex items-end gap-3">
                    <h3 className={`text-[2rem] font-bold tracking-[-0.06em] ${getAccentText(message.accent)}`}>
                      {message.author}
                    </h3>
                    <span className="mono-detail pb-1 text-xs text-white/35">{message.timestamp}</span>
                  </div>
                  <div className="mt-1 space-y-2 text-[1.05rem] text-white/80 sm:text-[1.15rem]">
                    {message.content.map((line, index) => (
                      <p key={`${message.id}-${index}`}>{line}</p>
                    ))}
                  </div>
                </div>
              </article>
            ))}
          </div>
        </div>

        <div className="border-t border-white/8 px-4 py-4 sm:px-5">
          {isEmojiOpen ? (
            <div className="mb-3 rounded-xl border border-white/10 bg-panel p-3">
              <div className="grid grid-cols-6 gap-2">
                {emojiOptions.map((emoji) => (
                  <button
                    key={emoji}
                    type="button"
                    onClick={() => appendEmoji(emoji)}
                    className="rounded-lg bg-frame px-2 py-2 text-2xl transition hover:bg-white/10"
                  >
                    {emoji}
                  </button>
                ))}
              </div>
            </div>
          ) : null}
          <div className="flex h-14 items-center rounded-md bg-panel px-4 text-muted">
            <input
              value={draft}
              onChange={(event) => setDraft(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === 'Enter') {
                  event.preventDefault();
                  handleSubmitMessage();
                }
              }}
              placeholder={`Message #${activeChannel}`}
              className="w-full bg-transparent text-lg text-white outline-none placeholder:text-muted"
            />
            <div className="ml-auto flex items-center gap-4">
              <button
                type="button"
                onClick={() => setIsEmojiOpen((current) => !current)}
                className="text-[#7e7e82] transition hover:text-white"
                aria-label="Toggle emoji picker"
              >
                <Smile className="h-5 w-5" strokeWidth={1.8} />
              </button>
              <button
                type="button"
                onClick={handleSubmitMessage}
                className="text-aqua transition hover:text-white"
                aria-label="Send message"
              >
                <ArrowRight className="h-5 w-5 rounded-full border border-aqua p-0.5" strokeWidth={2} />
              </button>
            </div>
          </div>
          <div className="mt-3 flex justify-between text-xs text-white/35">
            <span>Canal interactif local</span>
            <button
              type="button"
              onClick={() => setMobilePane('channels')}
              className="inline-flex items-center gap-2 md:hidden"
            >
              <MessageCircle className="h-4 w-4" strokeWidth={1.8} />
              Channels
            </button>
          </div>
        </div>
      </section>
    </div>
  );
}
