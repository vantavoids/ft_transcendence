'use client';

import { useEffect } from 'react';

export function isEscapeKey(event: KeyboardEvent | React.KeyboardEvent) {
  return event.key === 'Escape' || event.key === 'Esc' || event.code === 'Escape';
}

export function useCloseOnEscape(onClose: () => void) {
  useEffect(() => {
    function handleEscape(event: KeyboardEvent) {
      if (!isEscapeKey(event)) {
        return;
      }

      onClose();
    }

    window.addEventListener('keydown', handleEscape);
    return () => window.removeEventListener('keydown', handleEscape);
  }, [onClose]);
}
