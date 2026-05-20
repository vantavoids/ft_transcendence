'use client';

import { useEffect, useMemo, useRef, useState } from 'react';
import {
  ArrowLeft,
  ArrowRight,
  CircleEllipsis,
  MessageCircle,
  Smile,
  UserRound
} from 'lucide-react';
import { ChannelList, getChannelName } from './channel-list';
import { GuildSidebar } from './guild-sidebar';
import { SESSION_USERNAME_KEY } from '../shared/lib/session';

type ChatMessage = {
  id: string;
  author: string;
  accent: 'aqua' | 'yellow' | 'lime' | 'lavender' | 'pink';
  content: string[];
  timestamp: string;
};

type Member = {
  id: string;
  name: string;
  status: 'online' | 'idle' | 'offline';
  accent: ChatMessage['accent'];
  role: 'owner' | 'member';
};

const serverMembers: Member[] = [
  { id: 'um4ss', name: 'um4ss', status: 'online', accent: 'lime', role: 'owner' },
  { id: 'add', name: 'add', status: 'online', accent: 'aqua', role: 'member' },
  { id: 'skydogzz', name: 'SkyDogzz', status: 'idle', accent: 'yellow', role: 'member' },
  { id: 'vanta', name: 'Vanta', status: 'offline', accent: 'lavender', role: 'member' }
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

function getStatusClasses(status: Member['status']) {
  switch (status) {
    case 'online':
      return 'bg-lime';
    case 'idle':
      return 'bg-yellow';
    default:
      return 'bg-muted';
  }
}

export function ChatWorkspace() {
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const [activeChannel, setActiveChannel] = useState('general');
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

  const activeMessages = messagesByChannel[activeChannel] ?? [];
  const activeChannelName = getChannelName(activeChannel);
  const members = useMemo<Member[]>(
    () => [
      { id: 'current-user', name: username, status: 'online', accent: 'pink', role: 'member' },
      ...serverMembers
    ],
    [username]
  );

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ block: 'end', behavior: 'smooth' });
  }, [activeChannel, activeMessages.length]);

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
      content: content.split(/\r?\n/),
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
    <div className="mx-auto flex h-screen w-full gap-4 px-3 py-4 md:px-5 md:py-9">
      <GuildSidebar />

      <ChannelList
        activeChannel={activeChannel}
        mobilePane={mobilePane}
        username={username}
        onSelectChannel={handleSelectChannel}
      />

      <section
        className={`${
          mobilePane === 'messages' ? 'flex' : 'hidden'
        } min-h-0 flex-1 flex-col overflow-hidden rounded-[1rem] bg-secondary-bg ring-1 ring-white/5 md:flex`}
      >
        <div className="flex h-[4.9rem] shrink-0 items-center justify-between border-b border-white/8 px-5 sm:px-7">
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
              # {activeChannelName}
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
                    <h3
                      className={`text-[1.5rem] font-bold tracking-[-0.06em] ${getAccentText(message.accent)}`}
                    >
                      {message.author}
                    </h3>
                    <span className="mono-detail pb-2 text-xs text-white/35">
                      {message.timestamp}
                    </span>
                  </div>
                  <div className="mt-1 space-y-2 text-[1.05rem] text-white/80 sm:text-[1.15rem]">
                    {message.content.map((line, index) => (
                      <p key={`${message.id}-${index}`} className="break-words">
                        {line}
                      </p>
                    ))}
                  </div>
                </div>
              </article>
            ))}
            <div ref={messagesEndRef} />
          </div>
        </div>

        <div className="shrink-0 border-t border-white/8 px-4 py-4 sm:px-5">
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
            <textarea
              value={draft}
              onChange={(event) => setDraft(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === 'Enter' && !event.shiftKey) {
                  event.preventDefault();
                  handleSubmitMessage();
                }
              }}
              placeholder={`Message #${activeChannelName}`}
              rows={1}
              className="h-full min-h-0 w-full resize-none overflow-y-auto bg-transparent py-4 text-lg leading-6 text-white outline-none placeholder:text-muted"
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
                <ArrowRight
                  className="h-5 w-5 rounded-full border border-aqua p-0.5"
                  strokeWidth={2}
                />
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

      <aside className="hidden min-h-0 w-[20rem] shrink-0 flex-col overflow-hidden rounded-[1rem] bg-secondary-bg ring-1 ring-white/5 xl:flex">
        <div className="flex h-[4.9rem] shrink-0 items-center justify-between border-b border-white/8 px-5">
          <div>
            <p className="font-category text-[0.78rem] uppercase tracking-[0.18em] text-category">
              Members
            </p>
            <h2 className="mt-1 text-[1.25rem] font-bold tracking-[-0.03em] text-white">
              {members.length} members
            </h2>
          </div>
          <UserRound className="h-5 w-5 text-[#8c8c90]" strokeWidth={1.8} />
        </div>

        <div className="min-h-0 flex-1 overflow-auto px-4 py-5">
          <div className="space-y-2">
            {members.map((member) => (
              <button
                key={member.id}
                type="button"
                className="flex h-14 w-full items-center gap-3 rounded-lg px-3 text-left transition hover:bg-frame/70"
              >
                <span className="relative shrink-0">
                  <span
                    className={`flex h-10 w-10 items-center justify-center rounded-full text-sm font-bold ${getAccentClasses(
                      member.accent
                    )}`}
                  >
                    {member.name.slice(0, 1).toUpperCase()}
                  </span>
                  <span
                    className={`absolute -bottom-0.5 -right-0.5 h-3.5 w-3.5 rounded-full border-2 border-secondary-bg ${getStatusClasses(
                      member.status
                    )}`}
                  />
                </span>
                <span className="min-w-0 flex-1">
                  <span className="block truncate text-[0.95rem] font-semibold text-white">
                    {member.name}
                  </span>
                  <span className="font-category block text-[0.7rem] uppercase tracking-[0.14em] text-white/35">
                    {member.role}
                  </span>
                </span>
              </button>
            ))}
          </div>
        </div>
      </aside>
    </div>
  );
}
