'use client';

import { useEffect } from 'react';
import { LogOut, Settings, X } from 'lucide-react';

type SettingsModalProps = {
  username: string;
  onClose: () => void;
  onDisconnect: () => void;
};

export function SettingsModal({ username, onClose, onDisconnect }: SettingsModalProps) {
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
        aria-label="Close settings"
      />
      <section className="relative w-full max-w-[22rem] overflow-hidden rounded-[1rem] bg-secondary-bg shadow-2xl shadow-black/50 ring-1 ring-white/10">
        <div className="flex h-[4.75rem] items-center justify-between border-b border-white/8 px-5">
          <div className="flex min-w-0 items-center gap-3">
            <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md bg-aqua/10 text-aqua">
              <Settings className="h-5 w-5" strokeWidth={1.9} />
            </span>
            <div className="min-w-0">
              <h2 className="truncate text-[1.15rem] font-bold tracking-[-0.03em] text-white">
                Settings
              </h2>
              <p className="font-category text-[0.7rem] uppercase tracking-[0.14em] text-white/35">
                Profile
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="flex h-9 w-9 items-center justify-center rounded-md text-[#8b8b8f] transition hover:bg-frame hover:text-white"
            aria-label="Close settings"
          >
            <X className="h-4 w-4" strokeWidth={2} />
          </button>
        </div>

        <div className="px-5 py-5">
          <div className="flex items-center gap-4">
            <div className="relative h-16 w-16 shrink-0 rounded-xl bg-[linear-gradient(135deg,#78dce8,#ab9df2,#ff6188)]">
              <div className="absolute bottom-1.5 right-1.5 h-3.5 w-3.5 rounded-full border-2 border-secondary-bg bg-lime" />
            </div>
            <div className="min-w-0">
              <h3 className="truncate text-[1.35rem] font-bold tracking-[-0.04em] text-white">
                {username}
              </h3>
              <p className="mono-detail mt-1 truncate text-sm text-white/40">{username}#4242</p>
            </div>
          </div>

          <button
            type="button"
            onClick={onDisconnect}
            className="mt-6 flex h-11 w-full items-center justify-center gap-2 rounded-md border border-pink/25 bg-pink/10 text-sm font-bold text-pink transition hover:border-pink/45 hover:bg-pink/15"
          >
            <LogOut className="h-4 w-4" strokeWidth={1.9} />
            Disconnect
          </button>
        </div>
      </section>
    </div>
  );
}
