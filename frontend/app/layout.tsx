import type { Metadata } from 'next';
import { Inter, JetBrains_Mono, Outfit } from 'next/font/google';
import { AppProviders } from '../src/components/app-providers';
import './globals.css';

const inter = Inter({
  subsets: ['latin'],
  variable: '--font-inter',
  display: 'swap'
});

const outfit = Outfit({
  subsets: ['latin'],
  variable: '--font-outfit',
  display: 'swap',
  preload: false
});

const jetBrainsMono = JetBrains_Mono({
  subsets: ['latin'],
  variable: '--font-jetbrains-mono',
  display: 'swap',
  preload: false
});

export const metadata: Metadata = {
  title: 'ft_transcendence',
  description: 'Frontend for ft_transcendence'
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body className={`${inter.variable} ${outfit.variable} ${jetBrainsMono.variable}`}>
        <AppProviders>
          <main className="app-shell min-h-screen max-h-screen">{children}</main>
        </AppProviders>
      </body>
    </html>
  );
}
