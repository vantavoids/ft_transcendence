'use client';

import { useState } from 'react';
import { Plus, X } from 'lucide-react';
import { GuildCreateForm, GuildJoinForm } from './guild-forms';
import { useCloseOnEscape } from '../../shared/hooks/use-close-on-escape';

type AddGuildModalProps = {
  onClose: () => void;
};

export function AddGuildModal({ onClose }: AddGuildModalProps) {
  const [tab, setTab] = useState<'create' | 'join'>('create');

  useCloseOnEscape(onClose);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/55 px-4 py-6">
      <button
        type="button"
        className="absolute inset-0 cursor-default"
        onClick={onClose}
        aria-label="Close add guild"
      />
      <section className="relative w-full max-w-[24rem] overflow-hidden rounded-[1rem] bg-secondary-bg shadow-2xl shadow-black/50 ring-1 ring-white/10">
        <div className="flex h-[4.75rem] items-center justify-between border-b border-white/8 px-5">
          <div className="flex min-w-0 items-center gap-3">
            <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md bg-aqua/10 text-aqua">
              <Plus className="h-5 w-5" strokeWidth={1.9} />
            </span>
            <h2 className="truncate text-[1.15rem] font-bold tracking-[-0.03em] text-white">
              Add a guild
            </h2>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="flex h-9 w-9 items-center justify-center rounded-md text-[#8b8b8f] transition hover:bg-frame hover:text-white"
            aria-label="Close add guild"
          >
            <X className="h-4 w-4" strokeWidth={2} />
          </button>
        </div>

        <div className="px-5 py-5">
          <div className="mb-5 flex gap-2">
            {(['create', 'join'] as const).map((option) => (
              <button
                key={option}
                type="button"
                onClick={() => setTab(option)}
                className={`h-9 flex-1 rounded-md text-sm font-bold transition ${
                  tab === option
                    ? 'bg-aqua/15 text-aqua'
                    : 'bg-panel text-white/45 hover:text-white'
                }`}
              >
                {option === 'create' ? 'Create' : 'Join with invite'}
              </button>
            ))}
          </div>
          {tab === 'create' ? (
            <GuildCreateForm onCreated={onClose} />
          ) : (
            <GuildJoinForm onJoined={onClose} />
          )}
        </div>
      </section>
    </div>
  );
}
