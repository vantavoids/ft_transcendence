'use client';

import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react';
import { CheckCircle2, Info, TriangleAlert, X } from 'lucide-react';

type ToastTone = 'error' | 'success' | 'info';

type ToastInput = {
  title: string;
  description: string;
  tone?: ToastTone;
  durationMs?: number;
};

type ToastItem = ToastInput & {
  id: string;
};

type ToastContextValue = {
  pushToast: (toast: ToastInput) => string;
};

const ToastContext = createContext<ToastContextValue | null>(null);

function getToastClasses(tone: ToastTone) {
  switch (tone) {
    case 'success':
      return {
        shell: 'border-lime/25 bg-[rgba(14,18,16,0.96)] text-lime',
        accent: 'bg-lime/80',
        icon: CheckCircle2
      };
    case 'info':
      return {
        shell: 'border-aqua/25 bg-[rgba(10,15,18,0.96)] text-aqua',
        accent: 'bg-aqua/80',
        icon: Info
      };
    default:
      return {
        shell: 'border-pink/25 bg-[rgba(18,10,14,0.96)] text-pink',
        accent: 'bg-pink/80',
        icon: TriangleAlert
      };
  }
}

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([]);
  const timersRef = useRef<Map<string, number>>(new Map());
  const idRef = useRef(0);

  const dismissToast = useCallback((id: string) => {
    const timer = timersRef.current.get(id);
    if (timer !== undefined) {
      window.clearTimeout(timer);
      timersRef.current.delete(id);
    }
    setToasts((current) => current.filter((toast) => toast.id !== id));
  }, []);

  const pushToast = useCallback(
    ({ durationMs = 5000, tone = 'error', ...toast }: ToastInput) => {
      const id = `toast-${++idRef.current}`;
      setToasts((current) => [...current, { id, tone, durationMs, ...toast }]);

      const timer = window.setTimeout(() => {
        dismissToast(id);
      }, durationMs);
      timersRef.current.set(id, timer);

      return id;
    },
    [dismissToast]
  );

  useEffect(() => {
    return () => {
      for (const timer of timersRef.current.values()) {
        window.clearTimeout(timer);
      }
      timersRef.current.clear();
    };
  }, []);

  const value = useMemo(() => ({ pushToast }), [pushToast]);

  return (
    <ToastContext.Provider value={value}>
      {children}
      <div
        className="pointer-events-none fixed right-4 top-4 z-[80] grid w-[min(24rem,calc(100vw-2rem))] gap-3"
        aria-label="Notifications"
        aria-live="polite"
      >
        {toasts.map((toast) => {
          const { shell, accent, icon: Icon } = getToastClasses(toast.tone);
          return (
            <article
              key={toast.id}
              role={toast.tone === 'error' ? 'alert' : 'status'}
              className={`pointer-events-auto overflow-hidden rounded-[1rem] border shadow-2xl shadow-black/40 ${shell}`}
            >
              <div className={`h-1 ${accent}`} />
              <div className="flex items-start gap-3 px-4 py-3">
                <span className={`mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-md bg-white/5`}>
                  <Icon className="h-4 w-4" strokeWidth={2} />
                </span>
                <div className="min-w-0 flex-1">
                  <p className="text-sm font-bold tracking-[-0.02em] text-white">{toast.title}</p>
                  <p className="mt-1 text-sm leading-6 text-white/65">{toast.description}</p>
                </div>
                <button
                  type="button"
                  onClick={() => dismissToast(toast.id)}
                  className="mt-0.5 flex h-8 w-8 items-center justify-center rounded-md text-white/35 transition hover:bg-white/5 hover:text-white"
                  aria-label="Dismiss notification"
                >
                  <X className="h-4 w-4" strokeWidth={2} />
                </button>
              </div>
            </article>
          );
        })}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast() {
  const context = useContext(ToastContext);
  if (!context) {
    return {
      pushToast: () => 'toast-0'
    };
  }
  return context;
}
