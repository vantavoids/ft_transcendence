'use client';

import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useRouter } from 'next/navigation';
import { type ChatMessageData } from './chat-message';
import { ConversationHeader } from './chat/conversation-header';
import { MessageComposer } from './chat/message-composer';
import { MessageList } from './chat/message-list';
import { ChannelList, getChannelName } from './channel-list';
import { DmList, getDmDetails, getDmName } from './dm-list';
import {
  getGuildMemberByName,
  GuildMemberList,
  toProfileMember,
  type GuildMember
} from './guild-member-list';
import { GuildSidebar } from './guild-sidebar';
import { GuildSettingsModal } from './guild/guild-settings-modal';
import { NotificationCard } from './notification-card';
import { ProfileCard } from './profile-card';
import { SettingsModal } from './settings-modal';
import type { Friend } from './friends-list';
import { useNotifications } from '../shared/lib/use-notifications';
import { clearSession, getUserId } from '../shared/lib/session';
import { logout } from '../shared/api/auth';
import { markChannelRead, markDirectMessageRead } from '../shared/api/chat';
import { stopChatHub } from '../shared/api/chat-hub';
import { useCurrentUserId } from '../shared/hooks/use-current-user-id';
import { useGuildWorkspace } from '../shared/hooks/use-guild-workspace';
import { useDmWorkspace } from '../shared/hooks/use-dm-workspace';
import { useConversationHistory } from '../shared/hooks/use-conversation-history';
import { useScrollPreservation } from '../shared/hooks/use-scroll-preservation';
import { getUsersByIds, listFriends, type UserSummaryDto } from '../shared/api/user';
import { toFriend } from '../shared/api/hydrate';
import { useCall } from '../shared/call/call-context';
import { IncomingCallOverlay } from './call/incoming-call-overlay';
import { CallWindow } from './call/call-window';
import { useCurrentUserProfile } from '../shared/user/user-store';
import { useGuilds } from '../shared/guilds/guild-store';
import { useGuildMembers } from '../shared/guilds/use-guild-members';
import { accentForUserId, toSidebarStatus } from '../shared/mappers/user';

const LAST_CHAT_MODE_KEY = 'ft_transcendence_last_chat_mode';
const TOP_THRESHOLD_PX = 96;

type ChatMode = 'guild' | 'dm';

export function ChatWorkspace() {
  const router = useRouter();
  const { startCall } = useCall();
  const { currentUser, refreshCurrentUser, setCurrentUser } = useCurrentUserProfile();
  const { selectedGuild } = useGuilds();
  const [chatMode, setChatMode] = useState<ChatMode>('guild');
  const [isHydrated, setIsHydrated] = useState(false);
  const [draftsByConversation, setDraftsByConversation] = useState<Record<string, string>>({});
  const [isEmojiOpen, setIsEmojiOpen] = useState(false);
  const [editingMessageId, setEditingMessageId] = useState<string | null>(null);
  const [editingDraft, setEditingDraft] = useState('');
  const [mobilePane, setMobilePane] = useState<'channels' | 'messages'>('messages');
  const [friends, setFriends] = useState<Friend[]>([]);
  const [profileMember, setProfileMember] = useState<GuildMember | null>(null);
  const [isNotificationCardOpen, setIsNotificationCardOpen] = useState(false);
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [isGuildSettingsOpen, setIsGuildSettingsOpen] = useState(false);
  const [isMicMuted, setIsMicMuted] = useState(false);
  const [isDeafened, setIsDeafened] = useState(false);
  const [isMemberListOpen, setIsMemberListOpen] = useState(true);
  const [isDmProfileOpen, setIsDmProfileOpen] = useState(false);
  const notificationFeed = useNotifications();
  const [replyTarget, setReplyTarget] = useState<ChatMessageData | null>(null);
  const [highlightedMessageId, setHighlightedMessageId] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const highlightTimeoutRef = useRef<number | null>(null);

  const currentUserId = useCurrentUserId();
  const guildWorkspace = useGuildWorkspace();
  const dmWorkspace = useDmWorkspace(currentUserId);
  const { members: currentGuildMembers } = useGuildMembers(
    selectedGuild?.id ?? null,
    selectedGuild?.owner_id ?? null
  );
  const currentGuildMember = useMemo(
    () => currentGuildMembers.find((member) => member.userId === currentUserId) ?? null,
    [currentGuildMembers, currentUserId]
  );

  function toProfileMemberFromUser(user: UserSummaryDto) {
    return {
      id: user.id,
      name: user.display_name || user.username,
      role: 'Member' as const,
      status: toSidebarStatus(user.status),
      accent: accentForUserId(user.id),
      activity: 'No recent activity',
      bio: user.bio,
      avatarUrl: user.avatar_url ?? null,
      bannerUrl: user.banner_url ?? null
    };
  }

  useEffect(() => {
    return () => {
      if (highlightTimeoutRef.current !== null) {
        window.clearTimeout(highlightTimeoutRef.current);
      }
    };
  }, []);

  useEffect(() => {
    const storedMode = window.sessionStorage.getItem(LAST_CHAT_MODE_KEY);
    setChatMode(storedMode === 'dm' ? 'dm' : 'guild');
    setIsHydrated(true);
    void refreshCurrentUser();
  }, [refreshCurrentUser]);

  useEffect(() => {
    // Friends come straight from GET /users/{id}/friends; the DM list itself
    // is owned by useDmWorkspace. Best-effort: leave the list empty rather
    // than surface a console error (III: zero console errors in Chrome).
    let cancelled = false;

    async function loadFriends() {
      const userId = getUserId();
      if (!userId) {
        return;
      }

      const friendList = await listFriends(userId).catch(() => []);
      if (cancelled) {
        return;
      }

      setFriends(friendList.map(toFriend));
    }

    void loadFriends();

    return () => {
      cancelled = true;
    };
  }, []);

  const activeDmDetails =
    chatMode === 'dm' && dmWorkspace.activeDm
      ? getDmDetails(dmWorkspace.activeDm, dmWorkspace.dmConversations)
      : null;
  const activeConversationId =
    chatMode === 'dm' ? (activeDmDetails?.id ?? null) : guildWorkspace.activeChannel;
  const resolvePeerName = useCallback(
    (peerId: string | null) =>
      peerId
        ? (dmWorkspace.dmConversations.find((dm) => dm.id === peerId)?.name ?? 'Unknown user')
        : 'Unknown user',
    [dmWorkspace.dmConversations]
  );

  function startDmCall(callType: 'audio' | 'video') {
    if (!activeDmDetails) {
      return;
    }
    // the DM id is now the partner's real user snowflake (hydrated from GET /chat/dms),
    // so it is a valid callee for the signaling hub.
    void startCall(activeDmDetails.id, callType);
  }
  const activeConversationName =
    chatMode === 'dm'
      ? getDmName(activeDmDetails?.id ?? '', dmWorkspace.dmConversations)
      : getChannelName(guildWorkspace.activeChannel ?? '', guildWorkspace.channels);

  const conversationHistory = useConversationHistory(chatMode, activeConversationId, currentUserId);
  const { userProfilesById } = conversationHistory;

  // don't carry a reply target across conversations
  useEffect(() => {
    setReplyTarget(null);
  }, [activeConversationId]);

  const activeMessages = useMemo(
    () =>
      activeConversationId
        ? (conversationHistory.messagesByConversation[activeConversationId] ?? [])
        : [],
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
    if (!latestMessage?.id || latestMessage.pending) {
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
  const isComposerDisabled = !activeConversationId;
  const isActiveDmArchived = chatMode === 'dm' && (activeDmDetails?.isArchived ?? false);
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
        activity: activeDmDetails.lastMessage,
        bio: activeDmDetails.bio ?? null,
        avatarUrl: activeDmDetails.avatarUrl ?? null,
        bannerUrl: activeDmDetails.bannerUrl ?? null
      }
    : null;
  const isSidePanelOpen =
    (chatMode === 'guild' && isMemberListOpen) || (chatMode === 'dm' && isDmProfileOpen);
  const isSidePanelToggleDisabled = chatMode === 'dm' && !activeDmProfileMember;
  const sidePanelAriaLabel =
    chatMode === 'dm'
      ? isDmProfileOpen
        ? 'Hide profile'
        : 'Show profile'
      : isMemberListOpen
        ? 'Hide member list'
        : 'Show member list';

  function handleMessagesScroll() {
    scroll.rememberConversationScrollPosition(activeConversationId);
    scroll.updateNearBottomState();

    const viewport = scroll.messagesViewportRef.current;
    const activeChannel = guildWorkspace.activeChannel;
    if (
      chatMode === 'guild' &&
      activeChannel &&
      viewport &&
      viewport.scrollTop <= TOP_THRESHOLD_PX
    ) {
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

  function handleOpenGuild() {
    scroll.rememberConversationScrollPosition(activeConversationId);
    setChatMode('guild');
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

  const handleShowMobileSidebar = () => setMobilePane('channels');

  const handleToggleSidePanel = () => {
    if (chatMode === 'dm') {
      if (activeDmProfileMember) {
        setIsDmProfileOpen((current) => !current);
      }
      return;
    }

    setIsMemberListOpen((current) => !current);
  };
  const handleToggleEmojiPicker = () => setIsEmojiOpen((current) => !current);
  const handleOpenNotifications = () => setIsNotificationCardOpen(true);
  const handleCloseNotifications = () => setIsNotificationCardOpen(false);
  const handleOpenSettings = () => setIsSettingsOpen(true);
  const handleCloseSettings = () => setIsSettingsOpen(false);
  const handleOpenGuildSettings = () => setIsGuildSettingsOpen(true);
  const handleCloseGuildSettings = () => setIsGuildSettingsOpen(false);
  const handleCloseDmProfile = () => setIsDmProfileOpen(false);
  const handleCloseAuthorProfile = () => setProfileMember(null);

  async function handleDisconnect() {
    try {
      await logout();
    } catch {
      // best-effort revoke; clear the local session regardless
    }
    stopChatHub();
    clearSession();
    setCurrentUser(null);
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

    conversationHistory.sendMessage(content, replyTarget?.id ?? null);
    scroll.scrollToBottomOnNextRender();
    setReplyTarget(null);

    if (chatMode === 'dm') {
      const previewTimestamp = new Date().toLocaleTimeString('en-US', {
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

  function handleStartReply(message: ChatMessageData) {
    setReplyTarget(message);
    scroll.composerRef.current?.focus();
  }

  function handleCancelReply() {
    setReplyTarget(null);
  }

  function handleJumpToMessage(messageId: string) {
    const found = scroll.scrollToMessage(messageId);
    if (!found) {
      return;
    }

    if (highlightTimeoutRef.current !== null) {
      window.clearTimeout(highlightTimeoutRef.current);
    }

    setHighlightedMessageId(messageId);
    highlightTimeoutRef.current = window.setTimeout(() => {
      setHighlightedMessageId((current) => (current === messageId ? null : current));
      highlightTimeoutRef.current = null;
    }, 1600);
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

  async function handleOpenAuthorProfile(message: ChatMessageData) {
    const directMessage = dmWorkspace.dmConversations.find(
      (dm) => dm.name.toLowerCase() === message.author.toLowerCase()
    );
    const isCurrentUserMessage =
      Boolean(currentUserId && message.authorId === currentUserId) || message.author === 'You';

    if (directMessage) {
      setProfileMember({
        id: directMessage.id,
        name: directMessage.name,
        role: 'Member',
        status: directMessage.status,
        accent: directMessage.accent,
        activity: directMessage.lastMessage,
        bio: directMessage.bio ?? null,
        avatarUrl: directMessage.avatarUrl ?? null,
        bannerUrl: directMessage.bannerUrl ?? null
      });
      return;
    }

    if (isCurrentUserMessage && currentUser) {
      setProfileMember(
        currentGuildMember
          ? {
              ...toProfileMember(currentGuildMember),
              name: currentUser.displayName,
              avatarUrl: currentUser.avatarUrl,
              bannerUrl: currentUser.bannerUrl,
              bio: currentUser.bio
            }
          : {
              id: currentUser.id,
              name: currentUser.displayName,
              role: 'Member',
              status: toSidebarStatus(currentUser.status),
              accent: accentForUserId(currentUser.id),
              activity: 'No recent activity',
              bio: currentUser.bio,
              avatarUrl: currentUser.avatarUrl,
              bannerUrl: currentUser.bannerUrl
            }
      );
      return;
    }

    if (message.authorId) {
      const cachedUser = userProfilesById[message.authorId];
      if (cachedUser) {
        setProfileMember(toProfileMemberFromUser(cachedUser));
        return;
      }

      const fetchedUsers = await getUsersByIds([message.authorId]).catch(() => []);
      const fetchedUser = fetchedUsers[0];
      if (fetchedUser) {
        setProfileMember(toProfileMemberFromUser(fetchedUser));
        return;
      }
    }

    const guildMember = getGuildMemberByName(message.author);
    setProfileMember(
      guildMember ?? {
        id: message.author.toLowerCase(),
        name: message.author,
        role: 'Member',
        status: 'offline',
        accent: message.accent,
        activity: 'No recent activity',
        bio: null,
        avatarUrl: null,
        bannerUrl: null
      }
    );
  }

  // DmList and ChannelList render the same sidebar footer (account strip +
  // voice controls) regardless of chat mode, so they share this exact prop set.
  const sidebarFooterProps = {
    mobilePane,
    currentUser,
    isMicMuted,
    isDeafened,
    unreadNotifications: notificationFeed.unreadCount,
    onToggleDeafen: handleToggleDeafen,
    onToggleMicMute: handleToggleMicMute,
    onOpenNotifications: handleOpenNotifications,
    onOpenSettings: handleOpenSettings,
    onOpenGuildSettings: handleOpenGuildSettings
  };

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
              {...sidebarFooterProps}
              activeDm={dmWorkspace.activeDm ?? ''}
              directMessages={dmWorkspace.dmConversations}
              showArchived={dmWorkspace.showArchivedDms}
              friends={friends}
              onSelectDm={handleSelectDm}
              onToggleShowArchived={dmWorkspace.toggleShowArchivedDms}
              onArchiveDm={dmWorkspace.archiveDm}
            />
          ) : (
            <ChannelList
              {...sidebarFooterProps}
              activeChannel={guildWorkspace.activeChannel ?? ''}
              categories={guildWorkspace.channelCategories}
              unreadCounts={channelUnreadCounts}
              onSelectChannel={handleSelectChannel}
            />
          )}

          <section
            className={`${
              mobilePane === 'messages' ? 'flex' : 'hidden'
            } min-h-0 flex-1 flex-col overflow-hidden rounded-[1rem] bg-secondary-bg ring-1 ring-white/5 md:flex`}
          >
            <ConversationHeader
              chatMode={chatMode}
              activeDmDetails={activeDmDetails}
              activeConversationName={activeConversationName}
              isSidePanelOpen={isSidePanelOpen}
              isSidePanelToggleDisabled={isSidePanelToggleDisabled}
              sidePanelAriaLabel={sidePanelAriaLabel}
              onShowMobileSidebar={handleShowMobileSidebar}
              onToggleSidePanel={handleToggleSidePanel}
              onStartAudioCall={() => startDmCall('audio')}
              onStartVideoCall={() => startDmCall('video')}
            />

            <MessageList
              viewportRef={scroll.messagesViewportRef}
              onScroll={handleMessagesScroll}
              isDmEmptyState={isDmEmptyState}
              activeMessages={activeMessages}
              currentUserId={currentUserId}
              editingMessageId={editingMessageId}
              editingDraft={editingDraft}
              highlightedMessageId={highlightedMessageId}
              canReact={chatMode === 'guild'}
              isNearBottom={scroll.isNearBottom}
              setMessageRef={scroll.setMessageRef}
              onEditDraftChange={setEditingDraft}
              onStartEdit={handleStartEdit}
              onSaveEdit={handleSaveEdit}
              onCancelEdit={handleCancelEdit}
              onDelete={handleDeleteMessage}
              onToggleReaction={conversationHistory.toggleReaction}
              onReply={handleStartReply}
              onJumpToReply={handleJumpToMessage}
              onRetryMessage={conversationHistory.retryMessage}
              onOpenAuthorProfile={handleOpenAuthorProfile}
              onScrollToBottom={scroll.scrollToBottom}
            />

            <MessageComposer
              fileInputRef={fileInputRef}
              composerRef={scroll.composerRef}
              chatMode={chatMode}
              activeConversationName={activeConversationName}
              activeDraft={activeDraft}
              isComposerDisabled={isComposerDisabled}
              isSendDisabled={isSendDisabled}
              isActiveDmArchived={isActiveDmArchived}
              replyTarget={replyTarget}
              pendingAttachments={conversationHistory.pendingAttachments}
              isEmojiOpen={isEmojiOpen}
              onFilesSelected={handleFilesSelected}
              onRemovePendingAttachment={conversationHistory.removePendingAttachment}
              onCancelReply={handleCancelReply}
              onToggleEmojiPicker={handleToggleEmojiPicker}
              onAppendEmoji={appendEmoji}
              onDraftChange={handleDraftChange}
              onSubmitMessage={handleSubmitMessage}
              onShowMobileSidebar={handleShowMobileSidebar}
            />
          </section>

          {chatMode === 'guild' && isMemberListOpen ? (
            <GuildMemberList onOpenProfile={setProfileMember} />
          ) : null}

          {chatMode === 'dm' && isDmProfileOpen && activeDmProfileMember ? (
            <ProfileCard
              member={activeDmProfileMember}
              variant="side"
              onClose={handleCloseDmProfile}
            />
          ) : null}

          {profileMember ? (
            <ProfileCard member={profileMember} onClose={handleCloseAuthorProfile} />
          ) : null}

          {isNotificationCardOpen ? (
            <NotificationCard feed={notificationFeed} onClose={handleCloseNotifications} />
          ) : null}

          {isSettingsOpen ? (
            <SettingsModal
              currentUser={currentUser}
              onClose={handleCloseSettings}
              onDisconnect={handleDisconnect}
            />
          ) : null}

          {isGuildSettingsOpen && selectedGuild ? (
            <GuildSettingsModal guildId={selectedGuild.id} onClose={handleCloseGuildSettings} />
          ) : null}

          <IncomingCallOverlay resolvePeerName={resolvePeerName} />
          <CallWindow resolvePeerName={resolvePeerName} />
        </>
      )}
    </div>
  );
}
