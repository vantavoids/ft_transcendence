'use client';

import { useEffect, useLayoutEffect, useRef, useState } from 'react';
import { Bell, BellOff, ChevronRight, Copy, Link2, Lock as LockIcon, LogOut } from 'lucide-react';
import { ActionModal } from '../action-modal';
import { useCloseOnEscape } from '../../shared/hooks/use-close-on-escape';
import { useCurrentUserProfile } from '../../shared/user/user-store';
import { useToast } from '../../shared/ui/toast';
import { useNotificationPrefs } from '../../shared/lib/notification-prefs-store';
import { muteDurations, muteDurationToIso, type MuteDuration } from '../../shared/lib/mute-durations';
import { createGuildInvite, leaveGuild } from '../../shared/api/guild';

// invites can only expire on whole-hour boundaries (the guild service takes an
// int? of hours), so the "fast" invite settles on the shortest sensible window.
const FAST_INVITE_EXPIRES_IN_HOURS = 1;

// keep the menu off the viewport edges; a rough width/height is enough since we
// only need to avoid clipping, not pixel-perfect placement.
const MENU_WIDTH_PX = 224;
const SUBMENU_WIDTH_PX = 160;
const MENU_MARGIN_PX = 8;

// a guild target carries owner/name for the invite + leave actions; a channel
// target only ever offers mute + copy-id.
export type GuildContextMenuTarget =
  | {
      scope: 'guild';
      guildId: string;
      guildName: string;
      ownerId: string;
      x: number;
      y: number;
    }
  | {
      scope: 'channel';
      channelId: string;
      channelName: string;
      // present only when the viewer may manage the channel; renders the
      // "Channel permissions" entry
      onOpenPermissions?: () => void;
      x: number;
      y: number;
    };

type GuildContextMenuProps = {
  target: GuildContextMenuTarget;
  onClose: () => void;
  /** Only fired for guild targets, when the user leaves the guild. */
  onLeft?: (guildId: string) => void;
};

export function GuildContextMenu({ target, onClose, onLeft }: GuildContextMenuProps) {
  const menuRef = useRef<HTMLDivElement>(null);
  const { pushToast } = useToast();
  const { currentUser } = useCurrentUserProfile();
  const currentUserId = currentUser?.id ?? null;
  const { isMuted, mute, unmute } = useNotificationPrefs();

  const [isInviting, setIsInviting] = useState(false);
  const [isTogglingMute, setIsTogglingMute] = useState(false);
  const [isMuteSubmenuOpen, setIsMuteSubmenuOpen] = useState(false);
  const [isConfirmingLeave, setIsConfirmingLeave] = useState(false);
  const [isLeaving, setIsLeaving] = useState(false);
  // measured after mount so the vertical clamp can reserve the real menu height;
  // 0 until then, which just means the first frame uses the raw cursor y
  const [menuHeight, setMenuHeight] = useState(0);

  const scopeType = target.scope;
  const scopeId = target.scope === 'guild' ? target.guildId : target.channelId;
  const scopeName = target.scope === 'guild' ? target.guildName : target.channelName;
  const muted = isMuted(scopeType, scopeId);
  const isOwner =
    target.scope === 'guild' && currentUserId != null && currentUserId === target.ownerId;

  // measure the rendered menu so the vertical clamp knows its real height
  useLayoutEffect(() => {
    if (menuRef.current) {
      setMenuHeight(menuRef.current.offsetHeight);
    }
  }, [muted, target.scope]);

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
  useEffect(
    () => () => {
      if (closeSubmenuTimer.current) {
        clearTimeout(closeSubmenuTimer.current);
      }
    },
    []
  );

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

  async function handleFastInvite() {
    if (target.scope !== 'guild') {
      return;
    }
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

  async function handleCopyId() {
    try {
      await navigator.clipboard.writeText(scopeId);
      pushToast({
        title: 'Copied',
        description: `${scopeName} ID copied to the clipboard.`,
        tone: 'success'
      });
      onClose();
    } catch {
      pushToast({
        title: 'Copy failed',
        description: 'Could not access the clipboard.',
        tone: 'error'
      });
    }
  }

  async function handleMuteFor(duration: MuteDuration) {
    setIsTogglingMute(true);
    try {
      await mute(scopeType, scopeId, muteDurationToIso(duration));
      const chosen = muteDurations.find((option) => option.value === duration);
      pushToast({
        title: scopeType === 'guild' ? 'Server muted' : 'Channel muted',
        description:
          duration === 'forever'
            ? `You won't get notifications from ${scopeName}.`
            : `${scopeName} muted for ${chosen?.longLabel ?? duration}.`,
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
      await unmute(scopeType, scopeId);
      pushToast({
        title: scopeType === 'guild' ? 'Server unmuted' : 'Channel unmuted',
        description: `Notifications from ${scopeName} are back on.`,
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
    if (target.scope !== 'guild') {
      return;
    }
    setIsLeaving(true);
    try {
      await leaveGuild(target.guildId);
      onLeft?.(target.guildId);
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
  const top = Math.min(target.y, window.innerHeight - menuHeight - MENU_MARGIN_PX);
  const menuLeft = Math.max(MENU_MARGIN_PX, left);
  // flip the submenu to the left of the menu when it would overflow the viewport
  const submenuOnLeft =
    menuLeft + MENU_WIDTH_PX + SUBMENU_WIDTH_PX + MENU_MARGIN_PX > window.innerWidth;

  return (
    <>
      <div
        ref={menuRef}
        role="menu"
        aria-label={`${scopeName} actions`}
        className="fixed z-[60] w-56 rounded-lg border border-stroke bg-panel py-1.5 text-sm shadow-2xl shadow-black/50"
        style={{ left: menuLeft, top: Math.max(MENU_MARGIN_PX, top) }}
      >
        <p className="truncate px-3 pb-1.5 pt-1 text-xs font-semibold uppercase tracking-wide text-white/35">
          {scopeName}
        </p>

        {target.scope === 'guild' ? (
          <MenuItem
            icon={Link2}
            label="Fast invite"
            hint="Copy a 1h link"
            disabled={isInviting}
            onClick={() => void handleFastInvite()}
          />
        ) : null}

        {muted ? (
          <MenuItem
            icon={Bell}
            label="Unmute notifications"
            disabled={isTogglingMute}
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
              disabled={isTogglingMute}
              trailing={<ChevronRight className="h-4 w-4 text-white/40" strokeWidth={1.9} />}
              onClick={openMuteSubmenu}
            />
            {isMuteSubmenuOpen ? (
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

        <MenuItem
          icon={Copy}
          label={target.scope === 'guild' ? 'Copy server ID' : 'Copy channel ID'}
          onClick={() => void handleCopyId()}
        />

        {target.scope === 'channel' && target.onOpenPermissions ? (
          <>
            <div className="my-1 border-t border-stroke" />
            <MenuItem
              icon={LockIcon}
              label="Channel permissions"
              onClick={() => {
                target.onOpenPermissions?.();
                onClose();
              }}
            />
          </>
        ) : null}

        {target.scope === 'guild' ? (
          <>
            <div className="my-1 border-t border-stroke" />
            <MenuItem
              icon={LogOut}
              label="Leave server"
              destructive
              disabled={isOwner}
              hint={isOwner ? 'Owner' : undefined}
              onClick={() => setIsConfirmingLeave(true)}
            />
          </>
        ) : null}
      </div>

      {isConfirmingLeave && target.scope === 'guild' ? (
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
