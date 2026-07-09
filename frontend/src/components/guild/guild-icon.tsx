'use client';

import Image from 'next/image';

const ACCENTS = [
  'bg-aqua/15 text-aqua',
  'bg-lime/15 text-lime',
  'bg-yellow/15 text-yellow',
  'bg-pink/15 text-pink',
  'bg-lavender/15 text-lavender'
];

export function getGuildAccentClasses(guildId: string) {
  let hash = 0;
  for (const char of guildId) {
    hash = (hash * 31 + char.charCodeAt(0)) % ACCENTS.length;
  }

  return ACCENTS[hash];
}

type GuildIconProps = {
  guildId: string;
  name: string;
  iconUrl?: string | null;
  className?: string;
};

export function GuildIcon({ guildId, name, iconUrl, className = '' }: GuildIconProps) {
  if (iconUrl) {
    return (
      <Image
        src={iconUrl}
        alt=""
        width={160}
        height={160}
        unoptimized
        className={`object-cover ${className}`}
      />
    );
  }

  return (
    <span
      className={`flex items-center justify-center font-bold ${getGuildAccentClasses(
        guildId
      )} ${className}`}
    >
      {name.slice(0, 1).toUpperCase()}
    </span>
  );
}
