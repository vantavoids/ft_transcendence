'use client';

import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Ban,
  Check,
  LoaderCircle,
  Search,
  UserPlus,
  UserRound,
  UserX,
  Users
} from 'lucide-react';
import { AvatarWithStatus } from './avatar-with-status';
import type { ChatMessageData } from './chat-message';
import type { DirectMessage } from './dm-list';
import { SearchInput } from './search-input';
import {
  accentForUserId,
  toSidebarStatus,
  toUserStatusLabel,
  type CurrentUserProfile
} from '../shared/mappers/user';
import {
  blockUser,
  deleteFriendship,
  getFriendshipState,
  listFriendRequests,
  searchUsers,
  sendFriendRequest,
  unblockUser,
  updateFriendship,
  type FriendRequestDto,
  type FriendshipStateDto,
  type SearchUserDto
} from '../shared/api/user';

export type Friend = {
  id: string;
  name: string;
  status: DirectMessage['status'];
  accent: ChatMessageData['accent'];
  note: string;
};

type FriendsListProps = {
  friends: Friend[];
  search: string;
  onSearchChange: (value: string) => void;
  // one-shot deep-link (e.g. a friend_request notification click): while true,
  // force the requests tab, then acknowledge so it doesn't stick on remounts
  focusRequests?: boolean;
  onRequestsFocused?: () => void;
};

type PendingRequest = FriendRequestDto & {
  label: string;
};

type DiscoveryResult = SearchUserDto & {
  relationship: FriendshipStateDto['status'] | null;
  friendshipId: string | null;
};

function relationshipLabel(status: DiscoveryResult['relationship']) {
  switch (status) {
    case 'accepted':
      return 'Friends';
    case 'pending_outgoing':
      return 'Request sent';
    case 'pending_incoming':
      return 'Incoming request';
    case 'blocked_by_me':
      return 'Blocked';
    case 'blocked_by_them':
      return 'Blocked by them';
    default:
      return 'Not connected';
  }
}

function resultActionsLabel(status: DiscoveryResult['relationship']) {
  switch (status) {
    case 'accepted':
      return 'Block';
    case 'pending_outgoing':
      return 'Cancel request';
    case 'pending_incoming':
      return 'Review request';
    case 'blocked_by_me':
      return 'Unblock';
    case 'blocked_by_them':
      return 'Blocked';
    default:
      return 'Add friend';
  }
}

export function FriendsList({
  friends,
  search,
  onSearchChange,
  focusRequests = false,
  onRequestsFocused
}: FriendsListProps) {
  const [friendName, setFriendName] = useState('');
  const [activeTab, setActiveTab] = useState<'friends' | 'requests' | 'discover'>(
    focusRequests ? 'requests' : 'friends'
  );

  useEffect(() => {
    if (focusRequests) {
      setActiveTab('requests');
      onRequestsFocused?.();
    }
  }, [focusRequests, onRequestsFocused]);
  const [pendingRequests, setPendingRequests] = useState<PendingRequest[]>([]);
  const [pendingLoading, setPendingLoading] = useState(false);
  const [discoverResults, setDiscoverResults] = useState<DiscoveryResult[]>([]);
  const [discoverLoading, setDiscoverLoading] = useState(false);
  const [actionError, setActionError] = useState('');
  const [actionSuccess, setActionSuccess] = useState('');

  const term = search.trim().toLowerCase();

  const filteredFriends = term
    ? friends.filter(
        (friend) =>
          friend.name.toLowerCase().includes(term) || friend.note.toLowerCase().includes(term)
      )
    : friends;

  const pendingByUserId = useMemo(() => {
    return new Map(pendingRequests.map((request) => [request.user.id, request]));
  }, [pendingRequests]);

  const pendingIncoming = pendingRequests.filter((request) => request.direction === 'incoming');
  const pendingOutgoing = pendingRequests.filter((request) => request.direction === 'outgoing');

  const loadPendingRequests = useCallback(async () => {
    setPendingLoading(true);
    try {
      const requests = await listFriendRequests('all');
      setPendingRequests(
        requests.map((request) => ({
          ...request,
          label:
            request.direction === 'incoming'
              ? 'Incoming request'
              : `Outgoing request · ${request.created_at}`
        }))
      );
    } catch (loadError) {
      setActionError(loadError instanceof Error ? loadError.message : 'Failed to load requests.');
    } finally {
      setPendingLoading(false);
    }
  }, []);

  const loadDiscoveryResults = useCallback(
    async (query: string) => {
    const trimmed = query.trim();
    if (trimmed.length < 2) {
      setDiscoverResults([]);
      setDiscoverLoading(false);
      return;
    }

    setDiscoverLoading(true);
    try {
      const users = await searchUsers(trimmed);
      const results = await Promise.all(
        users.map(async (user) => {
          const [relationship, request] = await Promise.all([
            getFriendshipState(user.id).catch(() => null),
            Promise.resolve(pendingByUserId.get(user.id) ?? null)
          ]);

          return {
            ...user,
            relationship: relationship?.status ?? null,
            friendshipId: request?.friendship_id ?? null
          };
        })
      );

      setDiscoverResults(results);
    } catch (loadError) {
      setActionError(loadError instanceof Error ? loadError.message : 'Failed to search users.');
      setDiscoverResults([]);
    } finally {
      setDiscoverLoading(false);
    }
    },
    [pendingByUserId]
  );

  useEffect(() => {
    void loadPendingRequests();
  }, [loadPendingRequests]);

  useEffect(() => {
    if (activeTab !== 'discover') {
      return;
    }

    const timer = window.setTimeout(() => {
      void loadDiscoveryResults(search);
    }, 250);

    return () => {
      window.clearTimeout(timer);
    };
  }, [activeTab, search, loadDiscoveryResults]);

  const refreshEverything = useCallback(async () => {
    await Promise.all([loadPendingRequests(), loadDiscoveryResults(search)]);
  }, [loadDiscoveryResults, loadPendingRequests, search]);

  async function handleAddByUsername() {
    const query = friendName.trim();
    if (!query) {
      return;
    }

    setActionError('');
    setActionSuccess('');

    try {
      const matches = await searchUsers(query, 10);
      const exact = matches.find((user) => user.username.toLowerCase() === query.toLowerCase());

      if (!exact) {
        setActionError('No exact username match found.');
        return;
      }

      await sendFriendRequest(exact.id);
      setFriendName('');
      setActionSuccess(`Friend request sent to @${exact.username}.`);
      await refreshEverything();
    } catch (sendError) {
      setActionError(sendError instanceof Error ? sendError.message : 'Failed to send request.');
    }
  }

  async function handleAccept(friendshipId: string) {
    setActionError('');
    setActionSuccess('');
    try {
      await updateFriendship(friendshipId, 'accepted');
      setActionSuccess('Friend request accepted.');
      await refreshEverything();
    } catch (acceptError) {
      setActionError(acceptError instanceof Error ? acceptError.message : 'Failed to accept.');
    }
  }

  async function handleDecline(friendshipId: string) {
    setActionError('');
    setActionSuccess('');
    try {
      await deleteFriendship(friendshipId);
      setActionSuccess('Friend request removed.');
      await refreshEverything();
    } catch (removeError) {
      setActionError(removeError instanceof Error ? removeError.message : 'Failed to remove request.');
    }
  }

  async function handleBlock(userId: string) {
    setActionError('');
    setActionSuccess('');
    try {
      await blockUser(userId);
      setActionSuccess('User blocked.');
      await refreshEverything();
    } catch (blockError) {
      setActionError(blockError instanceof Error ? blockError.message : 'Failed to block user.');
    }
  }

  async function handleUnblock(userId: string) {
    setActionError('');
    setActionSuccess('');
    try {
      await unblockUser(userId);
      setActionSuccess('User unblocked.');
      await refreshEverything();
    } catch (unblockError) {
      setActionError(unblockError instanceof Error ? unblockError.message : 'Failed to unblock user.');
    }
  }

  async function handleSendRequest(user: DiscoveryResult) {
    setActionError('');
    setActionSuccess('');
    try {
      await sendFriendRequest(user.id);
      setActionSuccess(`Friend request sent to @${user.username}.`);
      await refreshEverything();
    } catch (sendError) {
      setActionError(sendError instanceof Error ? sendError.message : 'Failed to send request.');
    }
  }

  const isDiscoverTab = activeTab === 'discover';
  const isRequestsTab = activeTab === 'requests';
  const placeholder =
    activeTab === 'discover' ? 'Search users' : activeTab === 'requests' ? 'Filter requests' : 'Search friends';
  const pendingCount = pendingRequests.length;
  const requestCountLabel = pendingCount > 0 ? `(${pendingCount})` : '';

  return (
    <div className="flex min-h-0 flex-1 flex-col">
      <div className="px-3 pb-3 sm:px-5">
        <div className="grid grid-cols-3 gap-2 rounded-md bg-panel p-1">
          <button
            type="button"
            onClick={() => setActiveTab('friends')}
            className={`flex h-9 items-center justify-center gap-2 rounded text-sm font-bold transition ${
              activeTab === 'friends' ? 'bg-frame text-white' : 'text-white/40 hover:text-white'
            }`}
          >
            <Users className="h-4 w-4" strokeWidth={1.8} />
            Friends ({friends.length})
          </button>
          <button
            type="button"
            onClick={() => setActiveTab('requests')}
            className={`flex h-9 items-center justify-center gap-2 rounded text-sm font-bold transition ${
              activeTab === 'requests' ? 'bg-frame text-white' : 'text-white/40 hover:text-white'
            }`}
          >
            <UserRound className="h-4 w-4" strokeWidth={1.8} />
            Requests{requestCountLabel}
          </button>
          <button
            type="button"
            onClick={() => setActiveTab('discover')}
            className={`flex h-9 items-center justify-center gap-2 rounded text-sm font-bold transition ${
              activeTab === 'discover' ? 'bg-frame text-white' : 'text-white/40 hover:text-white'
            }`}
          >
            <Search className="h-4 w-4" strokeWidth={1.8} />
            Discover
          </button>
        </div>

        {isRequestsTab ? null : (
          <SearchInput value={search} onChange={onSearchChange} placeholder={placeholder} className="mt-3" />
        )}
      </div>

      <div className="min-h-0 flex-1 overflow-y-auto px-3 pb-5 sm:px-5">
        {isRequestsTab ? (
          <section className="grid gap-4">
            <div className="grid gap-2">
              <h3 className="font-category text-[0.72rem] uppercase tracking-[0.16em] text-category">
                Incoming ({pendingIncoming.length})
              </h3>
              {pendingLoading && pendingIncoming.length === 0 ? (
                <div className="h-20 animate-pulse rounded-md bg-panel/70" />
              ) : pendingIncoming.length === 0 ? (
                <p className="rounded-md border border-white/8 bg-panel px-3 py-3 text-sm text-white/45">
                  No incoming requests.
                </p>
              ) : (
                <div className="space-y-1">
                  {pendingIncoming.map((request) => (
                    <RequestRow
                      key={request.friendship_id}
                      request={request}
                      onAccept={() => void handleAccept(request.friendship_id)}
                      onDecline={() => void handleDecline(request.friendship_id)}
                      onBlock={() => void handleBlock(request.user.id)}
                    />
                  ))}
                </div>
              )}
            </div>

            <div className="grid gap-2">
              <h3 className="font-category text-[0.72rem] uppercase tracking-[0.16em] text-category">
                Outgoing ({pendingOutgoing.length})
              </h3>
              {pendingLoading && pendingOutgoing.length === 0 ? (
                <div className="h-20 animate-pulse rounded-md bg-panel/70" />
              ) : pendingOutgoing.length === 0 ? (
                <p className="rounded-md border border-white/8 bg-panel px-3 py-3 text-sm text-white/45">
                  No outgoing requests.
                </p>
              ) : (
                <div className="space-y-1">
                  {pendingOutgoing.map((request) => (
                    <RequestRow
                      key={request.friendship_id}
                      request={request}
                      onAccept={() => void handleAccept(request.friendship_id)}
                      onDecline={() => void handleDecline(request.friendship_id)}
                      onBlock={() => void handleBlock(request.user.id)}
                      outgoing
                    />
                  ))}
                </div>
              )}
            </div>
          </section>
        ) : isDiscoverTab ? (
          <section className="grid gap-3">
            {search.trim().length < 2 ? (
              <p className="rounded-md border border-white/8 bg-panel px-3 py-3 text-sm text-white/45">
                Type at least 2 characters to search users.
              </p>
            ) : discoverLoading ? (
              <div className="space-y-2">
                {[0, 1, 2].map((index) => (
                  <div key={index} className="h-16 animate-pulse rounded-md bg-panel/70" />
                ))}
              </div>
            ) : discoverResults.length === 0 ? (
              <p className="rounded-md border border-white/8 bg-panel px-3 py-3 text-sm text-white/45">
                No users found.
              </p>
            ) : (
              <div className="space-y-1">
                {discoverResults.map((user) => {
                  const relationship = relationshipLabel(user.relationship);
                  const actionLabel = resultActionsLabel(user.relationship);
                  const isBlockedByThem = user.relationship === 'blocked_by_them';
                  return (
                    <div
                      key={user.id}
                      className="flex min-h-[4.6rem] items-center gap-3 rounded-lg px-3 py-2 text-left text-grey-link transition hover:bg-frame/60 hover:text-white"
                    >
                      <AvatarWithStatus
                        name={user.display_name || user.username}
                        accent={accentForUserId(user.id)}
                        status={toSidebarStatus(user.status)}
                        avatarUrl={user.avatar_url ?? null}
                      />
                      <div className="min-w-0 flex-1">
                        <div className="flex min-w-0 items-center justify-between gap-3">
                          <span className="block truncate text-[1rem] font-bold">
                            {user.display_name || user.username}
                          </span>
                          <span className="mono-detail shrink-0 text-xs text-white/30">
                            @{user.username}
                          </span>
                        </div>
                        <div className="mt-0.5 flex min-w-0 flex-wrap items-center justify-between gap-x-3 gap-y-1.5">
                          <span className="block min-w-0 flex-1 truncate text-sm text-white/35">
                            {relationship} · {toUserStatusLabel(user.status)}
                          </span>
                          <div className="flex max-w-full flex-wrap items-center justify-end gap-2">
                            {user.relationship === 'none' ? (
                              <button
                                type="button"
                                onClick={() => void handleSendRequest(user)}
                                className="flex h-8 items-center gap-1.5 rounded-md bg-aqua px-3 text-xs font-bold text-primary-bg transition hover:bg-white"
                              >
                                <UserPlus className="h-3.5 w-3.5" strokeWidth={2} />
                                {actionLabel}
                              </button>
                            ) : user.relationship === 'pending_outgoing' ? (
                              <>
                                {user.friendshipId ? (
                                  <button
                                    type="button"
                                    onClick={() => void handleDecline(user.friendshipId!)}
                                    className="flex h-8 items-center gap-1.5 rounded-md border border-white/10 bg-frame px-3 text-xs font-semibold text-white/80 transition hover:text-white"
                                  >
                                    <XButtonIcon />
                                    Cancel
                                  </button>
                                ) : null}
                                <button
                                  type="button"
                                  onClick={() => void handleBlock(user.id)}
                                  className="flex h-8 items-center gap-1.5 rounded-md border border-pink/25 bg-pink/10 px-3 text-xs font-semibold text-pink transition hover:border-pink/45 hover:bg-pink/15"
                                >
                                  <Ban className="h-3.5 w-3.5" strokeWidth={2} />
                                  Block
                                </button>
                              </>
                            ) : user.relationship === 'pending_incoming' ? (
                              <>
                                {user.friendshipId ? (
                                  <>
                                    <button
                                      type="button"
                                      onClick={() => void handleAccept(user.friendshipId!)}
                                      className="flex h-8 items-center gap-1.5 rounded-md bg-aqua px-3 text-xs font-bold text-primary-bg transition hover:bg-white"
                                    >
                                      <Check className="h-3.5 w-3.5" strokeWidth={2} />
                                      Accept
                                    </button>
                                    <button
                                      type="button"
                                      onClick={() => void handleDecline(user.friendshipId!)}
                                      className="flex h-8 items-center gap-1.5 rounded-md border border-white/10 bg-frame px-3 text-xs font-semibold text-white/80 transition hover:text-white"
                                    >
                                      <UserX className="h-3.5 w-3.5" strokeWidth={2} />
                                      Decline
                                    </button>
                                  </>
                                ) : null}
                                <button
                                  type="button"
                                  onClick={() => void handleBlock(user.id)}
                                  className="flex h-8 items-center gap-1.5 rounded-md border border-pink/25 bg-pink/10 px-3 text-xs font-semibold text-pink transition hover:border-pink/45 hover:bg-pink/15"
                                >
                                  <Ban className="h-3.5 w-3.5" strokeWidth={2} />
                                  Block
                                </button>
                              </>
                            ) : user.relationship === 'blocked_by_me' ? (
                              <button
                                type="button"
                                onClick={() => void handleUnblock(user.id)}
                                className="flex h-8 items-center gap-1.5 rounded-md border border-white/10 bg-frame px-3 text-xs font-semibold text-white/80 transition hover:text-white"
                              >
                                <Check className="h-3.5 w-3.5" strokeWidth={2} />
                                Unblock
                              </button>
                            ) : isBlockedByThem ? (
                              <span className="rounded-md border border-white/10 bg-frame px-3 py-2 text-xs font-semibold text-white/45">
                                No action available
                              </span>
                            ) : (
                              <button
                                type="button"
                                onClick={() => void handleBlock(user.id)}
                                className="flex h-8 items-center gap-1.5 rounded-md border border-pink/25 bg-pink/10 px-3 text-xs font-semibold text-pink transition hover:border-pink/45 hover:bg-pink/15"
                              >
                                <Ban className="h-3.5 w-3.5" strokeWidth={2} />
                                Block
                              </button>
                            )}
                          </div>
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </section>
        ) : (
          <div className="space-y-1">
            {filteredFriends.length === 0 ? (
              <div className="flex h-full min-h-[16rem] flex-col items-center justify-center px-5 text-center">
                <div className="flex h-14 w-14 items-center justify-center rounded-full bg-panel text-[#8b8b8f]">
                  <UserPlus className="h-6 w-6" strokeWidth={1.8} />
                </div>
                <p className="mt-4 text-[1rem] font-bold text-white">No friends found</p>
                <p className="mt-1 max-w-[16rem] text-sm leading-5 text-white/35">
                  Try another name or activity.
                </p>
              </div>
            ) : (
              <div className="space-y-1">
                {filteredFriends.map((friend) => (
                  <button
                    key={friend.id}
                    type="button"
                    className="flex h-[4.25rem] w-full items-center gap-3 rounded-lg px-3 text-left text-grey-link transition hover:bg-frame/60 hover:text-white"
                  >
                    <AvatarWithStatus name={friend.name} accent={friend.accent} status={friend.status} />
                    <span className="min-w-0 flex-1">
                      <span className="block truncate text-[1rem] font-bold">{friend.name}</span>
                      <span className="mt-0.5 block truncate text-sm text-white/35">
                        {friend.note}
                      </span>
                    </span>
                    <span className="rounded-md border border-white/10 bg-panel px-2.5 py-1 text-[0.68rem] font-semibold uppercase tracking-[0.14em] text-white/35">
                      {toUserStatusLabel(friend.status === 'idle' ? 'idle' : friend.status === 'online' ? 'online' : 'offline')}
                    </span>
                  </button>
                ))}
              </div>
            )}
          </div>
        )}
      </div>

      <div className="shrink-0 border-t border-white/8 px-4 py-4">
        <form
          onSubmit={(event) => {
            event.preventDefault();
            void handleAddByUsername();
          }}
          className="flex h-11 items-center gap-2 rounded-md bg-panel px-3 text-muted"
        >
          <input
            value={friendName}
            onChange={(event) => setFriendName(event.target.value)}
            placeholder="Username"
            className="min-w-0 flex-1 bg-transparent text-sm text-white outline-none placeholder:text-muted"
          />
          <button
            type="submit"
            disabled={!friendName.trim()}
            className="flex h-8 w-8 shrink-0 items-center justify-center rounded-md bg-aqua text-primary-bg transition hover:bg-white disabled:cursor-not-allowed disabled:bg-frame disabled:text-white/25"
            aria-label="Add friend"
          >
            <UserPlus className="h-4 w-4" strokeWidth={2} />
          </button>
        </form>
        {actionError ? (
          <p className="mt-2 rounded-md border border-pink/25 bg-pink/10 px-3 py-2 text-sm text-pink">
            {actionError}
          </p>
        ) : null}
        {actionSuccess ? (
          <p className="mt-2 rounded-md border border-lime/25 bg-lime/10 px-3 py-2 text-sm text-lime">
            {actionSuccess}
          </p>
        ) : null}
      </div>
    </div>
  );
}

function XButtonIcon() {
  return <Search className="h-3.5 w-3.5 rotate-45" strokeWidth={2} />;
}

function RequestRow({
  request,
  onAccept,
  onDecline,
  onBlock,
  outgoing = false
}: {
  request: PendingRequest;
  onAccept: () => void;
  onDecline: () => void;
  onBlock: () => void;
  outgoing?: boolean;
}) {
  return (
    <div className="flex items-center gap-3 rounded-lg px-3 py-2 text-left text-grey-link transition hover:bg-frame/60 hover:text-white">
      <AvatarWithStatus
        name={request.user.display_name || request.user.username}
        accent={accentForUserId(request.user.id)}
        status={toSidebarStatus(request.user.status)}
        avatarUrl={request.user.avatar_url ?? null}
      />
      <div className="min-w-0 flex-1">
        <div className="flex min-w-0 items-center justify-between gap-3">
          <span className="block truncate text-[1rem] font-bold">
            {request.user.display_name || request.user.username}
          </span>
          <span className="mono-detail shrink-0 text-xs text-white/30">
            @{request.user.username}
          </span>
        </div>
        <div className="mt-0.5 flex min-w-0 flex-wrap items-center justify-between gap-x-3 gap-y-1.5">
          <span className="block min-w-0 flex-1 truncate text-sm text-white/35">
            {outgoing ? 'Outgoing request' : 'Incoming request'}
          </span>
          <div className="flex max-w-full flex-wrap items-center justify-end gap-2">
            {outgoing ? null : (
              <button
                type="button"
                onClick={onAccept}
                className="flex h-8 items-center gap-1.5 rounded-md bg-aqua px-3 text-xs font-bold text-primary-bg transition hover:bg-white"
              >
                <Check className="h-3.5 w-3.5" strokeWidth={2} />
                Accept
              </button>
            )}
            <button
              type="button"
              onClick={onDecline}
              className="flex h-8 items-center gap-1.5 rounded-md border border-white/10 bg-frame px-3 text-xs font-semibold text-white/80 transition hover:text-white"
            >
              <UserX className="h-3.5 w-3.5" strokeWidth={2} />
              {outgoing ? 'Cancel' : 'Decline'}
            </button>
            <button
              type="button"
              onClick={onBlock}
              className="flex h-8 items-center gap-1.5 rounded-md border border-pink/25 bg-pink/10 px-3 text-xs font-semibold text-pink transition hover:border-pink/45 hover:bg-pink/15"
            >
              <Ban className="h-3.5 w-3.5" strokeWidth={2} />
              Block
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
