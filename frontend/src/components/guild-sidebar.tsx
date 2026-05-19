import Link from 'next/link';
import { Plus } from 'lucide-react';

export function GuildSidebar() {
  return (
    <aside className="hidden w-[7.25rem] flex-col rounded-[1rem] bg-secondary-bg px-5 py-6 ring-1 ring-white/5 md:flex">
      <Link href="/" className="mono-detail text-[2rem] font-bold tracking-[-0.06em] text-white">
        Logo<span className="text-aqua">_</span>
      </Link>
      <div className="mx-1 mt-5 border-t border-white/10" />
      <div className="mt-6 flex flex-1 flex-col gap-4">
        {[0, 1, 2, 3].map((index) => (
          <button
            key={index}
            type="button"
            className={`h-[4.9rem] rounded-xl border transition ${
              index === 1
                ? 'border-aqua shadow-[0_0_0_1px_rgba(120,220,232,0.2)]'
                : 'border-frame'
            }`}
            aria-label={`Server ${index + 1}`}
          />
        ))}
        <button
          type="button"
          className="flex h-[4.9rem] items-center justify-center rounded-xl bg-panel text-[#535353] transition hover:text-white"
          aria-label="Add server"
        >
          <Plus className="h-8 w-8" strokeWidth={1.5} />
        </button>
      </div>
      <div className="flex justify-center pt-5 text-3xl tracking-[0.4em] text-[#9f9f9f]">...</div>
    </aside>
  );
}
