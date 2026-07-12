'use client';

import { useEffect, useRef, useState } from 'react';
import { Bell, BellOff, ChevronRight, Link2, LogOut } from 'lucide-react';
import { ActionModal } from '../action-modal';
import { useCloseOnEscape } from '../../shared/hooks/use-close-on-escape';
import { useCurrentUserId } from '../../shared/hooks/use-current-user-id';
import { useToast } from '../../shared/ui/toast';
import { muteDurations, muteDurationToIso, type MuteDuration } from '../../shared/lib/mute-durations';
import { createGuildInvite, leaveGuild } from '../../shared/api/guild';
import {
  deleteNotificationPreference,
  listNotificationPreferences,
  setNotificationPreference
} from '../../shared/api/notification';

// invites can only expire on whole-hour boundaries (the guild service takes an
// int? of hours), so the "fast" invite settles on the shortest sensible window.
const FAST_INVITE_EXPIRES_IN_HOURS = 1;

// keep the menu off the viewport edges; a rough width/height is enough since we
// only need to avoid clipping, not pixel-perfect placement.
const MENU_WIDTH_PX = 224;
const SUBMENU_WIDTH_PX = 160;
const MENU_MARGIN_PX = 8;

export type GuildContextMenuTarget = {
  guildId: string;
  guildName: string;
  ownerId: string;
  x: number;
  y: number;
};

type GuildContextMenuProps = {
  target: GuildContextMenuTarget;
  onClose: () => void;
  onLeft: (guildId: string) => void;
};

// null while the mute preference is still loading; false/true once known.
type MuteState = { muted: boolean } | null;

export function GuildContextMenu({ target, onClose, onLeft }: GuildContextMenuProps) {
  const menuRef = useRef<HTMLDivElement>(null);
  const { pushToast } = useToast();
  const currentUserId = useCurrentUserId();
  const [muteState, setMuteState] = useState<MuteState>(null);
  const [isInviting, setIsInviting] = useState(false);
  const [isTogglingMute, setIsTogglingMute] = useState(false);
  const [isMuteSubmenuOpen, setIsMuteSubmenuOpen] = useState(false);
  const [isConfirmingLeave, setIsConfirmingLeave] = useState(false);
  const [isLeaving, setIsLeaving] = useState(false);

  const isOwner = currentUserId != null && currentUserId === target.ownerId;

  // a short grace period before closing the submenu tolerates the diagonal
  // mouse path from the parent item, which briefly leaves both elements
  const closeSubmenuTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  function openMuteSubmenu() {
    if (closeSubmenuTimer.current) {
      clearTimeout(closeSubmenuTimer.current);
      closeSubmenuTimer.current = null;
    }
    setIsMuteSubmenuOpen(true);
  }
  function scheduleCloseMuteSubmenu() {
    closeSubmenuTimer.current = setTimeout(() => setIsMuteSubmenuOpen(false), 220);
  }
  useEffect(() => () => {
    if (closeSubmenuTimer.current) {
      clearTimeout(closeSubmenuTimer.current);
    }
  }, []);

  useCloseOnEscape(onClose);

  // close when clicking anywhere outside the menu. the leave-confirmation modal
  // renders its own overlay, so suspend this while it is open to avoid the menu
  // swallowing that click and tearing itself down mid-action.
  useEffect(() => {
    if (isConfirmingLeave) {
      return;
    }

    function handlePointerDown(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        onClose();
      }
    }

    window.addEventListener('mousedown', handlePointerDown);
    return () => window.removeEventListener('mousedown', handlePointerDown);
  }, [isConfirmingLeave, onClose]);

  // load the current mute state so the entry shows the right label. a failure
  // here just leaves the entry disabled rather than blocking the whole menu.
  useEffect(() => {
    let cancelled = false;

    listNotificationPreferences()
      .then((preferences) => {
        if (cancelled) {
          return;
        }
        const pref = preferences.find(
          (candidate) => candidate.scope_type === 'guild' && candidate.scope_id === target.guildId
        );
        setMuteState({ muted: pref?.muted ?? false });
      })
      .catch(() => {
        // best effort: leave muteState null so the entry renders disabled
      });

    return () => {
      cancelled = true;
    };
  }, [target.guildId]);

  async function handleFastInvite() {
    setIsInviting(true);
    try {
      const invite = await createGuildInvite(target.guildId, {
        expires_in_hours: FAST_INVITE_EXPIRES_IN_HOURS
      });
      await navigator.clipboard.writeText(invite.code);
      pushToast({
        title: 'Invite copied',
        description: `Code ${invite.code} copied — expires in 1 hour.`,
        tone: 'success'
      });
      onClose();
    } catch (error) {
      pushToast({
        title: 'Fast invite',
        description: error instanceof Error ? error.message : 'Could not create an invite.',
        tone: 'error'
      });
    } finally {
      setIsInviting(false);
    }
  }

  async function handleMuteFor(duration: MuteDuration) {
    setIsTogglingMute(true);
    try {
      await setNotificationPreference('guild', target.guildId, {
        muted: true,
        muted_until: muteDurationToIso(duration)
      });
      const chosen = muteDurations.find((option) => option.value === duration);
      pushToast({
        title: 'Server muted',
        description:
          duration === 'forever'
            ? `You won't get notifications from ${target.guildName}.`
            : `${target.guildName} muted for ${chosen?.longLabel ?? duration}.`,
        tone: 'success'
      });
      onClose();
    } catch (error) {
      pushToast({
        title: 'Notifications',
        description:
          error instanceof Error ? error.message : 'Could not update notification settings.',
        tone: 'error'
      });
    } finally {
      setIsTogglingMute(false);
    }
  }

  async function handleUnmute() {
    setIsTogglingMute(true);
    try {
      await deleteNotificationPreference('guild', target.guildId);
      pushToast({
        title: 'Server unmuted',
        description: `Notifications from ${target.guildName} are back on.`,
        tone: 'success'
      });
      onClose();
    } catch (error) {
      pushToast({
        title: 'Notifications',
        description:
          error instanceof Error ? error.message : 'Could not update notification settings.',
        tone: 'error'
      });
    } finally {
      setIsTogglingMute(false);
    }
  }

  async function confirmLeave() {
    setIsLeaving(true);
    try {
      await leaveGuild(target.guildId);
      onLeft(target.guildId);
      pushToast({
        title: 'Left server',
        description: `You left ${target.guildName}.`,
        tone: 'success'
      });
      onClose();
    } catch (error) {
      pushToast({
        title: 'Leave server',
        description: error instanceof Error ? error.message : 'Could not leave the server.',
        tone: 'error'
      });
      setIsLeaving(false);
      setIsConfirmingLeave(false);
    }
  }

  const left = Math.min(target.x, window.innerWidth - MENU_WIDTH_PX - MENU_MARGIN_PX);
  const top = Math.min(target.y, window.innerHeight - MENU_MARGIN_PX);
  const menuLeft = Math.max(MENU_MARGIN_PX, left);
  // flip the submenu to the left of the menu when it would overflow the viewport
  const submenuOnLeft = menuLeft + MENU_WIDTH_PX + SUBMENU_WIDTH_PX + MENU_MARGIN_PX > window.innerWidth;

  const isMuted = muteState?.muted ?? false;

  return (
    <>
      <div
        ref={menuRef}
        role="menu"
        aria-label={`${target.guildName} actions`}
        className="fixed z-[60] w-56 rounded-lg border border-stroke bg-panel py-1.5 text-sm shadow-2xl shadow-black/50"
        style={{ left: menuLeft, top: Math.max(MENU_MARGIN_PX, top) }}
      >
        <p className="truncate px-3 pb-1.5 pt-1 text-xs font-semibold uppercase tracking-wide text-white/35">
          {target.guildName}
        </p>
        <MenuItem
          icon={Link2}
          label="Fast invite"
          hint="Copy a 1h link"
          disabled={isInviting}
          onClick={() => void handleFastInvite()}
        />

        {isMuted ? (
          <MenuItem
            icon={Bell}
            label="Unmute notifications"
            disabled={muteState === null || isTogglingMute}
            onClick={() => void handleUnmute()}
          />
        ) : (
          <div
            className="relative"
            onMouseEnter={openMuteSubmenu}
            onMouseLeave={scheduleCloseMuteSubmenu}
          >
            <MenuItem
              icon={BellOff}
              label="Mute notifications"
              disabled={muteState === null || isTogglingMute}
              trailing={<ChevronRight className="h-4 w-4 text-white/40" strokeWidth={1.9} />}
              onClick={openMuteSubmenu}
            />
            {isMuteSubmenuOpen && muteState !== null ? (
              // overlap the parent by 1px (no visual gap) so the diagonal path to
              // the submenu never crosses dead space and dismisses it
              <div
                role="menu"
                aria-label="Mute duration"
                className={`absolute top-0 w-40 rounded-lg border border-stroke bg-panel py-1.5 shadow-2xl shadow-black/50 ${
                  submenuOnLeft ? 'right-full -mr-px' : 'left-full -ml-px'
                }`}
              >
                {muteDurations.map((option) => (
                  <MenuItem
                    key={option.value}
                    label={option.longLabel}
                    disabled={isTogglingMute}
                    onClick={() => void handleMuteFor(option.value)}
                  />
                ))}
              </div>
            ) : null}
          </div>
        )}

        <div className="my-1 border-t border-stroke" />
        <MenuItem
          icon={LogOut}
          label="Leave server"
          destructive
          disabled={isOwner}
          hint={isOwner ? 'Owner' : undefined}
          onClick={() => setIsConfirmingLeave(true)}
        />
      </div>

      {isConfirmingLeave ? (
        <ActionModal
          title={`Leave ${target.guildName}?`}
          description="You'll lose access to its channels until someone invites you back."
          confirmLabel="Leave server"
          destructive
          isBusy={isLeaving}
          onClose={() => setIsConfirmingLeave(false)}
          onConfirm={confirmLeave}
        />
      ) : null}
    </>
  );
}

function MenuItem({
  icon: Icon,
  label,
  hint,
  trailing,
  destructive = false,
  disabled = false,
  onClick
}: {
  icon?: typeof Link2;
  label: string;
  hint?: string;
  trailing?: React.ReactNode;
  destructive?: boolean;
  disabled?: boolean;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      role="menuitem"
      disabled={disabled}
      onClick={onClick}
      className={`flex w-full items-center gap-3 px-3 py-2 text-left font-medium transition disabled:cursor-not-allowed disabled:opacity-40 ${
        destructive ? 'text-pink hover:bg-pink/10' : 'text-white/80 hover:bg-frame hover:text-white'
      }`}
    >
      {Icon ? <Icon className="h-4 w-4 shrink-0" strokeWidth={1.9} /> : null}
      <span className="flex-1 truncate">{label}</span>
      {hint ? <span className="text-xs text-white/35">{hint}</span> : null}
      {trailing}
    </button>
  );
}
