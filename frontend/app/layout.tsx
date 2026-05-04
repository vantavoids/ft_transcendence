import type { Metadata } from 'next';
import './globals.css';

export const metadata: Metadata = {
  title: 'ft_transcendence',
  description: 'Frontend for ft_transcendence'
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>
        <main className="app-shell min-h-screen">
          {children}
        </main>
      </body>
    </html>
  );
}
