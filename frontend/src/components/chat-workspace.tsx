'use client';

import { useEffect, useMemo, useRef, useState } from 'react';
import { useRouter } from 'next/navigation';
import {
  ArrowLeft,
  ArrowRight,
  CircleEllipsis,
  MessageCircle,
  Paperclip,
  Smile,
  UserRound,
  X
} from 'lucide-react';
import {
  ChatMessage,
  getAccentClasses,
  type ChatMessageData,
  type ReplyPreview
} from './chat-message';
import { ChannelList, getChannelName } from './channel-list';
import { DmList, getDmDetails, getDmName, getDmStatusClasses } from './dm-list';
import {
  getGuildMemberByName,
  GuildMemberList,
  type GuildMember
} from './guild-member-list';
import { GuildSidebar } from './guild-sidebar';
import { NotificationCard } from './notification-card';
import { ProfileCard } from './profile-card';
import { SettingsModal } from './settings-modal';
import { clearSession } from '../shared/lib/session';
import { logout } from '../shared/api/auth';
import { markChannelRead, markDirectMessageRead } from '../shared/api/chat';
import { useCurrentUserId } from '../shared/hooks/use-current-user-id';
import { useGuildWorkspace } from '../shared/hooks/use-guild-workspace';
import { useDmWorkspace } from '../shared/hooks/use-dm-workspace';
import { useConversationHistory } from '../shared/hooks/use-conversation-history';
import { useScrollPreservation } from '../shared/hooks/use-scroll-preservation';

const LAST_CHAT_MODE_KEY = 'ft_transcendence_last_chat_mode';
const MESSAGE_GROUP_THRESHOLD_MINUTES = 5;
const TOP_THRESHOLD_PX = 96;

type ChatMode = 'guild' | 'dm';

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
  const [chatMode, setChatMode] = useState<ChatMode>('guild');
  const [isHydrated, setIsHydrated] = useState(false);
  const [draftsByConversation, setDraftsByConversation] = useState<Record<string, string>>({});
  const [isEmojiOpen, setIsEmojiOpen] = useState(false);
  const [editingMessageId, setEditingMessageId] = useState<string | null>(null);
  const [editingDraft, setEditingDraft] = useState('');
  const [mobilePane, setMobilePane] = useState<'channels' | 'messages'>('messages');
  const [username] = useState('cartoone');
  const [profileMember, setProfileMember] = useState<GuildMember | null>(null);
  const [isNotificationCardOpen, setIsNotificationCardOpen] = useState(false);
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [isMicMuted, setIsMicMuted] = useState(false);
  const [isDeafened, setIsDeafened] = useState(false);
  const [isMemberListOpen, setIsMemberListOpen] = useState(true);
  const [isDmProfileOpen, setIsDmProfileOpen] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const currentUserId = useCurrentUserId();
  const guildWorkspace = useGuildWorkspace();
  const dmWorkspace = useDmWorkspace();

  useEffect(() => {
    // TODO(api:user): hydrate the real profile from GET /users/me (epic 2).
    const storedMode = window.sessionStorage.getItem(LAST_CHAT_MODE_KEY);
    setChatMode(storedMode === 'dm' ? 'dm' : 'guild');
    setIsHydrated(true);
  }, []);

  const activeDmDetails =
    chatMode === 'dm' && dmWorkspace.activeDm
      ? getDmDetails(dmWorkspace.activeDm, dmWorkspace.dmConversations)
      : null;
  const activeConversationId =
    chatMode === 'dm' ? (activeDmDetails?.id ?? null) : guildWorkspace.activeChannel;
  const activeConversationName =
    chatMode === 'dm'
      ? getDmName(activeDmDetails?.id ?? '', dmWorkspace.dmConversations)
      : getChannelName(guildWorkspace.activeChannel ?? '', guildWorkspace.channels);

  const conversationHistory = useConversationHistory(chatMode, activeConversationId, currentUserId);

  const activeMessages = useMemo(
    () => (activeConversationId ? (conversationHistory.messagesByConversation[activeConversationId] ?? []) : []),
    [activeConversationId, conversationHistory.messagesByConversation]
  );

  const scroll = useScrollPreservation(
    activeConversationId,
    activeMessages,
    conversationHistory.messagesByConversation,
    isHydrated
  );

  const channelUnreadCounts = useMemo(() => {
    const counts: Record<string, number> = {};
    for (const [channelId, state] of Object.entries(guildWorkspace.channelReadStates)) {
      counts[channelId] = state.unreadCount;
    }
    return counts;
  }, [guildWorkspace.channelReadStates]);

  // mark the active conversation as read once its latest message is actually
  // in view, rather than as soon as it's selected
  useEffect(() => {
    if (!scroll.isNearBottom || !activeConversationId) {
      return;
    }

    const latestMessage = activeMessages[activeMessages.length - 1];
    if (!latestMessage?.id) {
      return;
    }

    if (chatMode === 'guild') {
      const channelId = activeConversationId;
      if (guildWorkspace.channelReadStates[channelId]?.lastReadMessageId === latestMessage.id) {
        return;
      }

      markChannelRead(channelId, latestMessage.id)
        .then(() => guildWorkspace.markChannelReadLocally(channelId, latestMessage.id))
        .catch(() => {
          // best effort: retry next time the viewport is at the bottom with a new message
        });
    } else {
      const partnerId = activeConversationId;
      const currentUnreadCount =
        dmWorkspace.dmConversations.find((dm) => dm.id === partnerId)?.unreadCount ?? 0;
      if (currentUnreadCount === 0) {
        return;
      }

      markDirectMessageRead(partnerId, latestMessage.id)
        .then(() => {
          dmWorkspace.setDmConversations((current) =>
            current.map((dm) => (dm.id === partnerId ? { ...dm, unreadCount: 0 } : dm))
          );
        })
        .catch(() => {
          // best effort: retry next time the viewport is at the bottom
        });
    }
    // guildWorkspace/dmWorkspace are fresh objects every render (not memoized) -
    // only their data fields belong in the trigger condition, not the whole objects
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [
    chatMode,
    activeConversationId,
    scroll.isNearBottom,
    activeMessages,
    guildWorkspace.channelReadStates,
    dmWorkspace.dmConversations
  ]);

  const activeDraft = (activeConversationId && draftsByConversation[activeConversationId]) ?? '';
  const isActiveDmArchived = chatMode === 'dm' && (activeDmDetails?.isArchived ?? false);
  const isComposerDisabled = !activeConversationId || isActiveDmArchived;
  // an uploading/errored attachment has no confirmed id yet - block send entirely
  // until it's ready or removed, rather than silently sending without it.
  const hasUnresolvedAttachment = conversationHistory.pendingAttachments.some(
    (attachment) => attachment.status !== 'ready'
  );
  const isSendDisabled = isComposerDisabled || hasUnresolvedAttachment;
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
  const activeMessageItems = useMemo(() => {
    const messagesById = new Map(activeMessages.map((message) => [message.id, message]));

    return activeMessages.map((message, index) => {
      const previousMessage = activeMessages[index - 1];
      const isGrouped =
        previousMessage?.author === message.author &&
        getMinutesBetween(previousMessage.timestamp, message.timestamp) <=
          MESSAGE_GROUP_THRESHOLD_MINUTES;

      let replyPreview: ReplyPreview | null = null;
      if (message.replyToId) {
        const target = messagesById.get(message.replyToId);
        replyPreview = target
          ? { author: target.author, snippet: target.content[0] ?? '' }
          : { author: '', snippet: 'an earlier message' };
      }

      return { message, isGrouped, replyPreview };
    });
  }, [activeMessages]);

  function handleMessagesScroll() {
    scroll.rememberConversationScrollPosition(activeConversationId);
    scroll.updateNearBottomState();

    const viewport = scroll.messagesViewportRef.current;
    const activeChannel = guildWorkspace.activeChannel;
    if (chatMode === 'guild' && activeChannel && viewport && viewport.scrollTop <= TOP_THRESHOLD_PX) {
      conversationHistory.loadOlderChannelHistory(activeChannel);
    }
  }

  function handleOpenDms() {
    scroll.rememberConversationScrollPosition(activeConversationId);
    setChatMode('dm');
    dmWorkspace.clearActiveDm();
    setIsDmProfileOpen(false);
    window.sessionStorage.setItem(LAST_CHAT_MODE_KEY, 'dm');
  }

  function handleSelectGuild(guildId: string) {
    scroll.rememberConversationScrollPosition(activeConversationId);
    setChatMode('guild');
    guildWorkspace.selectGuild(guildId);
    window.sessionStorage.setItem(LAST_CHAT_MODE_KEY, 'guild');
  }

  function handleSelectChannel(channelId: string) {
    scroll.rememberConversationScrollPosition(activeConversationId);
    setChatMode('guild');
    guildWorkspace.selectChannel(channelId);
    window.sessionStorage.setItem(LAST_CHAT_MODE_KEY, 'guild');
    setMobilePane('messages');
  }

  function handleSelectDm(dmId: string) {
    scroll.rememberConversationScrollPosition(activeConversationId);
    setChatMode('dm');
    dmWorkspace.selectDm(dmId);
    setIsDmProfileOpen(false);
    window.sessionStorage.setItem(LAST_CHAT_MODE_KEY, 'dm');
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
    const content = activeDraft.trim();
    const hasReadyAttachment = conversationHistory.pendingAttachments.some(
      (attachment) => attachment.status === 'ready'
    );
    if ((!content && !hasReadyAttachment) || !activeConversationId || isSendDisabled) {
      return;
    }

    conversationHistory.sendMessage(content);
    scroll.scrollToBottomOnNextRender();

    if (chatMode === 'dm') {
      const previewTimestamp = new Date().toLocaleTimeString('fr-FR', {
        hour: '2-digit',
        minute: '2-digit'
      });
      dmWorkspace.setDmConversations((current) =>
        current.map((dm) =>
          dm.id === activeConversationId
            ? {
                ...dm,
                lastMessage: content.split(/\r?\n/)[0] ?? content,
                lastMessageAt: previewTimestamp,
                lastActivityAt: Date.now(),
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

  function handleFilesSelected(event: React.ChangeEvent<HTMLInputElement>) {
    const files = event.target.files;
    if (files && files.length > 0) {
      conversationHistory.uploadAttachments(Array.from(files));
    }
    event.target.value = '';
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

  function handleStartEdit(message: ChatMessageData) {
    setEditingMessageId(message.id);
    setEditingDraft(message.content.join('\n'));
  }

  function handleCancelEdit() {
    setEditingMessageId(null);
    setEditingDraft('');
  }

  async function handleSaveEdit(messageId: string) {
    const content = editingDraft.trim();
    if (!content) {
      return;
    }

    try {
      await conversationHistory.updateMessage(messageId, content);
      handleCancelEdit();
    } catch {
      // best effort: keep the edit UI open so the user can retry or cancel
    }
  }

  async function handleDeleteMessage(messageId: string) {
    try {
      await conversationHistory.removeMessage(messageId);

      if (editingMessageId === messageId) {
        handleCancelEdit();
      }
    } catch {
      // best effort: leave the message in place, allow the user to retry
    }
  }

  function handleOpenAuthorProfile(message: ChatMessageData) {
    const guildMember = getGuildMemberByName(message.author);
    const directMessage = dmWorkspace.dmConversations.find(
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
            guilds={guildWorkspace.guilds}
            activeGuildId={guildWorkspace.activeGuildId}
            onOpenDms={handleOpenDms}
            onSelectGuild={handleSelectGuild}
          />

          {chatMode === 'dm' ? (
            <DmList
              activeDm={dmWorkspace.activeDm ?? ''}
              directMessages={dmWorkspace.dmConversations}
              showArchived={dmWorkspace.showArchivedDms}
              mobilePane={mobilePane}
              username={username}
              isMicMuted={isMicMuted}
              isDeafened={isDeafened}
              onToggleDeafen={handleToggleDeafen}
              onToggleMicMute={handleToggleMicMute}
              onOpenNotifications={() => setIsNotificationCardOpen(true)}
              onOpenSettings={() => setIsSettingsOpen(true)}
              onSelectDm={handleSelectDm}
              onToggleShowArchived={dmWorkspace.toggleShowArchivedDms}
              onArchiveDm={dmWorkspace.archiveDm}
            />
          ) : (
            <ChannelList
              activeChannel={guildWorkspace.activeChannel ?? ''}
              categories={guildWorkspace.channelCategories}
              unreadCounts={channelUnreadCounts}
              mobilePane={mobilePane}
              username={username}
              isMicMuted={isMicMuted}
              isDeafened={isDeafened}
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
              ref={scroll.messagesViewportRef}
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
                  {activeMessageItems.map(({ message, isGrouped, replyPreview }) => {
                    const isOwnMessage =
                      message.authorId != null && message.authorId === currentUserId;
                    const isEditing = editingMessageId === message.id;

                    return (
                      <ChatMessage
                        key={message.id}
                        message={message}
                        replyPreview={replyPreview}
                        isGrouped={isGrouped}
                        isOwnMessage={isOwnMessage}
                        isEditing={isEditing}
                        editingDraft={editingDraft}
                        canReact={chatMode === 'guild'}
                        onEditDraftChange={setEditingDraft}
                        onStartEdit={handleStartEdit}
                        onSaveEdit={handleSaveEdit}
                        onCancelEdit={handleCancelEdit}
                        onDelete={handleDeleteMessage}
                        onToggleReaction={conversationHistory.toggleReaction}
                        onOpenAuthorProfile={handleOpenAuthorProfile}
                        setMessageRef={scroll.setMessageRef}
                      />
                    );
                  })}
                </div>
              )}
              {!scroll.isNearBottom ? (
                <button
                  type="button"
                  onClick={scroll.scrollToBottom}
                  className="mono-detail sticky bottom-0 z-10 ml-auto flex h-10 items-center rounded-full border border-aqua/40 bg-panel px-4 text-sm font-bold text-aqua shadow-lg shadow-black/30 transition hover:border-aqua hover:text-white"
                >
                  Jump to bottom
                </button>
              ) : null}
            </div>

            <div className="shrink-0 border-t border-white/8 px-4 py-4 sm:px-5">
              <input
                ref={fileInputRef}
                type="file"
                multiple
                onChange={handleFilesSelected}
                className="hidden"
              />
              {conversationHistory.pendingAttachments.length > 0 ? (
                <div className="mb-3 flex flex-wrap gap-2">
                  {conversationHistory.pendingAttachments.map((attachment) => (
                    <span
                      key={attachment.id}
                      className={`flex h-8 items-center gap-2 rounded-full border px-3 text-xs ${
                        attachment.status === 'error'
                          ? 'border-pink/40 text-pink'
                          : 'border-white/10 text-white/70'
                      }`}
                    >
                      <span className="max-w-[10rem] truncate">{attachment.filename}</span>
                      {attachment.status === 'uploading' ? <span>Uploading…</span> : null}
                      {attachment.status === 'error' ? <span>Failed</span> : null}
                      <button
                        type="button"
                        onClick={() => conversationHistory.removePendingAttachment(attachment.id)}
                        aria-label={`Remove ${attachment.filename}`}
                        className="text-white/40 hover:text-white"
                      >
                        <X className="h-3 w-3" strokeWidth={2} />
                      </button>
                    </span>
                  ))}
                </div>
              ) : null}
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
                  ref={scroll.composerRef}
                  value={activeDraft}
                  onChange={(event) => handleDraftChange(event.target.value)}
                  onKeyDown={(event) => {
                    if (event.key === 'Enter' && !event.shiftKey) {
                      event.preventDefault();
                      handleSubmitMessage();
                    }
                  }}
                  disabled={isComposerDisabled}
                  placeholder={
                    isActiveDmArchived
                      ? 'This conversation is archived.'
                      : `Message ${chatMode === 'dm' ? '@' : '#'}${activeConversationName}`
                  }
                  rows={1}
                  className="h-full min-h-0 w-full resize-none overflow-y-auto bg-transparent py-4 text-lg leading-6 text-white outline-none placeholder:text-muted disabled:cursor-not-allowed disabled:text-white/30"
                />
                <div className="ml-auto flex items-center gap-4">
                  <button
                    type="button"
                    onClick={() => fileInputRef.current?.click()}
                    disabled={isComposerDisabled}
                    className="text-[#7e7e82] transition hover:text-white disabled:cursor-not-allowed disabled:text-[#535353]"
                    aria-label="Attach files"
                  >
                    <Paperclip className="h-5 w-5" strokeWidth={1.8} />
                  </button>
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
                    disabled={isSendDisabled}
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
            <NotificationCard onClose={() => setIsNotificationCardOpen(false)} />
          ) : null}

          {isSettingsOpen ? (
            <SettingsModal
              username={username}
              onClose={() => setIsSettingsOpen(false)}
              onDisconnect={handleDisconnect}
            />
          ) : null}
        </>
      )}
    </div>
  );
}
