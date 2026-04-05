import type { Metadata } from "next";
import Link from "next/link";
import "./globals.css";

export const metadata: Metadata = {
  title: "ft_transcendence",
  description: "Frontend for ft_transcendence",
};

const nav = [
  { href: "/", label: "Home" },
  { href: "/auth/login", label: "Login" },
  { href: "/guilds", label: "Guilds" },
  { href: "/chat", label: "Chat" },
  { href: "/notifications", label: "Notifications" },
  { href: "/profile", label: "Profile" },
];

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <body>
        <header className="border-b border-black/5 bg-white/70 backdrop-blur">
          <div className="container flex items-center justify-between py-5">
            <div className="text-lg font-semibold tracking-tight">
              ft_transcendence
            </div>
            <nav className="flex flex-wrap items-center gap-3 text-sm">
              {nav.map((item) => (
                <Link
                  key={item.href}
                  href={item.href}
                  className="rounded-full px-3 py-1.5 text-black/70 transition hover:bg-black/5 hover:text-black"
                >
                  {item.label}
                </Link>
              ))}
            </nav>
          </div>
        </header>
        <main className="container py-12">{children}</main>
      </body>
    </html>
  );
}
