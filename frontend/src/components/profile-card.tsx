'use client';

import { useEffect } from 'react';
import { MessageCircle, Shield, Trophy, X } from 'lucide-react';
import { getAccentClasses } from './chat-message';
import { getDmStatusClasses } from './dm-list';
import type { GuildMember } from './guild-member-list';

type ProfileCardProps = {
  member: GuildMember;
  onClose: () => void;
};

export function ProfileCard({ member, onClose }: ProfileCardProps) {
  useEffect(() => {
    function handleEscape(event: KeyboardEvent) {
      if (event.key !== 'Escape' && event.key !== 'Esc' && event.code !== 'Escape') {
        return;
      }

      onClose();
    }

    window.addEventListener('keydown', handleEscape);
    return () => window.removeEventListener('keydown', handleEscape);
  }, [onClose]);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/55 px-4 py-6">
      <button
        type="button"
        className="absolute inset-0 cursor-default"
        onClick={onClose}
        aria-label="Close profile"
      />
      <section className="relative w-full max-w-[23rem] overflow-hidden rounded-[1rem] bg-secondary-bg shadow-2xl shadow-black/50 ring-1 ring-white/10">
        <div className="h-24 bg-[linear-gradient(135deg,#1a1a1c_0%,#27333a_46%,#78dce8_100%)]" />
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
            <span className="relative shrink-0">
              <span
                className={`flex h-20 w-20 items-center justify-center rounded-full border-4 border-secondary-bg text-3xl font-bold ${getAccentClasses(
                  member.accent
                )}`}
              >
                {member.name.slice(0, 1).toUpperCase()}
              </span>
              <span
                className={`absolute bottom-1 right-1 h-4 w-4 rounded-full border-2 border-secondary-bg ${getDmStatusClasses(
                  member.status
                )}`}
              />
            </span>
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
              <p className="mt-2 truncate text-sm font-semibold text-white">server_name</p>
            </div>
          </div>

          <button
            type="button"
            className="mt-5 flex h-11 w-full items-center justify-center gap-2 rounded-md bg-aqua text-sm font-bold text-primary-bg transition hover:bg-white"
          >
            <MessageCircle className="h-4 w-4" strokeWidth={2} />
            Message
          </button>
        </div>
      </section>
    </div>
  );
}
