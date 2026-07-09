'use client';

import { useCallback, useLayoutEffect, useRef, useState } from 'react';
import type { ChatMessageData } from '../../components/chat-message';

const BOTTOM_THRESHOLD_PX = 96;

type ConversationScrollPosition = {
  messageId: string;
  topOffset: number;
};

export type ScrollPreservation = {
  messagesViewportRef: React.RefObject<HTMLDivElement | null>;
  composerRef: React.RefObject<HTMLTextAreaElement | null>;
  isNearBottom: boolean;
  setMessageRef: (messageId: string, element: HTMLElement | null) => void;
  rememberConversationScrollPosition: (conversationId: string | null) => void;
  updateNearBottomState: () => void;
  scrollToBottomOnNextRender: () => void;
  scrollToBottom: () => void;
};

// bundles the message-viewport scroll anchoring (restore the same message
// under the same viewport offset when history is prepended, or jump to the
// bottom for a fresh send) with the composer autofocus - both key off the
// same activeConversationId/message-count changes, so one layout effect
// covers both instead of two separate effects.
export function useScrollPreservation(
  activeConversationId: string | null,
  activeMessages: ChatMessageData[],
  messagesByConversation: Record<string, ChatMessageData[]>,
  isHydrated: boolean
): ScrollPreservation {
  const messagesViewportRef = useRef<HTMLDivElement>(null);
  const composerRef = useRef<HTMLTextAreaElement>(null);
  const messageRefs = useRef<Record<string, HTMLElement | null>>({});
  const conversationScrollPositions = useRef<Record<string, ConversationScrollPosition>>({});
  const pendingScrollBottom = useRef(false);
  const isRestoringScroll = useRef(false);
  // starts false (not "assume yes") so nothing treats this as a genuine
  // measurement - e.g. the mark-as-read effect - until updateNearBottomState()
  // has actually run against the real DOM at least once
  const [isNearBottom, setIsNearBottom] = useState(false);

  const updateNearBottomState = useCallback(() => {
    const viewport = messagesViewportRef.current;
    if (!viewport) {
      setIsNearBottom(true);
      return;
    }

    const distanceFromBottom = viewport.scrollHeight - viewport.scrollTop - viewport.clientHeight;
    setIsNearBottom(distanceFromBottom <= BOTTOM_THRESHOLD_PX);
  }, []);

  const rememberConversationScrollPosition = useCallback(
    (conversationId: string | null) => {
      if (!conversationId || isRestoringScroll.current) {
        return;
      }

      const viewport = messagesViewportRef.current;
      if (!viewport) {
        return;
      }

      const viewportTop = viewport.getBoundingClientRect().top;
      const visibleMessage = (messagesByConversation[conversationId] ?? [])
        .map((message) => {
          const element = messageRefs.current[message.id];
          if (!element) {
            return null;
          }

          return {
            element,
            id: message.id,
            top: element.getBoundingClientRect().top
          };
        })
        .filter((message): message is { element: HTMLElement; id: string; top: number } => {
          if (!message) {
            return false;
          }

          return message.element.getBoundingClientRect().bottom >= viewportTop;
        })
        .sort((a, b) => Math.abs(a.top - viewportTop) - Math.abs(b.top - viewportTop))[0];

      if (!visibleMessage) {
        return;
      }

      conversationScrollPositions.current[conversationId] = {
        messageId: visibleMessage.id,
        topOffset: visibleMessage.top - viewportTop
      };
    },
    [messagesByConversation]
  );

  useLayoutEffect(() => {
    if (!activeConversationId) {
      return;
    }

    const viewport = messagesViewportRef.current;
    if (!viewport) {
      return;
    }

    isRestoringScroll.current = true;

    if (pendingScrollBottom.current) {
      viewport.scrollTop = viewport.scrollHeight;
      pendingScrollBottom.current = false;
    } else {
      const savedPosition = conversationScrollPositions.current[activeConversationId];
      const savedElement = savedPosition ? messageRefs.current[savedPosition.messageId] : null;

      if (savedPosition && savedElement) {
        const viewportTop = viewport.getBoundingClientRect().top;
        const elementTop = savedElement.getBoundingClientRect().top;
        viewport.scrollTop += elementTop - viewportTop - savedPosition.topOffset;
      } else {
        viewport.scrollTop = viewport.scrollHeight;
      }
    }

    window.requestAnimationFrame(() => {
      isRestoringScroll.current = false;
      rememberConversationScrollPosition(activeConversationId);
      updateNearBottomState();
    });

    if (isHydrated) {
      composerRef.current?.focus();
    }
  }, [
    activeConversationId,
    activeMessages.length,
    isHydrated,
    rememberConversationScrollPosition,
    updateNearBottomState
  ]);

  function setMessageRef(messageId: string, element: HTMLElement | null) {
    messageRefs.current[messageId] = element;
  }

  function scrollToBottomOnNextRender() {
    pendingScrollBottom.current = true;
  }

  function scrollToBottom() {
    const viewport = messagesViewportRef.current;
    if (!viewport) {
      return;
    }

    viewport.scrollTo({ top: viewport.scrollHeight, behavior: 'smooth' });
  }

  return {
    messagesViewportRef,
    composerRef,
    isNearBottom,
    setMessageRef,
    rememberConversationScrollPosition,
    updateNearBottomState,
    scrollToBottomOnNextRender,
    scrollToBottom
  };
}
