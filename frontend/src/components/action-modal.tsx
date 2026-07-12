'use client';

import type { ReactNode } from 'react';
import { AlertTriangle, X } from 'lucide-react';
import { useCloseOnEscape } from '../shared/hooks/use-close-on-escape';

type ActionModalProps = {
  title: string;
  description: ReactNode;
  confirmLabel: string;
  cancelLabel?: string;
  destructive?: boolean;
  isBusy?: boolean;
  onClose: () => void;
  onConfirm: () => void | Promise<void>;
  children?: ReactNode;
};

export function ActionModal({
  title,
  description,
  confirmLabel,
  cancelLabel = 'Cancel',
  destructive = false,
  isBusy = false,
  onClose,
  onConfirm,
  children
}: ActionModalProps) {
  useCloseOnEscape(onClose);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/55 px-4 py-6">
      <button
        type="button"
        className="absolute inset-0 cursor-default"
        onClick={onClose}
        aria-label="Close dialog"
      />
      <section className="relative w-full max-w-[28rem] overflow-hidden rounded-[1rem] bg-secondary-bg shadow-2xl shadow-black/50 ring-1 ring-stroke">
        <div className="flex h-[4.75rem] items-center justify-between border-b border-stroke px-5">
          <div className="flex min-w-0 items-center gap-3">
            <span
              className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-md ${
                destructive ? 'bg-pink/10 text-pink' : 'bg-aqua/10 text-aqua'
              }`}
            >
              <AlertTriangle className="h-5 w-5" strokeWidth={1.9} />
            </span>
            <div className="min-w-0">
              <h2 className="truncate text-[1.15rem] font-bold tracking-[-0.03em] text-white">
                {title}
              </h2>
              <p className="font-category text-[0.7rem] uppercase tracking-[0.14em] text-white/35">
                Confirm action
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="flex h-9 w-9 items-center justify-center rounded-md text-[#8b8b8f] transition hover:bg-frame hover:text-white"
            aria-label="Close dialog"
          >
            <X className="h-4 w-4" strokeWidth={2} />
          </button>
        </div>

        <div className="grid gap-4 p-5">
          <div className="grid gap-3">
            <div className="text-sm leading-6 text-white/65">{description}</div>
            {children ? <div className="grid gap-2">{children}</div> : null}
          </div>
          <div className="flex flex-wrap justify-end gap-3">
            <button
              type="button"
              onClick={onClose}
              className="h-10 rounded-md border border-stroke bg-frame px-5 text-sm font-bold text-white/70 transition hover:text-white"
            >
              {cancelLabel}
            </button>
            <button
              type="button"
              onClick={() => void onConfirm()}
              disabled={isBusy}
              className={`h-10 rounded-md px-5 text-sm font-bold transition disabled:cursor-not-allowed disabled:opacity-50 ${
                destructive
                  ? 'border border-pink/25 bg-pink/10 text-pink hover:border-pink/45 hover:bg-pink/15'
                  : 'bg-aqua text-primary-bg hover:bg-white'
              }`}
            >
              {isBusy ? 'Working...' : confirmLabel}
            </button>
          </div>
        </div>
      </section>
    </div>
  );
}
