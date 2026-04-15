import type { Metadata } from 'next';
import './globals.css';

export const metadata: Metadata = {
  title: 'ft_transcendence',
  description: 'Frontend for ft_transcendence'
};

const nav = [
  { href: '/', label: 'Home' },
  { href: '/auth/login', label: 'Login' },
  { href: '/guilds', label: 'Guilds' },
  { href: '/chat', label: 'Chat' },
  { href: '/notifications', label: 'Notifications' },
  { href: '/profile', label: 'Profile' }
];

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>
        <main className="bg-primary-bg min-h-screen flex justify-center items-center">
          {children}
        </main>
      </body>
    </html>
  );
}
