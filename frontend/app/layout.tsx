import type { Metadata } from 'next';
import { Inter, JetBrains_Mono, Outfit } from 'next/font/google';
import { GuildProvider } from '../src/shared/guilds/guild-store';
import { CurrentUserProvider } from '../src/shared/user/user-store';
import './globals.css';

const inter = Inter({
  subsets: ['latin'],
  variable: '--font-inter',
  display: 'swap'
});

const outfit = Outfit({
  subsets: ['latin'],
  variable: '--font-outfit',
  display: 'swap'
});

const jetBrainsMono = JetBrains_Mono({
  subsets: ['latin'],
  variable: '--font-jetbrains-mono',
  display: 'swap'
});

export const metadata: Metadata = {
  title: 'ft_transcendence',
  description: 'Frontend for ft_transcendence'
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body className={`${inter.variable} ${outfit.variable} ${jetBrainsMono.variable}`}>
        <CurrentUserProvider>
          <GuildProvider>
            <main className="app-shell min-h-screen max-h-screen">{children}</main>
          </GuildProvider>
        </CurrentUserProvider>
      </body>
    </html>
  );
}
