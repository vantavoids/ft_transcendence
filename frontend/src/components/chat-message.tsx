'use client';

import { Check, Pencil, SmilePlus, Trash2, X } from 'lucide-react';

export type ChatMessageData = {
  id: string;
  author: string;
  accent: 'aqua' | 'yellow' | 'lime' | 'lavender' | 'pink';
  content: string[];
  timestamp: string;
  reactions?: Record<string, number>;
};

type ChatMessageProps = {
  message: ChatMessageData;
  isGrouped: boolean;
  isOwnMessage: boolean;
  isEditing: boolean;
  editingDraft: string;
  onEditDraftChange: (value: string) => void;
  onStartEdit: (message: ChatMessageData) => void;
  onSaveEdit: (messageId: string) => void;
  onCancelEdit: () => void;
  onDelete: (messageId: string) => void;
  onToggleReaction: (messageId: string) => void;
  setMessageRef: (messageId: string, element: HTMLElement | null) => void;
};

export function getAccentClasses(accent: ChatMessageData['accent']) {
  switch (accent) {
    case 'lime':
      return 'bg-lime text-primary-bg';
    case 'aqua':
      return 'bg-aqua text-primary-bg';
    case 'yellow':
      return 'bg-yellow text-primary-bg';
    case 'lavender':
      return 'bg-lavender text-primary-bg';
    default:
      return 'bg-pink text-primary-bg';
  }
}

export function getAccentText(accent: ChatMessageData['accent']) {
  switch (accent) {
    case 'lime':
      return 'text-lime';
    case 'aqua':
      return 'text-aqua';
    case 'yellow':
      return 'text-yellow';
    case 'lavender':
      return 'text-lavender';
    default:
      return 'text-pink';
  }
}

export function ChatMessage({
  message,
  isGrouped,
  isOwnMessage,
  isEditing,
  editingDraft,
  onEditDraftChange,
  onStartEdit,
  onSaveEdit,
  onCancelEdit,
  onDelete,
  onToggleReaction,
  setMessageRef
}: ChatMessageProps) {
  const reactions = Object.entries(message.reactions ?? {});

  return (
    <article
      ref={(element) => setMessageRef(message.id, element)}
      className={`group relative -mx-3 rounded-md px-3 py-1 pr-28 transition hover:bg-white/[0.04] ${
        isGrouped ? 'mt-1 grid grid-cols-[3rem_minmax(0,1fr)] gap-4' : 'mt-6 flex gap-4 first:mt-0'
      }`}
    >
      <div className="absolute right-3 top-0 hidden -translate-y-1/2 overflow-hidden rounded-md border border-white/10 bg-panel shadow-lg shadow-black/30 group-hover:flex">
        <button
          type="button"
          onClick={() => onToggleReaction(message.id)}
          className="flex h-8 w-8 items-center justify-center text-[#8b8b8f] transition hover:bg-frame hover:text-white"
          aria-label="React to message"
        >
          <SmilePlus className="h-4 w-4" strokeWidth={1.8} />
        </button>
        {isOwnMessage ? (
          <>
            <button
              type="button"
              onClick={() => onStartEdit(message)}
              className="flex h-8 w-8 items-center justify-center text-[#8b8b8f] transition hover:bg-frame hover:text-white"
              aria-label="Edit message"
            >
              <Pencil className="h-4 w-4" strokeWidth={1.8} />
            </button>
            <button
              type="button"
              onClick={() => onDelete(message.id)}
              className="flex h-8 w-8 items-center justify-center text-[#8b8b8f] transition hover:bg-frame hover:text-pink"
              aria-label="Delete message"
            >
              <Trash2 className="h-4 w-4" strokeWidth={1.8} />
            </button>
          </>
        ) : null}
      </div>

      {isGrouped ? (
        <span className="mono-detail pt-1 text-right text-[0.72rem] text-white/0 transition group-hover:text-white/35">
          {message.timestamp}
        </span>
      ) : (
        <div
          className={`flex h-12 w-12 shrink-0 items-center justify-center rounded-full text-xl font-semibold ${getAccentClasses(
            message.accent
          )}`}
        >
          {message.author.slice(0, 1).toUpperCase()}
        </div>
      )}

      <div className="min-w-0">
        {isGrouped ? null : (
          <div className="flex items-end gap-3">
            <h3
              className={`text-[1.5rem] font-bold tracking-[-0.06em] ${getAccentText(message.accent)}`}
            >
              {message.author}
            </h3>
            <span className="mono-detail pb-2 text-xs text-white/35">{message.timestamp}</span>
          </div>
        )}

        {isEditing ? (
          <div className={isGrouped ? '' : 'mt-2'}>
            <textarea
              value={editingDraft}
              onChange={(event) => onEditDraftChange(event.target.value)}
              rows={Math.max(2, editingDraft.split(/\r?\n/).length)}
              className="w-full resize-none rounded-md border border-aqua/30 bg-panel px-3 py-2 text-[1.05rem] text-white outline-none sm:text-[1.15rem]"
            />
            <div className="mt-2 flex gap-2">
              <button
                type="button"
                onClick={() => onSaveEdit(message.id)}
                className="flex h-8 items-center gap-2 rounded-md bg-aqua px-3 text-sm font-bold text-primary-bg"
              >
                <Check className="h-4 w-4" strokeWidth={2} />
                Save
              </button>
              <button
                type="button"
                onClick={onCancelEdit}
                className="flex h-8 items-center gap-2 rounded-md bg-frame px-3 text-sm font-bold text-white/70 transition hover:text-white"
              >
                <X className="h-4 w-4" strokeWidth={2} />
                Cancel
              </button>
            </div>
          </div>
        ) : (
          <div
            className={`space-y-2 text-[1.05rem] text-white/80 sm:text-[1.15rem] ${
              isGrouped ? '' : 'mt-1'
            }`}
          >
            {message.content.map((line, index) => (
              <p key={`${message.id}-${index}`} className="break-words">
                {line}
              </p>
            ))}
          </div>
        )}

        {reactions.length > 0 ? (
          <div className="mt-2 flex flex-wrap gap-2">
            {reactions.map(([emoji, count]) => (
              <button
                key={emoji}
                type="button"
                onClick={() => onToggleReaction(message.id)}
                className="flex h-7 items-center gap-1 rounded-full border border-aqua/30 bg-aqua/10 px-2 text-sm text-aqua transition hover:border-aqua"
              >
                <span>{emoji}</span>
                <span className="mono-detail text-xs">{count}</span>
              </button>
            ))}
          </div>
        ) : null}
      </div>
    </article>
  );
}
