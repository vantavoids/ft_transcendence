import { ArrowLeft, CircleEllipsis, Phone, UserRound, Video } from 'lucide-react';
import { AvatarWithStatus } from '../avatar-with-status';
import type { DirectMessage } from '../dm-list';

type ConversationHeaderProps = {
  chatMode: 'guild' | 'dm';
  activeDmDetails: DirectMessage | null;
  activeConversationName: string;
  isSidePanelOpen: boolean;
  isSidePanelToggleDisabled: boolean;
  sidePanelAriaLabel: string;
  onShowMobileSidebar: () => void;
  onToggleSidePanel: () => void;
  onStartAudioCall: () => void;
  onStartVideoCall: () => void;
};

export function ConversationHeader({
  chatMode,
  activeDmDetails,
  activeConversationName,
  isSidePanelOpen,
  isSidePanelToggleDisabled,
  sidePanelAriaLabel,
  onShowMobileSidebar,
  onToggleSidePanel,
  onStartAudioCall,
  onStartVideoCall
}: ConversationHeaderProps) {
  return (
    <div className="flex h-[4.9rem] shrink-0 items-center justify-between border-b border-white/8 px-5 sm:px-7">
      <div className="flex items-center gap-3">
        <button
          type="button"
          onClick={onShowMobileSidebar}
          className="flex h-11 w-11 items-center justify-center rounded-xl border border-frame text-[#7e7e82] md:hidden"
          aria-label={chatMode === 'dm' ? 'Show DMs' : 'Show channels'}
        >
          <ArrowLeft className="h-5 w-5" strokeWidth={1.9} />
        </button>
        {chatMode === 'dm' && activeDmDetails ? (
          <div className="flex min-w-0 items-center gap-3">
            <AvatarWithStatus
              name={activeDmDetails.name}
              accent={activeDmDetails.accent}
              status={activeDmDetails.status}
              avatarUrl={activeDmDetails.avatarUrl}
            />
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
              onClick={onStartAudioCall}
              className="transition hover:text-white"
              aria-label="Start voice call"
            >
              <Phone className="h-5 w-5" strokeWidth={1.8} />
            </button>
            <button
              type="button"
              onClick={onStartVideoCall}
              className="transition hover:text-white"
              aria-label="Start video call"
            >
              <Video className="h-5 w-5" strokeWidth={1.8} />
            </button>
          </>
        ) : null}
        <button
          type="button"
          onClick={onToggleSidePanel}
          className={`transition hover:text-white ${
            isSidePanelOpen ? 'text-aqua' : 'text-[#8c8c90]'
          } ${isSidePanelToggleDisabled ? 'cursor-not-allowed opacity-45' : ''}`}
          disabled={isSidePanelToggleDisabled}
          aria-label={sidePanelAriaLabel}
          aria-pressed={isSidePanelOpen}
        >
          <UserRound className="h-5 w-5" strokeWidth={1.8} />
        </button>
        <CircleEllipsis className="h-5 w-5" strokeWidth={1.8} />
      </div>
    </div>
  );
}
