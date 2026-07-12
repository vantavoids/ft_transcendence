import type { Metadata } from 'next';
import type { CSSProperties, ReactNode } from 'react';
import { AppProviders } from '../src/components/app-providers';
import './globals.css';

export const metadata: Metadata = {
  title: 'ft_transcendence',
  description: 'Frontend for ft_transcendence'
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="en">
      <body
        style={
          {
            '--font-inter': 'system-ui',
            '--font-outfit': 'system-ui',
            '--font-jetbrains-mono': 'ui-monospace'
          } as CSSProperties
        }
      >
        <AppProviders>
          <main className="app-shell min-h-screen max-h-screen">{children}</main>
        </AppProviders>
      </body>
    </html>
  );
}
