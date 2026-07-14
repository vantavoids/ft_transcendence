'use client';

import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useRouter } from 'next/navigation';
import { type ChatMessageData } from './chat-message';
import { ConversationHeader } from './chat/conversation-header';
import { MessageComposer } from './chat/message-composer';
import { MessageList } from './chat/message-list';
import { ChannelList, getChannelName, type TextChannel } from './channel-list';
import { DmList, getDmDetails, getDmName } from './dm-list';
import {
  getGuildMemberByName,
  GuildMemberList,
  toProfileMember,
  topRoleByPosition,
  type GuildMember
} from './guild-member-list';
import { GuildSidebar } from './guild-sidebar';
import { GuildSettingsModal } from './guild/guild-settings-modal';
import { ChannelPermissionsModal } from './guild/channel-permissions-modal';
import { NotificationCard } from './notification-card';
import { ProfileCard } from './profile-card';
import { SettingsModal } from './settings-modal';
import type { Friend } from './friends-list';
import type { NotificationDto } from '../shared/api/notification';
import { useNotifications } from '../shared/lib/use-notifications';
import { clearSession, getUserId } from '../shared/lib/session';
import { logout } from '../shared/api/auth';
import { markChannelRead, markDirectMessageRead } from '../shared/api/chat';
import { onChatHubEvent, stopChatHub } from '../shared/api/chat-hub';
import { dispatchFriendsChanged, subscribeFriendsChanged } from '../shared/lib/friends-events';
import { useGuildWorkspace } from '../shared/hooks/use-guild-workspace';
import { useDmWorkspace } from '../shared/hooks/use-dm-workspace';
import { useConversationHistory } from '../shared/hooks/use-conversation-history';
import { useScrollPreservation } from '../shared/hooks/use-scroll-preservation';
import {
  blockUser,
  deleteFriendship,
  getFriendshipState,
  getUsersByIds,
  listFriends,
  listBlockedUsers,
  sendFriendRequest,
  unblockUser,
  type FriendshipStateDto,
  type UserSummaryDto
} from '../shared/api/user';
import { toFriend } from '../shared/api/hydrate';
import { useCall } from '../shared/call/call-context';
import { IncomingCallOverlay } from './call/incoming-call-overlay';
import { CallWindow, type CallPeer } from './call/call-window';
import { useCurrentUserProfile } from '../shared/user/user-store';
import { useGuilds } from '../shared/guilds/guild-store';
import { useGuildMembers } from '../shared/guilds/use-guild-members';
import {
  canManageMemberRoles,
  effectivePermissions,
  hasPermission,
  memberRank,
  PERMISSIONS,
  type RoleCaller
} from '../shared/guilds/role-permissions';
import { toSidebarStatus } from '../shared/mappers/user';
import { accentForId } from '../shared/lib/accent';
import { useToast } from '../shared/ui/toast';

const LAST_CHAT_MODE_KEY = 'ft_transcendence_last_chat_mode';
const TOP_THRESHOLD_PX = 96;

type ChatMode = 'guild' | 'dm';

export function ChatWorkspace() {
  const router = useRouter();
  const { startCall } = useCall();
  const { pushToast } = useToast();
  const { currentUser, refreshCurrentUser, setCurrentUser } = useCurrentUserProfile();
  const { guilds, hasLoadedGuilds, selectedGuild, selectGuild } = useGuilds();
  const [chatMode, setChatMode] = useState<ChatMode>('guild');
  const [isHydrated, setIsHydrated] = useState(false);
  const [draftsByConversation, setDraftsByConversation] = useState<Record<string, string>>({});
  const [isEmojiOpen, setIsEmojiOpen] = useState(false);
  const [editingMessageId, setEditingMessageId] = useState<string | null>(null);
  const [editingDraft, setEditingDraft] = useState('');
  const [mobilePane, setMobilePane] = useState<'channels' | 'messages'>('messages');
  const [friends, setFriends] = useState<Friend[]>([]);
  const [friendsReloadKey, setFriendsReloadKey] = useState(0);
  const [blockedUserIds, setBlockedUserIds] = useState<string[]>([]);
  const [activeDmRelationship, setActiveDmRelationship] = useState<{
    userId: string | null;
    status: FriendshipStateDto['status'] | null;
  }>({ userId: null, status: null });
  const [profileRelationshipStatus, setProfileRelationshipStatus] = useState<
    FriendshipStateDto['status'] | null
  >(null);
  const [isProfileRelationshipKnown, setIsProfileRelationshipKnown] = useState(false);
  const [profileMember, setProfileMember] = useState<GuildMember | null>(null);
  const [isNotificationCardOpen, setIsNotificationCardOpen] = useState(false);
  // the bell button in the sidebar footer; the notification popup anchors above it
  const bellRef = useRef<HTMLButtonElement>(null);
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [isGuildSettingsOpen, setIsGuildSettingsOpen] = useState(false);
  const [permissionsChannel, setPermissionsChannel] = useState<TextChannel | null>(null);
  const [isMicMuted, setIsMicMuted] = useState(false);
  const [isDeafened, setIsDeafened] = useState(false);
  const [isMemberListOpen, setIsMemberListOpen] = useState(true);
  const [isDmProfileOpen, setIsDmProfileOpen] = useState(false);
  // set by a friend_request notification click, consumed by the sidebar once
  // it has switched to the friends view / requests tab
  const [isFriendRequestsFocusPending, setIsFriendRequestsFocusPending] = useState(false);
  const notificationFeed = useNotifications();
  const [replyTarget, setReplyTarget] = useState<ChatMessageData | null>(null);
  const [highlightedMessageId, setHighlightedMessageId] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const highlightTimeoutRef = useRef<number | null>(null);
  // message to highlight after a notification click, once its conversation is
  // active and its history window is loaded
  const pendingNotificationJumpRef = useRef<{ conversationId: string; messageId: string } | null>(
    null
  );
  // last message whose DM read cursor was pushed to the server, so the same
  // message isn't re-marked on every scroll event
  const dmLastMarkedReadRef = useRef<Record<string, string>>({});
  // bell notifications already marked read because their DM conversation was
  // open - avoids retrying in a loop when the PATCH fails
  const consumedDmNotificationIdsRef = useRef<Set<string>>(new Set());

  const currentUserId = currentUser?.id ?? null;
  const guildWorkspace = useGuildWorkspace();
  const dmWorkspace = useDmWorkspace(currentUserId);
  const {
    members: currentGuildMembers,
    roles: currentGuildRoles,
    refresh: refreshGuildMembers
  } = useGuildMembers(selectedGuild?.id ?? null, selectedGuild?.owner_id ?? null);
  const currentGuildMember = useMemo(
    () => currentGuildMembers.find((member) => member.userId === currentUserId) ?? null,
    [currentGuildMembers, currentUserId]
  );
  // Gates the channel right-click menu to callers the server would authorize
  // for the channel-permissions endpoints (ManageChannels).
  const canManageChannels = useMemo(() => {
    if (!currentGuildMember) {
      return false;
    }

    const mask = effectivePermissions(
      currentGuildMember.roles,
      currentGuildRoles,
      currentGuildMember.isOwner
    );
    return hasPermission(mask, PERMISSIONS.ManageChannels);
  }, [currentGuildMember, currentGuildRoles]);

  // Callers holding MANAGE_ROLES (or the owner) may assign roles; null hides the
  // profile-card role editor. Mirrors the members panel's gating.
  const roleCaller = useMemo<RoleCaller | null>(() => {
    if (!currentGuildMember) {
      return null;
    }

    const permissions = effectivePermissions(
      currentGuildMember.roles,
      currentGuildRoles,
      currentGuildMember.isOwner
    );
    if (!canManageMemberRoles(permissions, currentGuildMember.isOwner)) {
      return null;
    }

    return {
      rank: memberRank(currentGuildMember.roles, currentGuildMember.isOwner),
      permissions,
      isOwner: currentGuildMember.isOwner
    };
  }, [currentGuildMember, currentGuildRoles]);

  // Role management for the open profile card: only when the profile is a member
  // of the current guild and the viewer can manage roles.
  const profileRoleManagement = useMemo(() => {
    if (chatMode !== 'guild' || !selectedGuild || !roleCaller || !profileMember) {
      return undefined;
    }

    const target = currentGuildMembers.find((member) => member.userId === profileMember.id);
    if (!target) {
      return undefined;
    }

    return {
      guildId: selectedGuild.id,
      member: target,
      roles: currentGuildRoles,
      caller: roleCaller,
      onChanged: () => void refreshGuildMembers()
    };
  }, [
    chatMode,
    selectedGuild,
    roleCaller,
    profileMember,
    currentGuildMembers,
    currentGuildRoles,
    refreshGuildMembers
  ]);

  // A guild switch invalidates any channel-scoped dialog left open.
  useEffect(() => {
    setPermissionsChannel(null);
  }, [selectedGuild?.id]);

  function toProfileMemberFromUser(user: UserSummaryDto) {
    return {
      id: user.id,
      name: user.display_name || user.username,
      role: 'Member' as const,
      status: toSidebarStatus(user.status),
      accent: accentForId(user.id),
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

  // A user with no guilds has nothing to show in guild mode (the sidebar
  // renders a placeholder guild); fall back to the DM view once the guild
  // list has actually loaded. Not persisted: it's a fallback, not a
  // preference.
  useEffect(() => {
    if (!isHydrated || !hasLoadedGuilds) {
      return;
    }

    if (guilds.length === 0 && chatMode === 'guild') {
      setChatMode('dm');
    }
  }, [isHydrated, hasLoadedGuilds, guilds.length, chatMode]);

  useEffect(() => {
    // Friends come straight from GET /users/{id}/friends; the DM list itself
    // is owned by useDmWorkspace. Best-effort: leave the list empty rather
    // than surface a console error (III: zero console errors in Chrome).
    let cancelled = false;

    async function loadSocialState() {
      if (!currentUserId) {
        setFriends([]);
        setBlockedUserIds([]);
        return;
      }

      const [friendList, blockedList] = await Promise.all([
        listFriends(currentUserId).catch(() => []),
        listBlockedUsers().catch(() => [])
      ]);
      if (cancelled) {
        return;
      }

      setFriends(friendList.map(toFriend));
      setBlockedUserIds(blockedList.map((entry) => entry.id));
    }

    void loadSocialState();

    return () => {
      cancelled = true;
    };
  }, [currentUserId, friendsReloadKey]);

  // re-fetch the friends list when the friend graph changed elsewhere (a
  // pending request we sent was accepted, delivered as a friend_accept
  // notification over SSE).
  useEffect(() => {
    return subscribeFriendsChanged(() => {
      setFriendsReloadKey((key) => key + 1);
    });
  }, []);

  // live presence: patch a friend's status dot when they go online/offline,
  // instead of only reflecting the status fetched at load.
  useEffect(() => {
    return onChatHubEvent('UserPresence', (event) => {
      const status = event.status === 'dnd' ? 'idle' : event.status;
      setFriends((current) => {
        let changed = false;
        const next = current.map((friend) => {
          if (friend.id !== event.user_id || friend.status === status) {
            return friend;
          }
          changed = true;
          return { ...friend, status };
        });
        return changed ? next : current;
      });
    });
  }, []);

  const activeDmDetails =
    chatMode === 'dm' && dmWorkspace.activeDm
      ? getDmDetails(dmWorkspace.activeDm, dmWorkspace.dmConversations)
      : null;
  const isActiveDmRelationshipKnown = !activeDmDetails
    ? true
    : activeDmRelationship.userId === activeDmDetails.id;
  const isActiveDmBlockedByThem =
    isActiveDmRelationshipKnown && activeDmRelationship.status === 'blocked_by_them';
  const isActiveDmBlocked =
    Boolean(activeDmDetails && blockedUserIds.includes(activeDmDetails.id)) ||
    isActiveDmBlockedByThem;
  const activeConversationId =
    chatMode === 'dm' ? (activeDmDetails?.id ?? null) : guildWorkspace.activeChannel;
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

  // per-member display overrides for the active guild: nickname, avatar, and top
  // role colour, keyed by user id. Empty outside guild mode.
  const guildMemberDisplayById = useMemo(() => {
    const map = new Map<string, { name: string; avatarUrl: string | null; nameColor: string | null }>();
    for (const member of currentGuildMembers) {
      map.set(member.userId, {
        name: member.displayName,
        avatarUrl: member.avatarUrl,
        nameColor: topRoleByPosition(member.roles)?.color ?? null
      });
    }
    return map;
  }, [currentGuildMembers]);

  // messages as rendered: in a guild, overlay each author's nickname, avatar and
  // role colour so they match the member list. The current user's own messages
  // always carry their name + avatar (even in a DM, where the fetched-profile map
  // does not include self), so nobody shows up as a bare "You" with no picture.
  const displayMessages = useMemo(() => {
    return activeMessages.map((message) => {
      if (!message.authorId) {
        return message;
      }

      if (chatMode === 'guild') {
        const member = guildMemberDisplayById.get(message.authorId);
        if (member) {
          return {
            ...message,
            author: member.name,
            avatarUrl: member.avatarUrl ?? message.avatarUrl,
            nameColor: member.nameColor
          };
        }
      }

      if (currentUser && message.authorId === currentUser.id) {
        return {
          ...message,
          author: currentUser.displayName,
          avatarUrl: currentUser.avatarUrl ?? message.avatarUrl
        };
      }

      return message;
    });
  }, [chatMode, activeMessages, guildMemberDisplayById, currentUser]);

  useEffect(() => {
    let cancelled = false;

    async function loadActiveDmRelationship() {
      if (!currentUserId || chatMode !== 'dm' || !activeDmDetails) {
        setActiveDmRelationship({ userId: null, status: null });
        return;
      }

      const relationship = await getFriendshipState(activeDmDetails.id).catch(() => null);
      if (cancelled) {
        return;
      }

      setActiveDmRelationship({ userId: activeDmDetails.id, status: relationship?.status ?? null });
    }

    void loadActiveDmRelationship();

    return () => {
      cancelled = true;
    };
  }, [currentUserId, chatMode, activeDmDetails]);

  useEffect(() => {
    let cancelled = false;

    async function loadProfileRelationship() {
      if (!currentUserId || !profileMember || profileMember.id === currentUserId) {
        setProfileRelationshipStatus(null);
        setIsProfileRelationshipKnown(true);
        return;
      }

      setIsProfileRelationshipKnown(false);
      const relationship = await getFriendshipState(profileMember.id).catch(() => null);
      if (cancelled) {
        return;
      }

      setProfileRelationshipStatus(relationship?.status ?? null);
      setIsProfileRelationshipKnown(true);
    }

    void loadProfileRelationship();

    return () => {
      cancelled = true;
    };
  }, [currentUserId, profileMember]);

  const scroll = useScrollPreservation(
    activeConversationId,
    activeMessages,
    conversationHistory.messagesByConversation,
    isHydrated
  );

  // resolve a call peer's display name + avatar: prefer the guild member (so a
  // nickname wins), then the DM conversation, then the fetched user profile.
  const resolvePeer = useCallback(
    (peerId: string | null): CallPeer => {
      if (!peerId) {
        return { name: 'Unknown user', avatarUrl: null };
      }
      const member = guildMemberDisplayById.get(peerId);
      if (member) {
        return { name: member.name, avatarUrl: member.avatarUrl };
      }
      const dm = dmWorkspace.dmConversations.find((entry) => entry.id === peerId);
      if (dm) {
        return { name: dm.name, avatarUrl: dm.avatarUrl ?? null };
      }
      const profile = userProfilesById[peerId];
      if (profile) {
        return {
          name: profile.display_name || profile.username,
          avatarUrl: profile.avatar_url ?? null
        };
      }
      return { name: 'Unknown user', avatarUrl: null };
    },
    [guildMemberDisplayById, dmWorkspace.dmConversations, userProfilesById]
  );

  const callSelf: CallPeer = { name: 'You', avatarUrl: currentUser?.avatarUrl ?? null };

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
      // always advance the server-side read cursor, even when the local badge
      // already shows 0: a message received while the conversation is open
      // never increments the local count, but the server did - skipping the
      // call here left a phantom unread that resurfaced the sender's badge on
      // the next refetch/reload
      if (dmLastMarkedReadRef.current[partnerId] === latestMessage.id) {
        return;
      }
      dmLastMarkedReadRef.current[partnerId] = latestMessage.id;

      markDirectMessageRead(partnerId, latestMessage.id)
        .then(() => {
          dmWorkspace.setDmConversations((current) =>
            current.map((dm) => (dm.id === partnerId ? { ...dm, unreadCount: 0 } : dm))
          );
        })
        .catch(() => {
          // best effort: retry next time the viewport is at the bottom
          delete dmLastMarkedReadRef.current[partnerId];
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
    guildWorkspace.channelReadStates
  ]);

  // reading a DM in place consumes its bell notifications too: without this,
  // messages already seen in the open conversation kept counting toward the
  // bell badge until the user cleared them by hand
  useEffect(() => {
    if (chatMode !== 'dm' || !activeConversationId || !scroll.isNearBottom) {
      return;
    }

    for (const notification of notificationFeed.notifications) {
      if (
        notification.type === 'dm' &&
        !notification.read &&
        notification.actor_id === activeConversationId &&
        !consumedDmNotificationIdsRef.current.has(notification.id)
      ) {
        consumedDmNotificationIdsRef.current.add(notification.id);
        void notificationFeed.markRead(notification.id);
      }
    }
    // notificationFeed is a fresh object every render - depend on its data,
    // not the whole object
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [chatMode, activeConversationId, scroll.isNearBottom, notificationFeed.notifications]);

  // after a notification click, jump to the linked message once its
  // conversation is the active one and the message is in the loaded window
  useEffect(() => {
    const pending = pendingNotificationJumpRef.current;
    if (!pending) {
      return;
    }
    if (pending.conversationId !== activeConversationId) {
      // the user navigated somewhere else before the jump could happen
      pendingNotificationJumpRef.current = null;
      return;
    }
    if (activeMessages.some((message) => message.id === pending.messageId)) {
      pendingNotificationJumpRef.current = null;
      handleJumpToMessage(pending.messageId);
    }
    // handleJumpToMessage is stable in behavior but recreated every render
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [activeConversationId, activeMessages]);

  const activeDraft = (activeConversationId && draftsByConversation[activeConversationId]) ?? '';
  const isComposerDisabled =
    !activeConversationId ||
    isActiveDmBlocked ||
    (chatMode === 'dm' && Boolean(activeDmDetails) && !isActiveDmRelationshipKnown);
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
        activity: 'No recent activity',
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

  function handleSendMessageToProfile(member: GuildMember) {
    if (
      member.id === currentUserId ||
      blockedUserIds.includes(member.id) ||
      profileRelationshipStatus === 'blocked_by_them'
    ) {
      setProfileMember(null);
      return;
    }

    dmWorkspace.openDmWith({
      id: member.id,
      name: member.name,
      status: member.status,
      accent: member.accent,
      avatarUrl: member.avatarUrl,
      bannerUrl: member.bannerUrl,
      bio: member.bio
    });
    setChatMode('dm');
    setMobilePane('messages');
    window.sessionStorage.setItem(LAST_CHAT_MODE_KEY, 'dm');
    setProfileMember(null);
  }

  async function handleUnfriend(member: GuildMember) {
    const friend = friends.find((entry) => entry.id === member.id);
    if (!friend) {
      return;
    }

    setProfileMember(null);
    // optimistic: drop them locally, then confirm with the server and re-sync.
    setFriends((current) => current.filter((entry) => entry.id !== member.id));
    try {
      await deleteFriendship(friend.friendshipId);
    } catch {
      // best effort: a failed delete reconciles on the next friends re-fetch
    }
    dispatchFriendsChanged();
  }

  async function handleAddFriend(member: GuildMember) {
    if (member.id === currentUserId) {
      return;
    }

    try {
      await sendFriendRequest(member.id);
      setProfileMember(null);
      pushToast({
        title: 'Friends',
        description: `Friend request sent to ${member.name}.`,
        tone: 'success'
      });
    } catch (error) {
      pushToast({
        title: 'Friends',
        description: error instanceof Error ? error.message : 'Failed to send friend request.',
        tone: 'error'
      });
    }
  }

  async function handleBlock(member: GuildMember) {
    if (member.id === currentUserId) {
      return;
    }

    try {
      await blockUser(member.id);
      setProfileMember(null);
      setFriends((current) => current.filter((entry) => entry.id !== member.id));
      dispatchFriendsChanged();
      pushToast({
        title: 'Friends',
        description: `${member.name} blocked.`,
        tone: 'success'
      });
    } catch (error) {
      pushToast({
        title: 'Friends',
        description: error instanceof Error ? error.message : 'Failed to block user.',
        tone: 'error'
      });
    }
  }

  async function handleUnblock(member: GuildMember) {
    if (member.id === currentUserId) {
      return;
    }

    try {
      await unblockUser(member.id);
      setBlockedUserIds((current) => current.filter((id) => id !== member.id));
      dispatchFriendsChanged();
      pushToast({
        title: 'Friends',
        description: `${member.name} unblocked.`,
        tone: 'success'
      });
    } catch (error) {
      pushToast({
        title: 'Friends',
        description: error instanceof Error ? error.message : 'Failed to unblock user.',
        tone: 'error'
      });
    }
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
    if (
      !activeConversationId ||
      isActiveDmBlocked ||
      (chatMode === 'dm' && Boolean(activeDmDetails) && !isActiveDmRelationshipKnown)
    ) {
      return;
    }

    if ((!content && !hasReadyAttachment) || isSendDisabled) {
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

  // routes a clicked notification to its target: a DM opens the sender's
  // conversation (the actor is the partner) and a mention switches
  // guild+channel, both highlighting the message carried by source_id; a
  // friend request opens the friends requests tab, a guild invite the join
  // form on /guilds, and a guild welcome the guild itself (source_id)
  function handleOpenNotification(notification: NotificationDto) {
    if (notification.type === 'dm') {
      if (!notification.actor_id) {
        return;
      }
      pendingNotificationJumpRef.current = notification.source_id
        ? { conversationId: notification.actor_id, messageId: notification.source_id }
        : null;
      handleSelectDm(notification.actor_id);
    } else if (notification.type === 'mention') {
      if (notification.payload.guild_id !== selectedGuild?.id) {
        selectGuild(notification.payload.guild_id);
      }
      pendingNotificationJumpRef.current = notification.source_id
        ? { conversationId: notification.payload.channel_id, messageId: notification.source_id }
        : null;
      handleSelectChannel(notification.payload.channel_id);
    } else if (notification.type === 'friend_request') {
      handleOpenDms();
      setIsFriendRequestsFocusPending(true);
      setMobilePane('channels');
    } else if (notification.type === 'friend_accept') {
      handleOpenDms();
      setMobilePane('channels');
    } else if (notification.type === 'guild_invite') {
      // no "invites addressed to me" endpoint exists, so the closest target
      // is the join-a-guild form where the invite code is redeemed
      router.push('/guilds#join-guild');
    } else if (notification.type === 'guild_welcome') {
      if (!notification.source_id) {
        return;
      }
      selectGuild(notification.source_id);
      handleOpenGuild();
      setMobilePane('channels');
    } else {
      return;
    }

    void notificationFeed.markRead(notification.id);
    handleCloseNotifications();
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
        activity: 'No recent activity',
        bio: directMessage.bio ?? null,
        avatarUrl: directMessage.avatarUrl ?? null,
        bannerUrl: directMessage.bannerUrl ?? null
      });
      return;
    }

    if (isCurrentUserMessage && currentUser) {
      // only surface guild roles on the card when actually viewing a guild; a DM
      // is not guild-scoped, so it must not show the selected guild's roles.
      setProfileMember(
        chatMode === 'guild' && currentGuildMember
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
              accent: accentForId(currentUser.id),
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

  async function handleOpenFriendProfile(friend: Friend) {
    const directMessage = dmWorkspace.dmConversations.find((dm) => dm.id === friend.id);
    if (directMessage) {
      setProfileMember({
        id: directMessage.id,
        name: directMessage.name,
        role: 'Member',
        status: directMessage.status,
        accent: directMessage.accent,
        activity: 'No recent activity',
        bio: directMessage.bio ?? null,
        avatarUrl: directMessage.avatarUrl ?? null,
        bannerUrl: directMessage.bannerUrl ?? null
      });
      return;
    }

    const cachedUser = userProfilesById[friend.id];
    if (cachedUser) {
      setProfileMember(toProfileMemberFromUser(cachedUser));
      return;
    }

    const fetchedUsers = await getUsersByIds([friend.id]).catch(() => []);
    const fetchedUser = fetchedUsers[0];
    setProfileMember(
      toProfileMemberFromUser(
        fetchedUser ?? {
          id: friend.id,
          username: friend.name,
          display_name: friend.name,
          status: friend.status,
          bio: null,
          avatar_url: friend.avatarUrl,
          banner_url: null
        }
      )
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
    bellRef,
    onOpenSettings: handleOpenSettings,
    onOpenGuildSettings: handleOpenGuildSettings
  };

  return (
    <div
      className="mx-auto flex h-screen w-full gap-4 px-3 py-4 md:px-5 md:py-9"
      onContextMenu={(event) => event.preventDefault()}
    >
      {!isHydrated ? (
        <div className="flex min-h-0 flex-1 rounded-[1rem] bg-secondary-bg ring-1 ring-stroke" />
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
              focusFriendRequests={isFriendRequestsFocusPending}
              onFriendRequestsFocused={() => setIsFriendRequestsFocusPending(false)}
              onOpenFriendProfile={handleOpenFriendProfile}
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
              canManageChannels={canManageChannels}
              onSelectChannel={handleSelectChannel}
              onOpenChannelPermissions={setPermissionsChannel}
            />
          )}

          <section
            className={`${
              mobilePane === 'messages' ? 'flex' : 'hidden'
            } min-h-0 flex-1 flex-col overflow-hidden rounded-[1rem] bg-secondary-bg ring-1 ring-stroke md:flex`}
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
              activeMessages={displayMessages}
              currentUserId={currentUserId}
              blockedUserIds={chatMode === 'guild' ? blockedUserIds : []}
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
              isActiveDmBlockedByThem={isActiveDmBlockedByThem}
              isActiveDmBlocked={isActiveDmBlocked}
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
            <GuildMemberList
              activeChannelId={guildWorkspace.activeChannel ?? null}
              onOpenProfile={setProfileMember}
            />
          ) : null}

          {chatMode === 'dm' && isDmProfileOpen && activeDmProfileMember ? (
            <ProfileCard
              member={activeDmProfileMember}
              variant="side"
              currentUserId={currentUserId}
              isBlocked={blockedUserIds.includes(activeDmProfileMember.id)}
              isBlockedByThem={isActiveDmBlockedByThem}
              onAddFriend={
                activeDmProfileMember.id !== currentUserId &&
                !blockedUserIds.includes(activeDmProfileMember.id) &&
                !friends.some((entry) => entry.id === activeDmProfileMember.id)
                  ? handleAddFriend
                  : undefined
              }
              onBlock={
                activeDmProfileMember.id !== currentUserId &&
                !blockedUserIds.includes(activeDmProfileMember.id)
                  ? handleBlock
                  : undefined
              }
              onUnblock={
                activeDmProfileMember.id !== currentUserId &&
                blockedUserIds.includes(activeDmProfileMember.id)
                  ? handleUnblock
                  : undefined
              }
              onClose={handleCloseDmProfile}
            />
          ) : null}

          {profileMember ? (
            <ProfileCard
              member={profileMember}
              currentUserId={currentUserId}
              isBlocked={blockedUserIds.includes(profileMember.id)}
              isBlockedByThem={isProfileRelationshipKnown && profileRelationshipStatus === 'blocked_by_them'}
              roleManagement={profileRoleManagement}
              onClose={handleCloseAuthorProfile}
              onAddFriend={
                profileMember.id !== currentUserId &&
                isProfileRelationshipKnown &&
                profileRelationshipStatus !== 'blocked_by_them' &&
                !blockedUserIds.includes(profileMember.id) &&
                !friends.some((entry) => entry.id === profileMember.id)
                  ? handleAddFriend
                  : undefined
              }
              onBlock={
                profileMember.id !== currentUserId &&
                isProfileRelationshipKnown &&
                profileRelationshipStatus !== 'blocked_by_them' &&
                !blockedUserIds.includes(profileMember.id)
                  ? handleBlock
                  : undefined
              }
              onUnblock={
                profileMember.id !== currentUserId &&
                isProfileRelationshipKnown &&
                profileRelationshipStatus !== 'blocked_by_them' &&
                blockedUserIds.includes(profileMember.id)
                  ? handleUnblock
                  : undefined
              }
              onSendMessage={
                profileMember.id !== currentUserId &&
                isProfileRelationshipKnown &&
                profileRelationshipStatus !== 'blocked_by_them' &&
                !blockedUserIds.includes(profileMember.id)
                  ? handleSendMessageToProfile
                  : undefined
              }
              onUnfriend={
                friends.some((entry) => entry.id === profileMember.id)
                  ? handleUnfriend
                  : undefined
              }
            />
          ) : null}

          {isNotificationCardOpen ? (
            <NotificationCard
              feed={notificationFeed}
              anchorRef={bellRef}
              onClose={handleCloseNotifications}
              onOpenNotification={handleOpenNotification}
            />
          ) : null}

          {isSettingsOpen ? (
            <SettingsModal
              currentUser={currentUser}
              onClose={handleCloseSettings}
              onDisconnect={handleDisconnect}
            />
          ) : null}

          {isGuildSettingsOpen && selectedGuild ? (
            <GuildSettingsModal
              guildId={selectedGuild.id}
              onClose={handleCloseGuildSettings}
              onChannelsChanged={guildWorkspace.refreshChannels}
            />
          ) : null}

          {permissionsChannel && selectedGuild ? (
            <ChannelPermissionsModal
              guildId={selectedGuild.id}
              channelId={permissionsChannel.id}
              channelName={permissionsChannel.name}
              onClose={() => setPermissionsChannel(null)}
            />
          ) : null}

          <IncomingCallOverlay resolvePeer={resolvePeer} />
          <CallWindow resolvePeer={resolvePeer} self={callSelf} />
        </>
      )}
    </div>
  );
}
