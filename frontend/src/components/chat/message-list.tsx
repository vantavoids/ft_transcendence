import { useMemo, type RefObject } from 'react';
import { MessageCircle } from 'lucide-react';
import { ChatMessage, type ChatMessageData, type ReplyPreview } from '../chat-message';

const MESSAGE_GROUP_THRESHOLD_MINUTES = 5;

function getTimestampMinutes(timestamp: string) {
  const [hours, minutes] = timestamp.split(':').map(Number);

  if (!Number.isFinite(hours) || !Number.isFinite(minutes)) {
    return null;
  }

  return hours * 60 + minutes;
}

function getMinutesBetween(previousTimestamp: string, currentTimestamp: string) {
  const previousMinutes = getTimestampMinutes(previousTimestamp);
  const currentMinutes = getTimestampMinutes(currentTimestamp);

  if (previousMinutes === null || currentMinutes === null) {
    return Number.POSITIVE_INFINITY;
  }

  return currentMinutes >= previousMinutes
    ? currentMinutes - previousMinutes
    : currentMinutes + 24 * 60 - previousMinutes;
}

type MessageListProps = {
  viewportRef: RefObject<HTMLDivElement | null>;
  onScroll: () => void;
  isDmEmptyState: boolean;
  activeMessages: ChatMessageData[];
  currentUserId: string | null;
  blockedUserIds: string[];
  editingMessageId: string | null;
  editingDraft: string;
  highlightedMessageId: string | null;
  canReact: boolean;
  isNearBottom: boolean;
  setMessageRef: (messageId: string, element: HTMLElement | null) => void;
  onEditDraftChange: (value: string) => void;
  onStartEdit: (message: ChatMessageData) => void;
  onSaveEdit: (messageId: string) => Promise<void>;
  onCancelEdit: () => void;
  onDelete: (messageId: string) => Promise<void>;
  onToggleReaction: (messageId: string, emoji: string) => Promise<void>;
  onReply: (message: ChatMessageData) => void;
  onJumpToReply: (messageId: string) => void;
  onRetryMessage: (messageId: string) => Promise<void>;
  onOpenAuthorProfile: (message: ChatMessageData) => void;
  onScrollToBottom: () => void;
};

export function MessageList({
  viewportRef,
  onScroll,
  isDmEmptyState,
  activeMessages,
  currentUserId,
  blockedUserIds,
  editingMessageId,
  editingDraft,
  highlightedMessageId,
  canReact,
  isNearBottom,
  setMessageRef,
  onEditDraftChange,
  onStartEdit,
  onSaveEdit,
  onCancelEdit,
  onDelete,
  onToggleReaction,
  onReply,
  onJumpToReply,
  onRetryMessage,
  onOpenAuthorProfile,
  onScrollToBottom
}: MessageListProps) {
  const blockedUserIdSet = useMemo(() => new Set(blockedUserIds), [blockedUserIds]);
  const activeMessageItems = useMemo(() => {
    const messagesById = new Map(activeMessages.map((message) => [message.id, message]));

    return activeMessages.map((message, index) => {
      const previousMessage = activeMessages[index - 1];
      const isGrouped =
        (previousMessage?.authorId ?? previousMessage?.author) ===
          (message.authorId ?? message.author) &&
        getMinutesBetween(previousMessage.timestamp, message.timestamp) <=
          MESSAGE_GROUP_THRESHOLD_MINUTES;

      let replyPreview: ReplyPreview | null = null;
      if (message.replyToId) {
        const target = messagesById.get(message.replyToId);
        replyPreview = target
          ? { author: target.author, snippet: target.content[0] ?? '' }
          : { author: '', snippet: 'an earlier message' };
      }

      const isBlockedMessage =
        message.authorId != null &&
        currentUserId != null &&
        message.authorId !== currentUserId &&
        blockedUserIdSet.has(message.authorId);

      return { message, isGrouped, replyPreview, isBlockedMessage };
    });
  }, [activeMessages, blockedUserIdSet, currentUserId]);

  return (
    <div
      ref={viewportRef}
      onScroll={onScroll}
      className="min-h-0 flex-1 overflow-auto px-5 py-7 sm:px-7"
    >
      {isDmEmptyState ? (
        <div className="flex min-h-full flex-col items-center justify-center px-6 text-center">
          <div className="flex h-16 w-16 items-center justify-center rounded-full bg-panel text-[#8b8b8f]">
            <MessageCircle className="h-7 w-7" strokeWidth={1.8} />
          </div>
          <h3 className="mt-5 text-[1.25rem] font-bold tracking-[-0.03em] text-white">
            No DM selected
          </h3>
          <p className="mt-2 max-w-[22rem] text-sm leading-6 text-white/40">
            Select a conversation from the DM list to start reading or sending messages.
          </p>
        </div>
      ) : (
        <div>
          {activeMessageItems.map(({ message, isGrouped, replyPreview, isBlockedMessage }) => {
            const isOwnMessage = message.authorId != null && message.authorId === currentUserId;
            const isEditing = editingMessageId === message.id;

            return (
              <ChatMessage
                key={message.id}
                message={message}
                replyPreview={replyPreview}
                isGrouped={isGrouped}
                isOwnMessage={isOwnMessage}
                isEditing={isEditing}
                isHighlighted={highlightedMessageId === message.id}
                isBlockedMessage={isBlockedMessage}
                editingDraft={editingDraft}
                canReact={canReact}
                onEditDraftChange={onEditDraftChange}
                onStartEdit={onStartEdit}
                onSaveEdit={onSaveEdit}
                onCancelEdit={onCancelEdit}
                onDelete={onDelete}
                onToggleReaction={onToggleReaction}
                onReply={onReply}
                onJumpToReply={onJumpToReply}
                onRetryMessage={onRetryMessage}
                onOpenAuthorProfile={onOpenAuthorProfile}
                setMessageRef={setMessageRef}
              />
            );
          })}
        </div>
      )}
      {!isNearBottom ? (
        <button
          type="button"
          onClick={onScrollToBottom}
          className="mono-detail sticky bottom-0 z-10 ml-auto flex h-10 items-center rounded-full border border-aqua/40 bg-panel px-4 text-sm font-bold text-aqua shadow-lg shadow-black/30 transition hover:border-aqua hover:text-white"
        >
          Jump to bottom
        </button>
      ) : null}
    </div>
  );
}
