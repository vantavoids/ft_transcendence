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
import { ChatMessage, type ChatMessageData } from './chat-message';
import { ChannelList, getChannelName, hasChannel } from './channel-list';
import { DmList, getDmName, hasDm } from './dm-list';
import { GuildSidebar } from './guild-sidebar';
import { SESSION_USERNAME_KEY } from '../shared/lib/session';

const LAST_CHAT_MODE_KEY = 'ft_transcendence_last_chat_mode';
const LAST_CHAT_CHANNEL_KEY = 'ft_transcendence_last_chat_channel';
const LAST_CHAT_DM_KEY = 'ft_transcendence_last_chat_dm';
const BOTTOM_THRESHOLD_PX = 96;
const MESSAGE_GROUP_THRESHOLD_MINUTES = 5;

type ChatMode = 'guild' | 'dm';

type ChannelScrollPosition = {
  messageId: string;
  topOffset: number;
};

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
  ],
  'dm-skydogzz': [
    {
      id: 'dm-skydogzz-1',
      author: 'SkyDogzz',
      accent: 'yellow',
      content: ['On teste la nouvelle view DM ici.'],
      timestamp: '19:42'
    },
    {
      id: 'dm-skydogzz-2',
      author: 'cartoone',
      accent: 'pink',
      content: ['Oui, la colonne de gauche doit juste lister les DMs.'],
      timestamp: '19:44'
    }
  ],
  'dm-add': [
    {
      id: 'dm-add-1',
      author: 'add',
      accent: 'aqua',
      content: ['Je passe après le build.'],
      timestamp: '18:12'
    }
  ],
  'dm-um4ss': [
    {
      id: 'dm-um4ss-1',
      author: 'um4ss',
      accent: 'lime',
      content: ['Ping quand tu peux.'],
      timestamp: '17:58'
    }
  ],
  'dm-vanta': [
    {
      id: 'dm-vanta-1',
      author: 'Vanta',
      accent: 'lavender',
      content: ['Archive de conversation.'],
      timestamp: '15:21'
    }
  ],
  'dm-cartoone': [
    {
      id: 'dm-cartoone-1',
      author: 'cartoone',
      accent: 'pink',
      content: ['Notes personnelles.'],
      timestamp: '12:04'
    }
  ]
};

const emojiOptions = ['😀', '😅', '🤣', '😂', '🙂', '🙃', '🤔', '😎', '🥳', '😍', '😘', '😉'];

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
  const conversationScrollPositions = useRef<Record<string, ChannelScrollPosition>>({});
  const pendingScrollBottom = useRef(false);
  const isRestoringScroll = useRef(false);
  const [chatMode, setChatMode] = useState<ChatMode>('guild');
  const [activeChannel, setActiveChannel] = useState<string | null>(null);
  const [activeDm, setActiveDm] = useState<string | null>(null);
  const [draft, setDraft] = useState('');
  const [isEmojiOpen, setIsEmojiOpen] = useState(false);
  const [isNearBottom, setIsNearBottom] = useState(true);
  const [editingMessageId, setEditingMessageId] = useState<string | null>(null);
  const [editingDraft, setEditingDraft] = useState('');
  const [mobilePane, setMobilePane] = useState<'channels' | 'messages'>('messages');
  const [username, setUsername] = useState('cartoone');
  const [messagesByConversation, setMessagesByConversation] = useState(initialMessages);

  useEffect(() => {
    const storedUsername = window.localStorage.getItem(SESSION_USERNAME_KEY);
    if (storedUsername) {
      setUsername(storedUsername);
    }

    const storedMode = window.sessionStorage.getItem(LAST_CHAT_MODE_KEY);
    const storedChannel = window.sessionStorage.getItem(LAST_CHAT_CHANNEL_KEY);
    const storedDm = window.sessionStorage.getItem(LAST_CHAT_DM_KEY);

    setChatMode(storedMode === 'dm' ? 'dm' : 'guild');
    setActiveChannel(storedChannel && hasChannel(storedChannel) ? storedChannel : 'general');
    setActiveDm(storedDm && hasDm(storedDm) ? storedDm : 'dm-skydogzz');
  }, []);

  const activeConversationId = chatMode === 'dm' ? activeDm : activeChannel;
  const activeConversationName =
    chatMode === 'dm'
      ? activeDm
        ? getDmName(activeDm)
        : ''
      : activeChannel
        ? getChannelName(activeChannel)
        : '';
  const activeMessages = activeConversationId
    ? (messagesByConversation[activeConversationId] ?? [])
    : [];
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

  const updateNearBottomState = useCallback(() => {
    const viewport = messagesViewportRef.current;
    if (!viewport) {
      setIsNearBottom(true);
      return;
    }

    const distanceFromBottom = viewport.scrollHeight - viewport.scrollTop - viewport.clientHeight;
    setIsNearBottom(distanceFromBottom <= BOTTOM_THRESHOLD_PX);
  }, []);

  const rememberConversationScrollPosition = useCallback(
    (conversationId: string | null) => {
      if (!conversationId || isRestoringScroll.current) {
        return;
      }

      const viewport = messagesViewportRef.current;
      if (!viewport) {
        return;
      }

      const viewportTop = viewport.getBoundingClientRect().top;
      const visibleMessage = (messagesByConversation[conversationId] ?? [])
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

      conversationScrollPositions.current[conversationId] = {
        messageId: visibleMessage.id,
        topOffset: visibleMessage.top - viewportTop
      };
    },
    [messagesByConversation]
  );

  useLayoutEffect(() => {
    if (!activeConversationId) {
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
      const savedPosition = conversationScrollPositions.current[activeConversationId];
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
      rememberConversationScrollPosition(activeConversationId);
      updateNearBottomState();
    });
  }, [
    activeConversationId,
    activeMessages.length,
    rememberConversationScrollPosition,
    updateNearBottomState
  ]);

  function handleMessagesScroll() {
    rememberConversationScrollPosition(activeConversationId);
    updateNearBottomState();
  }

  function handleJumpToBottom() {
    const viewport = messagesViewportRef.current;
    if (!viewport) {
      return;
    }

    viewport.scrollTo({ top: viewport.scrollHeight, behavior: 'smooth' });
  }

  function handleOpenDms() {
    rememberConversationScrollPosition(activeConversationId);
    setChatMode('dm');
    window.sessionStorage.setItem(LAST_CHAT_MODE_KEY, 'dm');
  }

  function handleOpenGuild() {
    rememberConversationScrollPosition(activeConversationId);
    setChatMode('guild');
    window.sessionStorage.setItem(LAST_CHAT_MODE_KEY, 'guild');
  }

  function handleSelectChannel(channelId: string) {
    rememberConversationScrollPosition(activeConversationId);
    setChatMode('guild');
    setActiveChannel(channelId);
    window.sessionStorage.setItem(LAST_CHAT_MODE_KEY, 'guild');
    window.sessionStorage.setItem(LAST_CHAT_CHANNEL_KEY, channelId);
    setMobilePane('messages');
  }

  function handleSelectDm(dmId: string) {
    rememberConversationScrollPosition(activeConversationId);
    setChatMode('dm');
    setActiveDm(dmId);
    window.sessionStorage.setItem(LAST_CHAT_MODE_KEY, 'dm');
    window.sessionStorage.setItem(LAST_CHAT_DM_KEY, dmId);
    setMobilePane('messages');
  }

  function handleSubmitMessage() {
    const content = draft.trim();
    if (!content || !activeConversationId) {
      return;
    }

    const nextMessage: ChatMessageData = {
      id: `${activeConversationId}-${Date.now()}`,
      author: username,
      accent: 'pink',
      content: content.split(/\r?\n/),
      timestamp: new Date().toLocaleTimeString('fr-FR', {
        hour: '2-digit',
        minute: '2-digit'
      })
    };

    setMessagesByConversation((current) => ({
      ...current,
      [activeConversationId]: [...(current[activeConversationId] ?? []), nextMessage]
    }));
    pendingScrollBottom.current = true;
    setDraft('');
    setIsEmojiOpen(false);
  }

  function appendEmoji(emoji: string) {
    setDraft((current) => `${current}${emoji}`);
  }

  function handleToggleReaction(messageId: string) {
    if (!activeConversationId) {
      return;
    }

    setMessagesByConversation((current) => ({
      ...current,
      [activeConversationId]: (current[activeConversationId] ?? []).map((message) => {
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
    if (!activeConversationId) {
      return;
    }

    const content = editingDraft.trim();
    if (!content) {
      return;
    }

    setMessagesByConversation((current) => ({
      ...current,
      [activeConversationId]: (current[activeConversationId] ?? []).map((message) =>
        message.id === messageId ? { ...message, content: content.split(/\r?\n/) } : message
      )
    }));
    handleCancelEdit();
  }

  function handleDeleteMessage(messageId: string) {
    if (!activeConversationId) {
      return;
    }

    setMessagesByConversation((current) => ({
      ...current,
      [activeConversationId]: (current[activeConversationId] ?? []).filter(
        (message) => message.id !== messageId
      )
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
      <GuildSidebar activeMode={chatMode} onOpenDms={handleOpenDms} onOpenGuild={handleOpenGuild} />

      {chatMode === 'dm' ? (
        <DmList
          activeDm={activeDm ?? ''}
          mobilePane={mobilePane}
          username={username}
          onSelectDm={handleSelectDm}
        />
      ) : (
        <ChannelList
          activeChannel={activeChannel ?? ''}
          mobilePane={mobilePane}
          username={username}
          onSelectChannel={handleSelectChannel}
        />
      )}

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
              aria-label={chatMode === 'dm' ? 'Show DMs' : 'Show channels'}
            >
              <ArrowLeft className="h-5 w-5" strokeWidth={1.9} />
            </button>
            <h2 className="mono-detail text-[1.85rem] font-bold tracking-[-0.05em] text-white">
              {chatMode === 'dm' ? '@' : '#'} {activeConversationName}
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
              placeholder={`Message ${chatMode === 'dm' ? '@' : '#'}${activeConversationName}`}
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
            <span>
              {chatMode === 'dm' ? 'Conversation directe locale' : 'Canal interactif local'}
            </span>
            <button
              type="button"
              onClick={() => setMobilePane('channels')}
              className="inline-flex items-center gap-2 md:hidden"
            >
              <MessageCircle className="h-4 w-4" strokeWidth={1.8} />
              {chatMode === 'dm' ? 'DMs' : 'Channels'}
            </button>
          </div>
        </div>
      </section>
    </div>
  );
}
