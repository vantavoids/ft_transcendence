import { Bell, Headphones, Mic, MicOff, Settings } from 'lucide-react';
import { AvatarWithStatus } from './avatar-with-status';
import { toSidebarStatus, type CurrentUserProfile } from '../shared/mappers/user';

type ConversationListFooterProps = {
  currentUser: CurrentUserProfile | null;
  isMicMuted: boolean;
  isDeafened: boolean;
  unreadNotifications: number;
  onToggleMicMute: () => void;
  onToggleDeafen: () => void;
  onOpenNotifications: () => void;
  onOpenSettings: () => void;
};

export function ConversationListFooter({
  currentUser,
  isMicMuted,
  isDeafened,
  unreadNotifications,
  onToggleMicMute,
  onToggleDeafen,
  onOpenNotifications,
  onOpenSettings
}: ConversationListFooterProps) {
  return (
    <div className="shrink-0 border-t border-stroke px-4 py-4">
      <div className="flex items-center justify-between gap-3">
        <div className="flex min-w-0 items-center gap-3">
          <AvatarWithStatus
            size="sm"
            name={currentUser?.displayName ?? currentUser?.username ?? 'Guest'}
            accent="aqua"
            status={currentUser ? toSidebarStatus(currentUser.status) : 'offline'}
            avatarUrl={currentUser?.avatarUrl}
          />
          <span className="min-w-0">
            <span className="block truncate text-[1rem] font-bold text-white">
              {currentUser?.displayName ?? currentUser?.username ?? 'Guest'}
            </span>
            <span className="mono-detail block truncate text-xs text-white/35">
              {currentUser?.username ? `@${currentUser.username}` : 'Loading profile'}
            </span>
          </span>
        </div>
        <div className="flex shrink-0 items-center gap-3">
          <button
            type="button"
            onClick={onToggleMicMute}
            className={`transition ${
              isMicMuted ? 'text-pink hover:text-[#ff8aa7]' : 'text-[#8b8b8f] hover:text-white'
            }`}
            aria-label={isMicMuted ? 'Unmute microphone' : 'Mute microphone'}
            aria-pressed={isMicMuted}
          >
            {isMicMuted ? (
              <MicOff className="h-6 w-6" strokeWidth={1.8} />
            ) : (
              <Mic className="h-6 w-6" strokeWidth={1.8} />
            )}
          </button>
          <button
            type="button"
            onClick={onToggleDeafen}
            className={`transition ${
              isDeafened ? 'text-pink hover:text-[#ff8aa7]' : 'text-[#8b8b8f] hover:text-white'
            }`}
            aria-label={isDeafened ? 'Undeafen audio' : 'Deafen audio'}
            aria-pressed={isDeafened}
          >
            <Headphones className="h-6 w-6" strokeWidth={1.8} />
          </button>
          <button
            type="button"
            onClick={onOpenNotifications}
            className="relative text-[#8b8b8f] transition hover:text-white"
            aria-label="Show notifications"
          >
            <Bell className="h-6 w-6" strokeWidth={1.8} />
            {unreadNotifications > 0 ? (
              <span className="absolute -right-1.5 -top-1.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-pink px-1 text-[0.6rem] font-bold leading-none text-primary-bg">
                {unreadNotifications > 99 ? '99+' : unreadNotifications}
              </span>
            ) : null}
          </button>
          <button
            type="button"
            onClick={onOpenSettings}
            className="text-[#8b8b8f] transition hover:text-white"
            aria-label="Open settings"
          >
            <Settings className="h-6 w-6" strokeWidth={1.8} />
          </button>
        </div>
      </div>
    </div>
  );
}
