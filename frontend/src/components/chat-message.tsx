'use client';

import { useEffect, useRef } from 'react';
import { CornerUpLeft, FileText, Pencil, SmilePlus, Trash2 } from 'lucide-react';
import type { MessageAttachmentDto } from '../shared/api/chat';

function formatFileSize(sizeBytes: number): string {
  if (sizeBytes < 1024) {
    return `${sizeBytes} B`;
  }

  const units = ['KB', 'MB', 'GB'];
  let value = sizeBytes / 1024;
  let unitIndex = 0;

  while (value >= 1024 && unitIndex < units.length - 1) {
    value /= 1024;
    unitIndex += 1;
  }

  return `${value.toFixed(1)} ${units[unitIndex]}`;
}

export type ReactionSummary = {
  emoji: string;
  count: number;
  meReacted: boolean;
};

export type ChatMessageData = {
  id: string;
  // real author_id once fetched from the API; absent for still-mocked DM messages (epic 4 step 4)
  authorId?: string;
  author: string;
  accent: 'aqua' | 'yellow' | 'lime' | 'lavender' | 'pink';
  content: string[];
  timestamp: string;
  // ISO created_at, needed for before_time pagination cursors; absent for mocked messages
  createdAt?: string;
  replyToId?: string | null;
  editedAt?: string | null;
  attachments?: MessageAttachmentDto[];
  // channel messages only - DMs never carry reactions (docs/contracts/chat.md)
  reactions?: ReactionSummary[];
  // set when the optimistic send failed - neither the 201 nor a matching
  // ReceiveMessage/ReceiveDirectMessage arrived (docs/contracts/chat.md nonce semantics)
  failed?: boolean;
};

export type ReplyPreview = {
  author: string;
  snippet: string;
};

type ChatMessageProps = {
  message: ChatMessageData;
  replyPreview?: ReplyPreview | null;
  isGrouped: boolean;
  isOwnMessage: boolean;
  isEditing: boolean;
  editingDraft: string;
  canReact: boolean;
  onEditDraftChange: (value: string) => void;
  onStartEdit: (message: ChatMessageData) => void;
  onSaveEdit: (messageId: string) => void;
  onCancelEdit: () => void;
  onDelete: (messageId: string) => void;
  onToggleReaction: (messageId: string, emoji: string) => void;
  onOpenAuthorProfile?: (message: ChatMessageData) => void;
  setMessageRef: (messageId: string, element: HTMLElement | null) => void;
};

const QUICK_REACTION_EMOJI = '👍';

function isEscapeKey(event: KeyboardEvent | React.KeyboardEvent) {
  return event.key === 'Escape' || event.key === 'Esc' || event.code === 'Escape';
}

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
  replyPreview,
  isGrouped,
  isOwnMessage,
  isEditing,
  editingDraft,
  canReact,
  onEditDraftChange,
  onStartEdit,
  onSaveEdit,
  onCancelEdit,
  onDelete,
  onToggleReaction,
  onOpenAuthorProfile,
  setMessageRef
}: ChatMessageProps) {
  const editTextareaRef = useRef<HTMLTextAreaElement>(null);
  const reactions = message.reactions ?? [];

  useEffect(() => {
    if (!isEditing) {
      return;
    }

    editTextareaRef.current?.focus();
    editTextareaRef.current?.setSelectionRange(editingDraft.length, editingDraft.length);
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isEditing]);

  useEffect(() => {
    if (!isEditing) {
      return;
    }

    function handleEscape(event: KeyboardEvent) {
      if (!isEscapeKey(event)) {
        return;
      }

      event.preventDefault();
      event.stopPropagation();
      onCancelEdit();
    }

    window.addEventListener('keydown', handleEscape, { capture: true });
    return () => window.removeEventListener('keydown', handleEscape, { capture: true });
  }, [isEditing, message.id, onCancelEdit]);

  return (
    <article
      ref={(element) => setMessageRef(message.id, element)}
      className={`group relative -mx-3 rounded-md px-3 py-1 pr-28 transition hover:bg-white/[0.04] ${
        isGrouped ? 'mt-1 grid grid-cols-[3rem_minmax(0,1fr)] gap-4' : 'mt-6 flex gap-4 first:mt-0'
      }`}
    >
      <div className="absolute right-3 top-0 hidden -translate-y-1/2 overflow-hidden rounded-md border border-white/10 bg-panel shadow-lg shadow-black/30 group-hover:flex">
        {canReact ? (
          <button
            type="button"
            onClick={() => onToggleReaction(message.id, QUICK_REACTION_EMOJI)}
            className="flex h-8 w-8 items-center justify-center text-[#8b8b8f] transition hover:bg-frame hover:text-white"
            aria-label="React to message"
          >
            <SmilePlus className="h-4 w-4" strokeWidth={1.8} />
          </button>
        ) : null}
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
          {message.editedAt ? ' (edited)' : ''}
          {message.failed ? <span className="text-pink"> · Failed to send</span> : null}
        </span>
      ) : (
        <button
          type="button"
          onClick={() => onOpenAuthorProfile?.(message)}
          className={`flex h-12 w-12 shrink-0 items-center justify-center rounded-full text-xl font-semibold ${getAccentClasses(
            message.accent
          )} ${onOpenAuthorProfile ? 'transition hover:scale-105 hover:ring-2 hover:ring-white/25' : ''}`}
          aria-label={`Open ${message.author} profile`}
        >
          {message.author.slice(0, 1).toUpperCase()}
        </button>
      )}

      <div className="min-w-0">
        {isGrouped ? null : (
          <div className="flex items-end gap-3">
            <h3
              className={`text-[1.5rem] font-bold tracking-[-0.06em] ${getAccentText(message.accent)}`}
            >
              {message.author}
            </h3>
            <span className="mono-detail pb-2 text-xs text-white/35">
              {message.timestamp}
              {message.editedAt ? ' (edited)' : ''}
              {message.failed ? <span className="text-pink"> · Failed to send</span> : null}
            </span>
          </div>
        )}

        {!isEditing && replyPreview ? (
          <p className="mono-detail mb-1 flex items-center gap-1.5 text-xs text-white/35">
            <CornerUpLeft className="h-3 w-3 shrink-0" strokeWidth={2} />
            <span className="truncate">
              {replyPreview.author ? (
                <>
                  Replying to <span className="text-white/55">{replyPreview.author}</span>:{' '}
                  {replyPreview.snippet}
                </>
              ) : (
                <>Replying to {replyPreview.snippet}</>
              )}
            </span>
          </p>
        ) : null}

        {isEditing ? (
          <div className={isGrouped ? '' : 'mt-2'}>
            <textarea
              ref={editTextareaRef}
              value={editingDraft}
              onChange={(event) => onEditDraftChange(event.target.value)}
              onKeyDownCapture={(event) => {
                if (isEscapeKey(event)) {
                  event.preventDefault();
                  event.stopPropagation();
                  onCancelEdit();
                  return;
                }

                if (event.key === 'Enter' && !event.shiftKey) {
                  event.preventDefault();
                  onSaveEdit(message.id);
                }
              }}
              rows={Math.max(2, editingDraft.split(/\r?\n/).length)}
              className="w-full resize-none rounded-md border border-aqua/30 bg-panel px-3 py-2 text-[1.05rem] text-white outline-none sm:text-[1.15rem]"
            />
            <p className="mono-detail mt-2 text-xs text-white/35">
              <button
                type="button"
                onClick={() => onSaveEdit(message.id)}
                className="text-aqua underline-offset-2 transition hover:text-white hover:underline"
              >
                Enter
              </button>
              {' to save · Shift+Enter for newline · '}
              <button
                type="button"
                onClick={onCancelEdit}
                className="text-aqua underline-offset-2 transition hover:text-white hover:underline"
              >
                Escape
              </button>
              {' to cancel'}
            </p>
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

        {!isEditing && message.attachments && message.attachments.length > 0 ? (
          <div className="mt-2 flex flex-wrap gap-2">
            {message.attachments.map((attachment) =>
              attachment.mime_type.startsWith('image/') ? (
                <a
                  key={attachment.id}
                  href={attachment.url}
                  target="_blank"
                  rel="noreferrer"
                  className="block max-w-[16rem] overflow-hidden rounded-md border border-white/10"
                >
                  {/* eslint-disable-next-line @next/next/no-img-element -- same-origin, deployment-specific attachment host, not worth a next/image remotePatterns entry */}
                  <img src={attachment.url} alt={attachment.filename} className="max-h-64 w-full object-cover" />
                </a>
              ) : (
                <a
                  key={attachment.id}
                  href={attachment.url}
                  target="_blank"
                  rel="noreferrer"
                  className="flex items-center gap-2 rounded-md border border-white/10 bg-panel px-3 py-2 text-sm text-white/80 transition hover:border-aqua/40 hover:text-white"
                >
                  <FileText className="h-4 w-4 shrink-0" strokeWidth={1.8} />
                  <span className="truncate">{attachment.filename}</span>
                  <span className="mono-detail shrink-0 text-xs text-white/35">
                    {formatFileSize(attachment.size_bytes)}
                  </span>
                </a>
              )
            )}
          </div>
        ) : null}

        {reactions.length > 0 ? (
          <div className="mt-2 flex flex-wrap gap-2">
            {reactions.map(({ emoji, count, meReacted }) => (
              <button
                key={emoji}
                type="button"
                disabled={!canReact}
                onClick={() => onToggleReaction(message.id, emoji)}
                className={`flex h-7 items-center gap-1 rounded-full border px-2 text-sm transition ${
                  meReacted
                    ? 'border-aqua bg-aqua/20 text-aqua'
                    : 'border-aqua/30 bg-aqua/10 text-aqua hover:border-aqua'
                } ${!canReact ? 'cursor-default' : ''}`}
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
