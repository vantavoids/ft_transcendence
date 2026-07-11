'use client';

import { MessageCircle, Shield, Trophy, X } from 'lucide-react';
import { AvatarWithStatus } from './avatar-with-status';
import type { GuildMember } from './guild-member-list';
import { useCloseOnEscape } from '../shared/hooks/use-close-on-escape';

type ProfileCardProps = {
  member: GuildMember;
  variant?: 'modal' | 'side';
  onClose: () => void;
};

export function ProfileCard({ member, variant = 'modal', onClose }: ProfileCardProps) {
  useCloseOnEscape(onClose);

  const card = (
    <section
      className={`relative w-full overflow-hidden rounded-[1rem] bg-secondary-bg shadow-2xl shadow-black/50 ring-1 ring-white/10 ${
        variant === 'modal' ? 'max-w-[23rem]' : 'min-h-0 max-w-none'
      }`}
    >
      <div
        className="h-24 bg-[linear-gradient(135deg,#1a1a1c_0%,#27333a_46%,#78dce8_100%)]"
        style={
          member.bannerUrl
            ? {
                backgroundImage: `linear-gradient(135deg, rgba(26,26,28,0.72), rgba(39,51,58,0.48)), url(${member.bannerUrl})`,
                backgroundSize: 'cover',
                backgroundPosition: 'center'
              }
            : undefined
        }
      />
      <button
        type="button"
        onClick={onClose}
        className="absolute right-4 top-4 flex h-9 w-9 items-center justify-center rounded-md bg-black/35 text-white/70 transition hover:bg-black/55 hover:text-white"
        aria-label="Close profile"
      >
        <X className="h-4 w-4" strokeWidth={2} />
      </button>

      <div className="px-5 pb-5">
        <div className="-mt-10 flex items-end justify-between gap-4">
          <AvatarWithStatus
            size="lg"
            name={member.name}
            accent={member.accent}
            status={member.status}
            avatarUrl={member.avatarUrl ?? undefined}
          />
          <span className="font-category mb-2 rounded-full border border-white/10 bg-panel px-3 py-1 text-[0.68rem] uppercase tracking-[0.14em] text-white/45">
            {member.role}
          </span>
        </div>

        <div className="mt-4">
          <h2 className="truncate text-[1.6rem] font-bold tracking-[-0.05em] text-white">
            {member.name}
          </h2>
          <p className="mt-1 text-sm text-white/45">{member.activity}</p>
        </div>

        <div className="mt-4 rounded-md border border-white/8 bg-panel px-3 py-3">
          <p className="font-category text-[0.68rem] uppercase tracking-[0.14em] text-white/35">
            Bio
          </p>
          <p className="mt-2 text-sm leading-6 text-white/70">
            {member.bio ? member.bio : 'No bio set.'}
          </p>
        </div>

        <div className="mt-5 grid grid-cols-2 gap-3">
          <div className="rounded-md border border-white/8 bg-panel px-3 py-3">
            <div className="flex items-center gap-2 text-aqua">
              <Shield className="h-4 w-4" strokeWidth={1.8} />
              <span className="font-category text-[0.68rem] uppercase tracking-[0.14em]">
                Status
              </span>
            </div>
            <p className="mt-2 truncate text-sm font-semibold capitalize text-white">
              {member.status}
            </p>
          </div>
          <div className="rounded-md border border-white/8 bg-panel px-3 py-3">
            <div className="flex items-center gap-2 text-yellow">
              <Trophy className="h-4 w-4" strokeWidth={1.8} />
              <span className="font-category text-[0.68rem] uppercase tracking-[0.14em]">
                Guild
              </span>
            </div>
            <p className="mt-2 truncate text-sm font-semibold text-white">Guild name</p>
          </div>
        </div>

        <button
          type="button"
          className="mt-5 flex h-11 w-full items-center justify-center gap-2 rounded-md bg-aqua text-sm font-bold text-primary-bg transition hover:bg-white"
        >
          <MessageCircle className="h-4 w-4" strokeWidth={2} />
          Send message
        </button>
      </div>
    </section>
  );

  if (variant === 'side') {
    return (
      <aside className="hidden min-h-0 w-[20rem] shrink-0 overflow-hidden xl:flex">{card}</aside>
    );
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/55 px-4 py-6">
      <button
        type="button"
        className="absolute inset-0 cursor-default"
        onClick={onClose}
        aria-label="Close profile"
      />
      {card}
    </div>
  );
}
