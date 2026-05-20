'use client';

import { useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react';
import {
  ArrowLeft,
  ArrowRight,
  CircleEllipsis,
  MessageCircle,
  Smile,
  UserRound
} from 'lucide-react';
import { ChatMessage, getAccentClasses, type ChatMessageData } from './chat-message';
import { ChannelList, getChannelName, hasChannel } from './channel-list';
import { GuildSidebar } from './guild-sidebar';
import { SESSION_USERNAME_KEY } from '../shared/lib/session';

const LAST_CHAT_CHANNEL_KEY = 'ft_transcendence_last_chat_channel';
const BOTTOM_THRESHOLD_PX = 96;
const MESSAGE_GROUP_THRESHOLD_MINUTES = 5;

type ChannelScrollPosition = {
  messageId: string;
  topOffset: number;
};

type Member = {
  id: string;
  name: string;
  status: 'online' | 'idle' | 'offline';
  accent: ChatMessageData['accent'];
  role: 'owner' | 'member';
};

const serverMembers: Member[] = [
  { id: 'um4ss', name: 'um4ss', status: 'online', accent: 'lime', role: 'owner' },
  { id: 'add', name: 'add', status: 'online', accent: 'aqua', role: 'member' },
  { id: 'skydogzz', name: 'SkyDogzz', status: 'idle', accent: 'yellow', role: 'member' },
  { id: 'vanta', name: 'Vanta', status: 'offline', accent: 'lavender', role: 'member' }
];

const initialMessages: Record<string, ChatMessageData[]> = {
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

function getTimestampMinutes(timestamp: string) {
  const [hours, minutes] = timestamp.split(':').map(Number);

  if (!Number.isFinite(hours) || !Number.isFinite(minutes)) {
    return null;
  }

  return hours * 60 + minutes;
}

function getMinutesBetween(previousTimestamp: string, currentTimestamp: string) {
  const previousMinutes = getTimestampMinutes(previousTimestamp);
  const currentMinutes = getTimestampMinutes(currentTimestamp);

  if (previousMinutes === null || currentMinutes === null) {
    return Number.POSITIVE_INFINITY;
  }

  return currentMinutes >= previousMinutes
    ? currentMinutes - previousMinutes
    : currentMinutes + 24 * 60 - previousMinutes;
}

export function ChatWorkspace() {
  const messagesViewportRef = useRef<HTMLDivElement>(null);
  const messageRefs = useRef<Record<string, HTMLElement | null>>({});
  const channelScrollPositions = useRef<Record<string, ChannelScrollPosition>>({});
  const pendingScrollBottom = useRef(false);
  const isRestoringScroll = useRef(false);
  const [activeChannel, setActiveChannel] = useState<string | null>(null);
  const [draft, setDraft] = useState('');
  const [isEmojiOpen, setIsEmojiOpen] = useState(false);
  const [isNearBottom, setIsNearBottom] = useState(true);
  const [editingMessageId, setEditingMessageId] = useState<string | null>(null);
  const [editingDraft, setEditingDraft] = useState('');
  const [mobilePane, setMobilePane] = useState<'channels' | 'messages'>('messages');
  const [username, setUsername] = useState('cartoone');
  const [messagesByChannel, setMessagesByChannel] = useState(initialMessages);

  useEffect(() => {
    const storedUsername = window.localStorage.getItem(SESSION_USERNAME_KEY);
    if (storedUsername) {
      setUsername(storedUsername);
    }

    const storedChannel = window.sessionStorage.getItem(LAST_CHAT_CHANNEL_KEY);
    setActiveChannel(storedChannel && hasChannel(storedChannel) ? storedChannel : 'general');
  }, []);

  const activeMessages = activeChannel ? (messagesByChannel[activeChannel] ?? []) : [];
  const activeMessageItems = useMemo(
    () =>
      activeMessages.map((message, index) => {
        const previousMessage = activeMessages[index - 1];
        const isGrouped =
          previousMessage?.author === message.author &&
          getMinutesBetween(previousMessage.timestamp, message.timestamp) <=
            MESSAGE_GROUP_THRESHOLD_MINUTES;

        return { message, isGrouped };
      }),
    [activeMessages]
  );
  const activeChannelName = activeChannel ? getChannelName(activeChannel) : '';
  const members = useMemo<Member[]>(
    () => [
      { id: 'current-user', name: username, status: 'online', accent: 'pink', role: 'member' },
      ...serverMembers
    ],
    [username]
  );

  const updateNearBottomState = useCallback(() => {
    const viewport = messagesViewportRef.current;
    if (!viewport) {
      setIsNearBottom(true);
      return;
    }

    const distanceFromBottom = viewport.scrollHeight - viewport.scrollTop - viewport.clientHeight;
    setIsNearBottom(distanceFromBottom <= BOTTOM_THRESHOLD_PX);
  }, []);

  const rememberChannelScrollPosition = useCallback(
    (channelId: string | null) => {
      if (!channelId || isRestoringScroll.current) {
        return;
      }

      const viewport = messagesViewportRef.current;
      if (!viewport) {
        return;
      }

      const viewportTop = viewport.getBoundingClientRect().top;
      const visibleMessage = (messagesByChannel[channelId] ?? [])
        .map((message) => {
          const element = messageRefs.current[message.id];
          if (!element) {
            return null;
          }

          return {
            element,
            id: message.id,
            top: element.getBoundingClientRect().top
          };
        })
        .filter((message): message is { element: HTMLElement; id: string; top: number } => {
          if (!message) {
            return false;
          }

          return message.element.getBoundingClientRect().bottom >= viewportTop;
        })
        .sort((a, b) => Math.abs(a.top - viewportTop) - Math.abs(b.top - viewportTop))[0];

      if (!visibleMessage) {
        return;
      }

      channelScrollPositions.current[channelId] = {
        messageId: visibleMessage.id,
        topOffset: visibleMessage.top - viewportTop
      };
    },
    [messagesByChannel]
  );

  useLayoutEffect(() => {
    if (!activeChannel) {
      return;
    }

    const viewport = messagesViewportRef.current;
    if (!viewport) {
      return;
    }

    isRestoringScroll.current = true;

    if (pendingScrollBottom.current) {
      viewport.scrollTop = viewport.scrollHeight;
      pendingScrollBottom.current = false;
    } else {
      const savedPosition = channelScrollPositions.current[activeChannel];
      const savedElement = savedPosition ? messageRefs.current[savedPosition.messageId] : null;

      if (savedPosition && savedElement) {
        const viewportTop = viewport.getBoundingClientRect().top;
        const elementTop = savedElement.getBoundingClientRect().top;
        viewport.scrollTop += elementTop - viewportTop - savedPosition.topOffset;
      } else {
        viewport.scrollTop = viewport.scrollHeight;
      }
    }

    window.requestAnimationFrame(() => {
      isRestoringScroll.current = false;
      rememberChannelScrollPosition(activeChannel);
      updateNearBottomState();
    });
  }, [activeChannel, activeMessages.length, rememberChannelScrollPosition, updateNearBottomState]);

  function handleMessagesScroll() {
    rememberChannelScrollPosition(activeChannel);
    updateNearBottomState();
  }

  function handleJumpToBottom() {
    const viewport = messagesViewportRef.current;
    if (!viewport) {
      return;
    }

    viewport.scrollTo({ top: viewport.scrollHeight, behavior: 'smooth' });
  }

  function handleSelectChannel(channelId: string) {
    rememberChannelScrollPosition(activeChannel);
    setActiveChannel(channelId);
    window.sessionStorage.setItem(LAST_CHAT_CHANNEL_KEY, channelId);
    setMobilePane('messages');
  }

  function handleSubmitMessage() {
    const content = draft.trim();
    if (!content || !activeChannel) {
      return;
    }

    const nextMessage: ChatMessageData = {
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
    pendingScrollBottom.current = true;
    setDraft('');
    setIsEmojiOpen(false);
  }

  function appendEmoji(emoji: string) {
    setDraft((current) => `${current}${emoji}`);
  }

  function handleToggleReaction(messageId: string) {
    if (!activeChannel) {
      return;
    }

    setMessagesByChannel((current) => ({
      ...current,
      [activeChannel]: (current[activeChannel] ?? []).map((message) => {
        if (message.id !== messageId) {
          return message;
        }

        const currentCount = message.reactions?.['👍'] ?? 0;
        const nextReactions = { ...message.reactions };

        if (currentCount > 0) {
          delete nextReactions['👍'];
        } else {
          nextReactions['👍'] = 1;
        }

        return {
          ...message,
          reactions: Object.keys(nextReactions).length > 0 ? nextReactions : undefined
        };
      })
    }));
  }

  function handleStartEdit(message: ChatMessageData) {
    setEditingMessageId(message.id);
    setEditingDraft(message.content.join('\n'));
  }

  function handleCancelEdit() {
    setEditingMessageId(null);
    setEditingDraft('');
  }

  function handleSaveEdit(messageId: string) {
    if (!activeChannel) {
      return;
    }

    const content = editingDraft.trim();
    if (!content) {
      return;
    }

    setMessagesByChannel((current) => ({
      ...current,
      [activeChannel]: (current[activeChannel] ?? []).map((message) =>
        message.id === messageId ? { ...message, content: content.split(/\r?\n/) } : message
      )
    }));
    handleCancelEdit();
  }

  function handleDeleteMessage(messageId: string) {
    if (!activeChannel) {
      return;
    }

    setMessagesByChannel((current) => ({
      ...current,
      [activeChannel]: (current[activeChannel] ?? []).filter((message) => message.id !== messageId)
    }));

    if (editingMessageId === messageId) {
      handleCancelEdit();
    }
  }

  function setMessageRef(messageId: string, element: HTMLElement | null) {
    messageRefs.current[messageId] = element;
  }

  return (
    <div className="mx-auto flex h-screen w-full gap-4 px-3 py-4 md:px-5 md:py-9">
      <GuildSidebar />

      <ChannelList
        activeChannel={activeChannel ?? ''}
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

        <div
          ref={messagesViewportRef}
          onScroll={handleMessagesScroll}
          className="min-h-0 flex-1 overflow-auto px-5 py-7 sm:px-7"
        >
          <div>
            {activeMessageItems.map(({ message, isGrouped }) => {
              const isOwnMessage = message.author.toLowerCase() === username.toLowerCase();
              const isEditing = editingMessageId === message.id;

              return (
                <ChatMessage
                  key={message.id}
                  message={message}
                  isGrouped={isGrouped}
                  isOwnMessage={isOwnMessage}
                  isEditing={isEditing}
                  editingDraft={editingDraft}
                  onEditDraftChange={setEditingDraft}
                  onStartEdit={handleStartEdit}
                  onSaveEdit={handleSaveEdit}
                  onCancelEdit={handleCancelEdit}
                  onDelete={handleDeleteMessage}
                  onToggleReaction={handleToggleReaction}
                  setMessageRef={setMessageRef}
                />
              );
            })}
          </div>
          {!isNearBottom ? (
            <button
              type="button"
              onClick={handleJumpToBottom}
              className="mono-detail sticky bottom-0 z-10 ml-auto flex h-10 items-center rounded-full border border-aqua/40 bg-panel px-4 text-sm font-bold text-aqua shadow-lg shadow-black/30 transition hover:border-aqua hover:text-white"
            >
              Jump to bottom
            </button>
          ) : null}
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
