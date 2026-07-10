'use client';

import { useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react';
import { useRouter } from 'next/navigation';
import {
  ArrowLeft,
  ArrowRight,
  CircleEllipsis,
  MessageCircle,
  Phone,
  Smile,
  UserRound,
  Video
} from 'lucide-react';
import { ChatMessage, getAccentClasses, type ChatMessageData } from './chat-message';
import { ChannelList, getChannelName, hasChannel } from './channel-list';
import {
  DmList,
  getDmDetails,
  getDmName,
  getDmStatusClasses,
  type DirectMessage
} from './dm-list';
import {
  getGuildMemberByName,
  GuildMemberList,
  type GuildMember
} from './guild-member-list';
import { GuildSidebar } from './guild-sidebar';
import { NotificationCard } from './notification-card';
import { ProfileCard } from './profile-card';
import { SettingsModal } from './settings-modal';
import type { Friend } from './friends-list';
import { initialMessages } from './mocks/message-mocks';
import { useNotifications } from '../shared/lib/use-notifications';
import { clearSession, getUserId } from '../shared/lib/session';
import { logout } from '../shared/api/auth';
import { listDirectMessages } from '../shared/api/chat';
import { getUser, listFriends } from '../shared/api/user';
import { toDirectMessage, toFriend } from '../shared/api/hydrate';
import { useCall } from '../shared/call/call-context';
import { IncomingCallOverlay } from './call/incoming-call-overlay';
import { CallWindow } from './call/call-window';

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
  const router = useRouter();
  const { startCall } = useCall();
  const messagesViewportRef = useRef<HTMLDivElement>(null);
  const composerRef = useRef<HTMLTextAreaElement>(null);
  const messageRefs = useRef<Record<string, HTMLElement | null>>({});
  const conversationScrollPositions = useRef<Record<string, ChannelScrollPosition>>({});
  const pendingScrollBottom = useRef(false);
  const isRestoringScroll = useRef(false);
  const [chatMode, setChatMode] = useState<ChatMode>('guild');
  const [activeChannel, setActiveChannel] = useState<string | null>(null);
  const [activeDm, setActiveDm] = useState<string | null>(null);
  const [draftsByConversation, setDraftsByConversation] = useState<Record<string, string>>({});
  const [isEmojiOpen, setIsEmojiOpen] = useState(false);
  const [isNearBottom, setIsNearBottom] = useState(true);
  const [editingMessageId, setEditingMessageId] = useState<string | null>(null);
  const [editingDraft, setEditingDraft] = useState('');
  const [mobilePane, setMobilePane] = useState<'channels' | 'messages'>('messages');
  const [username] = useState('cartoone');
  const [isHydrated, setIsHydrated] = useState(false);
  const [dmConversations, setDmConversations] = useState<DirectMessage[]>([]);
  const [friends, setFriends] = useState<Friend[]>([]);
  const [messagesByConversation, setMessagesByConversation] = useState(initialMessages);
  const [profileMember, setProfileMember] = useState<GuildMember | null>(null);
  const [isNotificationCardOpen, setIsNotificationCardOpen] = useState(false);
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [isMicMuted, setIsMicMuted] = useState(false);
  const [isDeafened, setIsDeafened] = useState(false);
  const [isMemberListOpen, setIsMemberListOpen] = useState(true);
  const [isDmProfileOpen, setIsDmProfileOpen] = useState(false);
  const notificationFeed = useNotifications();

  useEffect(() => {
    // TODO(api:user): hydrate the real profile from GET /users/me (epic 2).
    const storedMode = window.sessionStorage.getItem(LAST_CHAT_MODE_KEY);
    const storedChannel = window.sessionStorage.getItem(LAST_CHAT_CHANNEL_KEY);
    const storedDm = window.sessionStorage.getItem(LAST_CHAT_DM_KEY);

    setChatMode(storedMode === 'dm' ? 'dm' : 'guild');
    setActiveChannel(storedChannel && hasChannel(storedChannel) ? storedChannel : 'general');
    // the DM list loads asynchronously; keep the stored selection and let the
    // empty-state render until it resolves (getDmDetails returns null meanwhile).
    setActiveDm(storedDm && storedDm.length > 0 ? storedDm : null);
    setIsHydrated(true);
  }, []);

  useEffect(() => {
    // Hydrate the DM sidebar and friends list from the real services. DM rows are
    // conversation summaries (GET /chat/dms) enriched with the partner's profile
    // (GET /users/{id}); friends come straight from GET /users/{id}/friends. All
    // calls are best-effort: failures leave the lists empty rather than surfacing
    // as console errors (III: zero console errors in Chrome).
    let cancelled = false;

    async function loadWorkspaceData() {
      const userId = getUserId();

      const [conversations, friendList] = await Promise.all([
        listDirectMessages().catch(() => []),
        userId ? listFriends(userId).catch(() => []) : Promise.resolve([])
      ]);

      const hydratedDms = await Promise.all(
        conversations.map(async (conversation) => {
          try {
            const partner = await getUser(conversation.partner_id);
            return toDirectMessage(conversation, partner);
          } catch {
            return null;
          }
        })
      );

      if (cancelled) {
        return;
      }

      setDmConversations(hydratedDms.filter((dm): dm is DirectMessage => dm !== null));
      setFriends(friendList.map(toFriend));
    }

    void loadWorkspaceData();

    return () => {
      cancelled = true;
    };
  }, []);

  const activeDmDetails =
    chatMode === 'dm' && activeDm ? getDmDetails(activeDm, dmConversations) : null;
  const resolvePeerName = useCallback(
    (peerId: string | null) =>
      peerId ? (dmConversations.find((dm) => dm.id === peerId)?.name ?? 'Unknown user') : 'Unknown user',
    [dmConversations]
  );

  function startDmCall(callType: 'audio' | 'video') {
    if (!activeDmDetails) {
      return;
    }
    // the DM id is now the partner's real user snowflake (hydrated from GET /chat/dms),
    // so it is a valid callee for the signaling hub.
    void startCall(activeDmDetails.id, callType);
  }
  const activeConversationId = chatMode === 'dm' ? (activeDmDetails?.id ?? null) : activeChannel;
  const activeConversationName =
    chatMode === 'dm'
      ? activeDmDetails
        ? getDmName(activeDmDetails.id, dmConversations)
        : ''
      : activeChannel
        ? getChannelName(activeChannel)
        : '';
  const activeMessages = activeConversationId
    ? (messagesByConversation[activeConversationId] ?? [])
    : [];
  const activeDraft = activeConversationId
    ? (draftsByConversation[activeConversationId] ?? '')
    : '';
  const isDmEmptyState = chatMode === 'dm' && !activeDmDetails;
  const activeDmProfileMember: GuildMember | null = activeDmDetails
    ? {
        id: activeDmDetails.id,
        name: activeDmDetails.name,
        role: 'Member',
        status: activeDmDetails.status,
        accent: activeDmDetails.accent,
        activity: activeDmDetails.lastMessage
      }
    : null;
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

  useEffect(() => {
    if (!activeConversationId || !isHydrated) {
      return;
    }

    composerRef.current?.focus();
  }, [activeConversationId, isHydrated]);

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
    setActiveDm(null);
    setIsDmProfileOpen(false);
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
    setIsDmProfileOpen(false);
    window.sessionStorage.setItem(LAST_CHAT_MODE_KEY, 'dm');
    window.sessionStorage.setItem(LAST_CHAT_DM_KEY, dmId);
    setMobilePane('messages');
  }

  function handleToggleMicMute() {
    setIsMicMuted((current) => !current);
  }

  function handleToggleDeafen() {
    setIsDeafened((current) => {
      const next = !current;

      if (next) {
        setIsMicMuted(true);
      }

      return next;
    });
  }

  async function handleDisconnect() {
    try {
      await logout();
    } catch {
      // best-effort revoke; clear the local session regardless
    }
    clearSession();
    router.push('/auth/login');
    router.refresh();
  }

  function handleSubmitMessage() {
    // TODO(api:chat): send guild messages through SignalR SendMessage and DMs through SendDirectMessage.
    const content = activeDraft.trim();
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

    if (chatMode === 'dm') {
      const [hours, minutes] = nextMessage.timestamp.split(':').map(Number);
      const lastActivityMinutes =
        Number.isFinite(hours) && Number.isFinite(minutes) ? hours * 60 + minutes : Date.now();

      setDmConversations((current) =>
        current.map((dm) =>
          dm.id === activeConversationId
            ? {
                ...dm,
                lastMessage: content.split(/\r?\n/)[0] ?? content,
                lastMessageAt: nextMessage.timestamp,
                lastActivityMinutes,
                unreadCount: 0
              }
            : dm
        )
      );
    }

    setDraftsByConversation((current) => {
      const next = { ...current };
      delete next[activeConversationId];
      return next;
    });
    setIsEmojiOpen(false);
  }

  function appendEmoji(emoji: string) {
    if (!activeConversationId) {
      return;
    }

    setDraftsByConversation((current) => ({
      ...current,
      [activeConversationId]: `${current[activeConversationId] ?? ''}${emoji}`
    }));
  }

  function handleDraftChange(value: string) {
    if (!activeConversationId) {
      return;
    }

    setDraftsByConversation((current) => ({
      ...current,
      [activeConversationId]: value
    }));
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

  function handleOpenAuthorProfile(message: ChatMessageData) {
    const guildMember = getGuildMemberByName(message.author);
    const directMessage = dmConversations.find(
      (dm) => dm.name.toLowerCase() === message.author.toLowerCase()
    );

    setProfileMember(
      guildMember ??
        (directMessage
          ? {
              id: directMessage.id,
              name: directMessage.name,
              role: 'Member',
              status: directMessage.status,
              accent: directMessage.accent,
              activity: directMessage.lastMessage
            }
          : {
              id: message.author.toLowerCase(),
              name: message.author,
              role: 'Member',
              status: message.author.toLowerCase() === username.toLowerCase() ? 'online' : 'offline',
              accent: message.accent,
              activity: 'No recent activity'
            })
    );
  }

  return (
    <div className="mx-auto flex h-screen w-full gap-4 px-3 py-4 md:px-5 md:py-9">
      {!isHydrated ? (
        <div className="flex min-h-0 flex-1 rounded-[1rem] bg-secondary-bg ring-1 ring-white/5" />
      ) : (
        <>
          <GuildSidebar
            activeMode={chatMode}
            onOpenDms={handleOpenDms}
            onOpenGuild={handleOpenGuild}
          />

          {chatMode === 'dm' ? (
            <DmList
              activeDm={activeDm ?? ''}
              directMessages={dmConversations}
              friends={friends}
              mobilePane={mobilePane}
              username={username}
              isMicMuted={isMicMuted}
              isDeafened={isDeafened}
              unreadNotifications={notificationFeed.unreadCount}
              onToggleDeafen={handleToggleDeafen}
              onToggleMicMute={handleToggleMicMute}
              onOpenNotifications={() => setIsNotificationCardOpen(true)}
              onOpenSettings={() => setIsSettingsOpen(true)}
              onSelectDm={handleSelectDm}
            />
          ) : (
            <ChannelList
              activeChannel={activeChannel ?? ''}
              mobilePane={mobilePane}
              username={username}
              isMicMuted={isMicMuted}
              isDeafened={isDeafened}
              unreadNotifications={notificationFeed.unreadCount}
              onToggleDeafen={handleToggleDeafen}
              onToggleMicMute={handleToggleMicMute}
              onOpenNotifications={() => setIsNotificationCardOpen(true)}
              onOpenSettings={() => setIsSettingsOpen(true)}
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
                {chatMode === 'dm' && activeDmDetails ? (
                  <div className="flex min-w-0 items-center gap-3">
                    <span className="relative shrink-0">
                      <span
                        className={`flex h-11 w-11 items-center justify-center rounded-full text-sm font-bold ${getAccentClasses(
                          activeDmDetails.accent
                        )}`}
                      >
                        {activeDmDetails.name.slice(0, 1).toUpperCase()}
                      </span>
                      <span
                        className={`absolute -bottom-0.5 -right-0.5 h-3.5 w-3.5 rounded-full border-2 border-secondary-bg ${getDmStatusClasses(
                          activeDmDetails.status
                        )}`}
                      />
                    </span>
                    <span className="min-w-0">
                      <span className="block truncate text-[1.2rem] font-bold tracking-[-0.03em] text-white">
                        {activeDmDetails.name}
                      </span>
                      <span className="font-category block text-[0.72rem] uppercase tracking-[0.14em] text-white/35">
                        {activeDmDetails.status}
                      </span>
                    </span>
                  </div>
                ) : chatMode === 'dm' ? (
                  <h2 className="text-[1.25rem] font-bold tracking-[-0.03em] text-white">
                    Direct Messages
                  </h2>
                ) : (
                  <h2 className="mono-detail text-[1.85rem] font-bold tracking-[-0.05em] text-white">
                    # {activeConversationName}
                  </h2>
                )}
              </div>
              <div className="flex items-center gap-4 text-[#8c8c90]">
                {chatMode === 'dm' && activeDmDetails ? (
                  <>
                    <button
                      type="button"
                      onClick={() => startDmCall('audio')}
                      className="transition hover:text-white"
                      aria-label="Start voice call"
                    >
                      <Phone className="h-5 w-5" strokeWidth={1.8} />
                    </button>
                    <button
                      type="button"
                      onClick={() => startDmCall('video')}
                      className="transition hover:text-white"
                      aria-label="Start video call"
                    >
                      <Video className="h-5 w-5" strokeWidth={1.8} />
                    </button>
                  </>
                ) : null}
                <button
                  type="button"
                  onClick={() => {
                    if (chatMode === 'dm') {
                      if (activeDmProfileMember) {
                        setIsDmProfileOpen((current) => !current);
                      }
                      return;
                    }

                    setIsMemberListOpen((current) => !current);
                  }}
                  className={`transition hover:text-white ${
                    (chatMode === 'guild' && isMemberListOpen) ||
                    (chatMode === 'dm' && isDmProfileOpen)
                      ? 'text-aqua'
                      : 'text-[#8c8c90]'
                  } ${chatMode === 'dm' && !activeDmProfileMember ? 'cursor-not-allowed opacity-45' : ''}`}
                  disabled={chatMode === 'dm' && !activeDmProfileMember}
                  aria-label={
                    chatMode === 'dm'
                      ? isDmProfileOpen
                        ? 'Hide profile'
                        : 'Show profile'
                      : isMemberListOpen
                        ? 'Hide member list'
                        : 'Show member list'
                  }
                  aria-pressed={
                    (chatMode === 'guild' && isMemberListOpen) ||
                    (chatMode === 'dm' && isDmProfileOpen)
                  }
                >
                  <UserRound className="h-5 w-5" strokeWidth={1.8} />
                </button>
                <CircleEllipsis className="h-5 w-5" strokeWidth={1.8} />
              </div>
            </div>

            <div
              ref={messagesViewportRef}
              onScroll={handleMessagesScroll}
              className="min-h-0 flex-1 overflow-auto px-5 py-7 sm:px-7"
            >
              {isDmEmptyState ? (
                <div className="flex min-h-full flex-col items-center justify-center px-6 text-center">
                  <div className="flex h-16 w-16 items-center justify-center rounded-full bg-panel text-[#8b8b8f]">
                    <MessageCircle className="h-7 w-7" strokeWidth={1.8} />
                  </div>
                  <h3 className="mt-5 text-[1.25rem] font-bold tracking-[-0.03em] text-white">
                    No DM selected
                  </h3>
                  <p className="mt-2 max-w-[22rem] text-sm leading-6 text-white/40">
                    Select a conversation from the DM list to start reading or sending messages.
                  </p>
                </div>
              ) : (
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
                        onOpenAuthorProfile={handleOpenAuthorProfile}
                        setMessageRef={setMessageRef}
                      />
                    );
                  })}
                </div>
              )}
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
                  ref={composerRef}
                  value={activeDraft}
                  onChange={(event) => handleDraftChange(event.target.value)}
                  onKeyDown={(event) => {
                    if (event.key === 'Enter' && !event.shiftKey) {
                      event.preventDefault();
                      handleSubmitMessage();
                    }
                  }}
                  disabled={!activeConversationId}
                  placeholder={`Message ${chatMode === 'dm' ? '@' : '#'}${activeConversationName}`}
                  rows={1}
                  className="h-full min-h-0 w-full resize-none overflow-y-auto bg-transparent py-4 text-lg leading-6 text-white outline-none placeholder:text-muted disabled:cursor-not-allowed disabled:text-white/30"
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
                    disabled={!activeConversationId}
                    className="text-aqua transition hover:text-white disabled:cursor-not-allowed disabled:text-[#535353]"
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

          {chatMode === 'guild' && isMemberListOpen ? (
            <GuildMemberList
              onToggleVisibility={() => setIsMemberListOpen((current) => !current)}
              onOpenProfile={setProfileMember}
            />
          ) : null}

          {chatMode === 'dm' && isDmProfileOpen && activeDmProfileMember ? (
            <ProfileCard
              member={activeDmProfileMember}
              variant="side"
              onClose={() => setIsDmProfileOpen(false)}
            />
          ) : null}

          {profileMember ? (
            <ProfileCard member={profileMember} onClose={() => setProfileMember(null)} />
          ) : null}

          {isNotificationCardOpen ? (
            <NotificationCard
              feed={notificationFeed}
              onClose={() => setIsNotificationCardOpen(false)}
            />
          ) : null}

          {isSettingsOpen ? (
            <SettingsModal
              username={username}
              onClose={() => setIsSettingsOpen(false)}
              onDisconnect={handleDisconnect}
            />
          ) : null}

          <IncomingCallOverlay resolvePeerName={resolvePeerName} />
          <CallWindow resolvePeerName={resolvePeerName} />
        </>
      )}
    </div>
  );
}
