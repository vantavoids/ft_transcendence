import Image from 'next/image';
import { getAccentClasses, type ChatMessageData } from './chat-message';

export type PresenceStatus = 'online' | 'idle' | 'offline';

export function getDmStatusClasses(status: PresenceStatus) {
  switch (status) {
    case 'online':
      return 'bg-lime';
    case 'idle':
      return 'bg-yellow';
    default:
      return 'bg-muted';
  }
}

const SIZE_CONFIG = {
  sm: {
    dimension: 40,
    dimensionClassName: 'h-10 w-10',
    avatarClassName: 'h-10 w-10 text-sm font-bold',
    dotClassName: 'h-3.5 w-3.5 -bottom-0.5 -right-0.5 border-2'
  },
  md: {
    dimension: 44,
    dimensionClassName: 'h-11 w-11',
    avatarClassName: 'h-11 w-11 text-sm font-bold',
    dotClassName: 'h-3.5 w-3.5 -bottom-0.5 -right-0.5 border-2'
  },
  lg: {
    dimension: 80,
    dimensionClassName: 'h-20 w-20',
    avatarClassName: 'h-20 w-20 text-3xl font-bold border-4 border-secondary-bg',
    dotClassName: 'h-4 w-4 bottom-1 right-1 border-2'
  }
} as const;

type AvatarWithStatusProps = {
  name: string;
  accent: ChatMessageData['accent'];
  status: PresenceStatus;
  size?: keyof typeof SIZE_CONFIG;
  avatarUrl?: string | null;
};

// the accent-initial bubble + online-status dot pairing used for DMs,
// friends, guild members, and profile cards - identical shape, only the
// size (and whether a real avatar image is available) differs per caller
export function AvatarWithStatus({
  name,
  accent,
  status,
  size = 'md',
  avatarUrl
}: AvatarWithStatusProps) {
  const { dimension, dimensionClassName, avatarClassName, dotClassName } = SIZE_CONFIG[size];

  return (
    <span className="relative shrink-0">
      {avatarUrl ? (
        <Image
          src={avatarUrl}
          alt=""
          width={dimension}
          height={dimension}
          unoptimized
          className={`rounded-full object-cover ${dimensionClassName}`}
        />
      ) : (
        <span
          className={`flex items-center justify-center rounded-full ${avatarClassName} ${getAccentClasses(accent)}`}
        >
          {name.slice(0, 1).toUpperCase()}
        </span>
      )}
      <span
        className={`absolute rounded-full border-secondary-bg ${dotClassName} ${getDmStatusClasses(status)}`}
      />
    </span>
  );
}
