import type { RefObject } from 'react';
import { ArrowRight, CornerUpLeft, MessageCircle, Paperclip, Smile, X } from 'lucide-react';
import type { ChatMessageData } from '../chat-message';
import type { PendingAttachment } from '../../shared/hooks/use-conversation-history';

const emojiOptions = ['😀', '😅', '🤣', '😂', '🙂', '🙃', '🤔', '😎', '🥳', '😍', '😘', '😉'];

type MessageComposerProps = {
  fileInputRef: RefObject<HTMLInputElement | null>;
  composerRef: RefObject<HTMLTextAreaElement | null>;
  chatMode: 'guild' | 'dm';
  activeConversationName: string;
  activeDraft: string;
  isComposerDisabled: boolean;
  isSendDisabled: boolean;
  isActiveDmArchived: boolean;
  replyTarget: ChatMessageData | null;
  pendingAttachments: PendingAttachment[];
  isEmojiOpen: boolean;
  onFilesSelected: (event: React.ChangeEvent<HTMLInputElement>) => void;
  onRemovePendingAttachment: (attachmentId: string) => void;
  onCancelReply: () => void;
  onToggleEmojiPicker: () => void;
  onAppendEmoji: (emoji: string) => void;
  onDraftChange: (value: string) => void;
  onSubmitMessage: () => void;
  onShowMobileSidebar: () => void;
};

export function MessageComposer({
  fileInputRef,
  composerRef,
  chatMode,
  activeConversationName,
  activeDraft,
  isComposerDisabled,
  isSendDisabled,
  isActiveDmArchived,
  replyTarget,
  pendingAttachments,
  isEmojiOpen,
  onFilesSelected,
  onRemovePendingAttachment,
  onCancelReply,
  onToggleEmojiPicker,
  onAppendEmoji,
  onDraftChange,
  onSubmitMessage,
  onShowMobileSidebar
}: MessageComposerProps) {
  return (
    <div className="shrink-0 border-t border-white/8 px-4 py-4 sm:px-5">
      <input ref={fileInputRef} type="file" multiple onChange={onFilesSelected} className="hidden" />
      {replyTarget ? (
        <div className="mb-3 flex items-center justify-between gap-3 rounded-md border border-white/10 bg-panel px-3 py-2 text-xs text-white/60">
          <span className="flex min-w-0 items-center gap-1.5">
            <CornerUpLeft className="h-3.5 w-3.5 shrink-0" strokeWidth={2} />
            <span className="truncate">
              Replying to <span className="text-white/85">{replyTarget.author}</span>:{' '}
              {replyTarget.content[0] ?? ''}
            </span>
          </span>
          <button
            type="button"
            onClick={onCancelReply}
            aria-label="Cancel reply"
            className="shrink-0 text-white/40 hover:text-white"
          >
            <X className="h-3.5 w-3.5" strokeWidth={2} />
          </button>
        </div>
      ) : null}
      {pendingAttachments.length > 0 ? (
        <div className="mb-3 flex flex-wrap gap-2">
          {pendingAttachments.map((attachment) => (
            <span
              key={attachment.id}
              className={`flex h-8 items-center gap-2 rounded-full border px-3 text-xs ${
                attachment.status === 'error'
                  ? 'border-pink/40 text-pink'
                  : 'border-white/10 text-white/70'
              }`}
            >
              <span className="max-w-[10rem] truncate">{attachment.filename}</span>
              {attachment.status === 'uploading' ? <span>Uploading…</span> : null}
              {attachment.status === 'error' ? <span>Failed</span> : null}
              <button
                type="button"
                onClick={() => onRemovePendingAttachment(attachment.id)}
                aria-label={`Remove ${attachment.filename}`}
                className="text-white/40 hover:text-white"
              >
                <X className="h-3 w-3" strokeWidth={2} />
              </button>
            </span>
          ))}
        </div>
      ) : null}
      {isEmojiOpen ? (
        <div className="mb-3 rounded-xl border border-white/10 bg-panel p-3">
          <div className="grid grid-cols-6 gap-2">
            {emojiOptions.map((emoji) => (
              <button
                key={emoji}
                type="button"
                onClick={() => onAppendEmoji(emoji)}
                className="rounded-lg bg-frame px-2 py-2 text-2xl transition hover:bg-white/10"
              >
                {emoji}
              </button>
            ))}
          </div>
        </div>
      ) : null}
      <div className="flex h-14 items-center rounded-md bg-panel px-4 text-muted">
        <textarea
          ref={composerRef}
          value={activeDraft}
          onChange={(event) => onDraftChange(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === 'Enter' && !event.shiftKey) {
              event.preventDefault();
              onSubmitMessage();
            }
          }}
          disabled={isComposerDisabled}
          placeholder={
            isActiveDmArchived
              ? 'This conversation is archived -- send a message to unarchive it.'
              : `Message ${chatMode === 'dm' ? '@' : '#'}${activeConversationName}`
          }
          rows={1}
          className="h-full min-h-0 w-full resize-none overflow-y-auto bg-transparent py-4 text-lg leading-6 text-white outline-none placeholder:text-muted disabled:cursor-not-allowed disabled:text-white/30"
        />
        <div className="ml-auto flex items-center gap-4">
          <button
            type="button"
            onClick={() => fileInputRef.current?.click()}
            disabled={isComposerDisabled}
            className="text-[#7e7e82] transition hover:text-white disabled:cursor-not-allowed disabled:text-[#535353]"
            aria-label="Attach files"
          >
            <Paperclip className="h-5 w-5" strokeWidth={1.8} />
          </button>
          <button
            type="button"
            onClick={onToggleEmojiPicker}
            className="text-[#7e7e82] transition hover:text-white"
            aria-label="Toggle emoji picker"
          >
            <Smile className="h-5 w-5" strokeWidth={1.8} />
          </button>
          <button
            type="button"
            onClick={onSubmitMessage}
            disabled={isSendDisabled}
            className="text-aqua transition hover:text-white disabled:cursor-not-allowed disabled:text-[#535353]"
            aria-label="Send message"
          >
            <ArrowRight className="h-5 w-5 rounded-full border border-aqua p-0.5" strokeWidth={2} />
          </button>
        </div>
      </div>
      <div className="mt-3 flex justify-between text-xs text-white/35">
        <span>{chatMode === 'dm' ? 'Local direct conversation' : 'Local channel'}</span>
        <button
          type="button"
          onClick={onShowMobileSidebar}
          className="inline-flex items-center gap-2 md:hidden"
        >
          <MessageCircle className="h-4 w-4" strokeWidth={1.8} />
          {chatMode === 'dm' ? 'DMs' : 'Channels'}
        </button>
      </div>
    </div>
  );
}
