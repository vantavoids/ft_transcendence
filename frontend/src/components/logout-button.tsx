'use client';

import { useRouter } from 'next/navigation';
import { LogOut } from 'lucide-react';
import { logout } from '../shared/api/auth';
import { clearSession } from '../shared/lib/session';

export function LogoutButton() {
  const router = useRouter();

  async function handleLogout() {
    try {
      await logout();
    } catch {
      // best-effort revoke; clear the local session regardless
    }
    clearSession();
    router.refresh();
  }

  return (
    <button
      type="button"
      onClick={handleLogout}
      className="inline-flex items-center gap-2 rounded-full border border-white/15 bg-panel px-5 py-2 text-sm font-bold text-white transition hover:border-pink/50 hover:text-pink"
    >
      <LogOut className="h-4 w-4" strokeWidth={2} />
      Log out
    </button>
  );
}
