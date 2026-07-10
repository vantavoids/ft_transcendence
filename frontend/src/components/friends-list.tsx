'use client';

import { useEffect, useMemo, useState } from 'react';
import { LoaderCircle, Search, UserPlus, UserRound } from 'lucide-react';
import {
  blockUser,
  createFriendRequest,
  deleteFriendRequest,
  listBlockedUsers,
  listFriendRequests,
  listFriends,
  searchUsers,
  unblockUser,
  updateFriendRequest,
  type BlockListItemDto,
  type FriendRequestListItemDto,
  type FriendSummaryDto,
  type UserSummaryDto
} from '../shared/api/user';

type FriendsListProps = {
  currentUserId: string | null;
  refreshKey: number;
  search: string;
  onSearchChange: (value: string) => void;
  onOpenProfile: (userId: string, friendshipId?: string) => void;
  onDataMutated: () => void;
};

type Tab = 'friends' | 'pending' | 'blocked';

export type Friend = {
  id: string;
  name: string;
  status: FriendSummaryDto['status'];
  accent: string;
  note: string;
};

function getDisplayName(user: Pick<UserSummaryDto, 'username' | 'display_name'>) {
  return user.display_name?.trim() || user.username;
}

function matchesSearch(text: string, term: string) {
  return text.toLowerCase().includes(term);
}

export function FriendsList({
  currentUserId,
  refreshKey,
  search,
  onSearchChange,
  onOpenProfile,
  onDataMutated
}: FriendsListProps) {
  const [activeTab, setActiveTab] = useState<Tab>('friends');
  const [friends, setFriends] = useState<FriendSummaryDto[]>([]);
  const [requests, setRequests] = useState<FriendRequestListItemDto[]>([]);
  const [blocked, setBlocked] = useState<BlockListItemDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const [addFriendValue, setAddFriendValue] = useState('');
  const [addFriendError, setAddFriendError] = useState('');
  const [addFriendSuccess, setAddFriendSuccess] = useState('');
  const [isAddingFriend, setIsAddingFriend] = useState(false);
  const [busyIds, setBusyIds] = useState<Record<string, string>>({});

  useEffect(() => {
    if (!currentUserId) {
      setFriends([]);
      setRequests([]);
      setBlocked([]);
      return;
    }

    let active = true;
    setIsLoading(true);
    setError('');

    Promise.all([listFriends(currentUserId), listFriendRequests(), listBlockedUsers()])
      .then(([nextFriends, nextRequests, nextBlocked]) => {
        if (!active) {
          return;
        }

        setFriends(nextFriends);
        setRequests(nextRequests);
        setBlocked(nextBlocked);
      })
      .catch((err: unknown) => {
        if (!active) {
          return;
        }

        setError(err instanceof Error ? err.message : 'Unable to load friends.');
      })
      .finally(() => {
        if (active) {
          setIsLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, [currentUserId, refreshKey]);

  const filteredFriends = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) {
      return friends;
    }

    return friends.filter((friend) => {
      const haystacks = [friend.username, friend.display_name ?? '', friend.status];
      return haystacks.some((value) => matchesSearch(value, term));
    });
  }, [friends, search]);

  const filteredRequests = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) {
      return requests;
    }

    return requests.filter((request) => {
      const userName = getDisplayName(request.user);
      return matchesSearch(userName, term) || matchesSearch(request.user.username, term);
    });
  }, [requests, search]);

  const filteredBlocked = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) {
      return blocked;
    }

    return blocked.filter((entry) => matchesSearch(entry.username, term));
  }, [blocked, search]);

  async function handleAddFriend() {
    const query = addFriendValue.trim();
    if (query.length < 2) {
      setAddFriendError('Enter at least 2 characters.');
      return;
    }

    setAddFriendError('');
    setAddFriendSuccess('');
    setIsAddingFriend(true);

    try {
      const results = await searchUsers(query, 10);
      const exactMatch = results.find(
        (user) => user.username.toLowerCase() === query.toLowerCase()
      );
      const target = exactMatch ?? results[0];

      if (!target) {
        setAddFriendError('No matching user found.');
        return;
      }

      await createFriendRequest({ addressee_id: target.id });
      setAddFriendValue('');
      setAddFriendSuccess(`Request sent to ${getDisplayName(target)}.`);
      onDataMutated();
      if (currentUserId) {
        const [nextFriends, nextRequests, nextBlocked] = await Promise.all([
          listFriends(currentUserId),
          listFriendRequests(),
          listBlockedUsers()
        ]);
        setFriends(nextFriends);
        setRequests(nextRequests);
        setBlocked(nextBlocked);
      }
    } catch (err: unknown) {
      setAddFriendError(err instanceof Error ? err.message : 'Unable to add friend.');
    } finally {
      setIsAddingFriend(false);
    }
  }

  async function refreshLists() {
    if (!currentUserId) {
      return;
    }

    const [nextFriends, nextRequests, nextBlocked] = await Promise.all([
      listFriends(currentUserId),
      listFriendRequests(),
      listBlockedUsers()
    ]);
    setFriends(nextFriends);
    setRequests(nextRequests);
    setBlocked(nextBlocked);
    onDataMutated();
  }

  async function handleRequestAction(
    friendshipId: string,
    action: 'accept' | 'reject' | 'cancel' | 'block'
  ) {
    setBusyIds((current) => ({ ...current, [friendshipId]: action }));

    try {
      if (action === 'accept') {
        await updateFriendRequest(friendshipId, { status: 'accepted' });
      } else if (action === 'block') {
        await updateFriendRequest(friendshipId, { status: 'blocked' });
      } else {
        await deleteFriendRequest(friendshipId);
      }

      await refreshLists();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Unable to update request.');
    } finally {
      setBusyIds((current) => {
        const next = { ...current };
        delete next[friendshipId];
        return next;
      });
    }
  }

  async function handleBlockUser(userId: string) {
    setBusyIds((current) => ({ ...current, [userId]: 'block' }));

    try {
      await blockUser(userId);
      await refreshLists();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Unable to block user.');
    } finally {
      setBusyIds((current) => {
        const next = { ...current };
        delete next[userId];
        return next;
      });
    }
  }

  async function handleUnblockUser(userId: string) {
    setBusyIds((current) => ({ ...current, [userId]: 'unblock' }));

    try {
      await unblockUser(userId);
      await refreshLists();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Unable to unblock user.');
    } finally {
      setBusyIds((current) => {
        const next = { ...current };
        delete next[userId];
        return next;
      });
    }
  }

  const visibleCount =
    activeTab === 'friends'
      ? filteredFriends.length
      : activeTab === 'pending'
        ? filteredRequests.length
        : filteredBlocked.length;

  return (
    <div className="flex min-h-0 flex-1 flex-col">
      <div className="flex min-h-0 flex-1 flex-col">
        <div className="mt-4 flex rounded-md bg-panel p-1">
          <TabButton
            label="Friends"
            active={activeTab === 'friends'}
            onClick={() => setActiveTab('friends')}
          />
          <TabButton
            label="Pending"
            active={activeTab === 'pending'}
            onClick={() => setActiveTab('pending')}
          />
          <TabButton
            label="Blocked"
            active={activeTab === 'blocked'}
            onClick={() => setActiveTab('blocked')}
          />
        </div>

        <label className="mt-4 flex h-11 items-center gap-3 rounded-md bg-panel px-4 text-muted">
          <Search className="h-4 w-4 shrink-0" strokeWidth={1.75} />
          <input
            value={search}
            onChange={(event) => onSearchChange(event.target.value)}
            placeholder={`Search ${activeTab}`}
            className="mono-detail w-full min-w-0 bg-transparent text-xl text-white outline-none placeholder:text-muted"
          />
        </label>

        {error ? (
          <p className="mt-3 rounded-md border border-pink/25 bg-pink/10 px-3 py-2 text-sm text-pink">
            {error}
          </p>
        ) : null}

        <div className="min-h-0 flex-1 overflow-y-auto px-1 pb-4 pt-4 sm:px-3">
          {isLoading ? (
            <LoadingState label="Loading social graph" />
          ) : visibleCount === 0 ? (
            <EmptyState tab={activeTab} search={search} />
          ) : activeTab === 'friends' ? (
            <div className="space-y-1">
              {filteredFriends.map((friend) => (
                <FriendRow
                  key={friend.id}
                  title={getDisplayName(friend)}
                  subtitle={`${friend.username} • ${friend.status}`}
                  avatarLabel={friend.display_name ?? friend.username}
                  status={friend.status}
                  onClick={() => onOpenProfile(friend.id)}
                  actionLabel="Block"
                  actionDisabled={busyIds[friend.id] === 'block'}
                  onAction={() => handleBlockUser(friend.id)}
                />
              ))}
            </div>
          ) : activeTab === 'pending' ? (
            <div className="space-y-1">
              {filteredRequests.map((request) => {
                const userLabel = getDisplayName(request.user);
                const isIncoming = request.direction === 'incoming';
                const primaryLabel = isIncoming ? 'Accept' : 'Cancel';
                const secondaryLabel = isIncoming ? 'Decline' : undefined;

                return (
                  <div
                    key={request.friendship_id}
                    className="flex min-h-[4.25rem] items-center gap-3 rounded-lg px-3 text-left text-grey-link transition hover:bg-frame/60 hover:text-white"
                  >
                    <button
                      type="button"
                      onClick={() => onOpenProfile(request.user.id, request.friendship_id)}
                      className="flex min-w-0 flex-1 items-center gap-3 text-left"
                    >
                      <Avatar name={userLabel} />
                      <span className="min-w-0 flex-1">
                        <span className="block truncate text-[1rem] font-bold text-white">
                          {userLabel}
                        </span>
                        <span className="mt-0.5 block truncate text-sm text-white/35">
                          {isIncoming ? 'Incoming request' : 'Outgoing request'} •{' '}
                          {request.user.username}
                        </span>
                      </span>
                    </button>
                    <div className="flex shrink-0 items-center gap-2">
                      {secondaryLabel ? (
                        <button
                          type="button"
                          disabled={busyIds[request.friendship_id] != null}
                          onClick={() => handleRequestAction(request.friendship_id, 'reject')}
                          className="rounded-md border border-white/10 bg-frame px-3 py-2 text-xs font-semibold text-white/70 transition hover:text-white disabled:opacity-40"
                        >
                          {secondaryLabel}
                        </button>
                      ) : null}
                      <button
                        type="button"
                        disabled={busyIds[request.friendship_id] != null}
                        onClick={() =>
                          handleRequestAction(
                            request.friendship_id,
                            isIncoming ? 'accept' : 'cancel'
                          )
                        }
                        className="rounded-md bg-aqua px-3 py-2 text-xs font-bold text-primary-bg transition hover:bg-white disabled:opacity-40"
                      >
                        {busyIds[request.friendship_id] === 'accept' ||
                        busyIds[request.friendship_id] === 'cancel'
                          ? '...'
                          : primaryLabel}
                      </button>
                      <button
                        type="button"
                        disabled={busyIds[request.friendship_id] != null}
                        onClick={() => handleRequestAction(request.friendship_id, 'block')}
                        className="rounded-md border border-pink/25 bg-pink/10 px-3 py-2 text-xs font-semibold text-pink transition hover:border-pink/45 disabled:opacity-40"
                      >
                        Block
                      </button>
                    </div>
                  </div>
                );
              })}
            </div>
          ) : (
            <div className="space-y-1">
              {filteredBlocked.map((entry) => (
                <div
                  key={entry.id}
                  className="flex min-h-[4.25rem] items-center gap-3 rounded-lg px-3 text-left text-grey-link transition hover:bg-frame/60 hover:text-white"
                >
                  <button
                    type="button"
                    onClick={() => onOpenProfile(entry.id)}
                    className="flex min-w-0 flex-1 items-center gap-3 text-left"
                  >
                    <Avatar name={entry.username} />
                    <span className="min-w-0 flex-1">
                      <span className="block truncate text-[1rem] font-bold text-white">
                        {entry.username}
                      </span>
                      <span className="mt-0.5 block truncate text-sm text-white/35">
                        Blocked on {new Date(entry.blocked_at).toLocaleDateString('fr-FR')}
                      </span>
                    </span>
                  </button>
                  <button
                    type="button"
                    disabled={busyIds[entry.id] != null}
                    onClick={() => handleUnblockUser(entry.id)}
                    className="rounded-md border border-white/10 bg-frame px-3 py-2 text-xs font-semibold text-white/70 transition hover:text-white disabled:opacity-40"
                  >
                    Unblock
                  </button>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      <div className="shrink-0 border-t border-white/8 px-4 py-4">
        <div className="flex h-11 items-center gap-2 rounded-md bg-panel px-3 text-muted">
          <input
            value={addFriendValue}
            onChange={(event) => setAddFriendValue(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === 'Enter') {
                event.preventDefault();
                void handleAddFriend();
              }
            }}
            placeholder="Add friend by username"
            className="min-w-0 flex-1 bg-transparent text-sm text-white outline-none placeholder:text-muted"
          />
          <button
            type="button"
            disabled={isAddingFriend || addFriendValue.trim().length < 2}
            onClick={() => void handleAddFriend()}
            className="flex h-8 w-8 shrink-0 items-center justify-center rounded-md bg-aqua text-primary-bg transition hover:bg-white disabled:cursor-not-allowed disabled:bg-frame disabled:text-white/25"
            aria-label="Add friend"
          >
            {isAddingFriend ? (
              <LoaderCircle className="h-4 w-4 animate-spin" strokeWidth={2} />
            ) : (
              <UserPlus className="h-4 w-4" strokeWidth={2} />
            )}
          </button>
        </div>
        {addFriendError ? (
          <p className="mt-2 text-xs text-pink">{addFriendError}</p>
        ) : addFriendSuccess ? (
          <p className="mt-2 text-xs text-lime">{addFriendSuccess}</p>
        ) : (
          <p className="mt-2 text-xs text-white/35">
            Search uses the User Service, then sends the request to the resolved user id.
          </p>
        )}
      </div>
    </div>
  );
}

type TabButtonProps = {
  label: string;
  active: boolean;
  onClick: () => void;
};

function TabButton({ label, active, onClick }: TabButtonProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`flex h-9 flex-1 items-center justify-center rounded-md text-sm font-bold transition ${
        active ? 'bg-frame text-white' : 'text-white/40 hover:text-white'
      }`}
    >
      {label}
    </button>
  );
}

function Avatar({ name }: { name: string }) {
  return (
    <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-[linear-gradient(135deg,#78dce8,#ab9df2,#ff6188)] text-sm font-bold text-primary-bg">
      {name.slice(0, 1).toUpperCase()}
    </span>
  );
}

function FriendRow({
  title,
  subtitle,
  avatarLabel,
  status,
  onClick,
  actionLabel,
  actionDisabled,
  onAction
}: {
  title: string;
  subtitle: string;
  avatarLabel: string;
  status: FriendSummaryDto['status'];
  onClick: () => void;
  actionLabel: string;
  actionDisabled?: boolean;
  onAction: () => void;
}) {
  const statusClass =
    status === 'online' ? 'bg-lime' : status === 'idle' ? 'bg-yellow' : 'bg-muted';

  return (
    <div className="flex min-h-[4.25rem] items-center gap-3 rounded-lg px-3 text-left text-grey-link transition hover:bg-frame/60 hover:text-white">
      <button
        type="button"
        onClick={onClick}
        className="flex min-w-0 flex-1 items-center gap-3 text-left"
      >
        <span className="relative shrink-0">
          <Avatar name={avatarLabel} />
          <span
            className={`absolute -bottom-0.5 -right-0.5 h-3.5 w-3.5 rounded-full border-2 border-secondary-bg ${statusClass}`}
          />
        </span>
        <span className="min-w-0 flex-1">
          <span className="block truncate text-[1rem] font-bold text-white">{title}</span>
          <span className="mt-0.5 block truncate text-sm text-white/35">{subtitle}</span>
        </span>
      </button>
      <button
        type="button"
        disabled={actionDisabled}
        onClick={onAction}
        className="rounded-md border border-white/10 bg-frame px-3 py-2 text-xs font-semibold text-white/70 transition hover:text-white disabled:opacity-40"
      >
        {actionLabel}
      </button>
    </div>
  );
}

function EmptyState({ tab, search }: { tab: Tab; search: string }) {
  const label =
    tab === 'friends'
      ? 'No friends found'
      : tab === 'pending'
        ? 'No pending requests'
        : 'No blocked users';
  const description =
    search.trim().length > 0
      ? 'Try another search term.'
      : tab === 'friends'
        ? 'Add someone below to start a friendship.'
        : tab === 'pending'
          ? 'Incoming and outgoing requests will appear here.'
          : 'Blocked users will appear here.';

  return (
    <div className="flex h-full min-h-[16rem] flex-col items-center justify-center px-5 text-center">
      <div className="flex h-14 w-14 items-center justify-center rounded-full bg-panel text-[#8b8b8f]">
        <UserRound className="h-6 w-6" strokeWidth={1.8} />
      </div>
      <p className="mt-4 text-[1rem] font-bold text-white">{label}</p>
      <p className="mt-1 max-w-[16rem] text-sm leading-5 text-white/35">{description}</p>
    </div>
  );
}

function LoadingState({ label }: { label: string }) {
  return (
    <div className="flex h-full min-h-[16rem] flex-col items-center justify-center px-5 text-center">
      <LoaderCircle className="h-7 w-7 animate-spin text-aqua" strokeWidth={1.9} />
      <p className="mt-4 text-[1rem] font-bold text-white">{label}</p>
    </div>
  );
}
