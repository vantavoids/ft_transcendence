'use client';

import { MessageCircle, Shield, Trophy, UserMinus, X } from 'lucide-react';
import { AvatarWithStatus } from './avatar-with-status';
import type { GuildMember } from './guild-member-list';
import { useCloseOnEscape } from '../shared/hooks/use-close-on-escape';

type ProfileCardProps = {
  member: GuildMember;
  variant?: 'modal' | 'side';
  onClose: () => void;
  onSendMessage?: (member: GuildMember) => void;
  onUnfriend?: (member: GuildMember) => void;
};

export function ProfileCard({
  member,
  variant = 'modal',
  onClose,
  onSendMessage,
  onUnfriend
}: ProfileCardProps) {
  useCloseOnEscape(onClose);

  const card = (
    <section
      className={`relative w-full overflow-hidden rounded-[1rem] bg-secondary-bg shadow-2xl shadow-black/50 ring-1 ring-stroke ${
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
          <span
            className="font-category mb-2 max-w-[11rem] truncate rounded-full border border-stroke bg-panel px-3 py-1 text-[0.68rem] uppercase tracking-[0.14em] text-white/45"
            style={
              member.roleColor
                ? {
                    color: member.roleColor,
                    borderColor: `${member.roleColor}59`,
                    backgroundColor: `${member.roleColor}1a`
                  }
                : undefined
            }
            title={member.role}
          >
            {member.role}
          </span>
        </div>

        <div className="mt-4">
          <h2 className="truncate text-[1.6rem] font-bold tracking-[-0.05em] text-white">
            {member.name}
          </h2>
          <p className="mt-1 text-sm text-white/45">{member.activity}</p>
        </div>

        <div className="mt-4 rounded-md border border-stroke bg-panel px-3 py-3">
          <p className="font-category text-[0.68rem] uppercase tracking-[0.14em] text-white/35">
            Bio
          </p>
          <p className="mt-2 text-sm leading-6 text-white/70">
            {member.bio ? member.bio : 'No bio set.'}
          </p>
        </div>

        {member.roles && member.roles.length > 0 ? (
          <div className="mt-4 rounded-md border border-stroke bg-panel px-3 py-3">
            <p className="font-category text-[0.68rem] uppercase tracking-[0.14em] text-white/35">
              Roles
            </p>
            <div className="mt-2 flex flex-wrap gap-1.5">
              {member.roles.map((role) => (
                <span
                  key={role.id}
                  className="inline-flex items-center gap-1.5 rounded-full border border-stroke bg-frame px-2 py-0.5 text-[0.72rem] font-semibold text-white/70"
                >
                  <span
                    className="h-2 w-2 shrink-0 rounded-full"
                    style={{ backgroundColor: role.color || '#8b8b8f' }}
                  />
                  {role.name}
                </span>
              ))}
            </div>
          </div>
        ) : null}

        {onSendMessage ? (
          <button
            type="button"
            onClick={() => onSendMessage(member)}
            className="mt-5 flex h-11 w-full items-center justify-center gap-2 rounded-md bg-aqua text-sm font-bold text-primary-bg transition hover:bg-white"
          >
            <MessageCircle className="h-4 w-4" strokeWidth={2} />
            Send message
          </button>
        ) : null}

        {onUnfriend ? (
          <button
            type="button"
            onClick={() => onUnfriend(member)}
            className="mt-3 flex h-11 w-full items-center justify-center gap-2 rounded-md border border-pink/40 bg-pink/10 text-sm font-bold text-pink transition hover:bg-pink/20"
          >
            <UserMinus className="h-4 w-4" strokeWidth={2} />
            Unfriend
          </button>
        ) : null}
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
