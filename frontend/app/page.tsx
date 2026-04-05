export default function HomePage() {
  return (
    <section className="grid gap-8 md:grid-cols-[1.1fr_0.9fr]">
      <div className="card p-8">
        <p className="text-sm font-medium uppercase tracking-[0.2em] text-black/50">
          Pong • Social • Guilds
        </p>
        <h1 className="mt-4 text-4xl font-semibold leading-tight md:text-5xl">
          Welcome to ft_transcendence
        </h1>
        <p className="mt-4 text-base text-black/70">
          This is the Next.js + TypeScript scaffold with Tailwind and basic
          routing. Plug in your services, auth, and realtime modules here.
        </p>
        <div className="mt-6 flex flex-wrap gap-3">
          <button className="rounded-full bg-accent px-5 py-2 text-sm font-semibold text-white">
            Get Started
          </button>
          <button className="rounded-full border border-black/10 px-5 py-2 text-sm font-semibold text-black/70">
            View Services
          </button>
        </div>
      </div>
      <div className="card p-8">
        <h2 className="text-xl font-semibold">Service Status</h2>
        <ul className="mt-4 space-y-3 text-sm text-black/70">
          <li>Auth: /healthz</li>
          <li>Guild: /healthz</li>
          <li>User: /hello-world</li>
          <li>Notification: /notifications/hello-world</li>
        </ul>
        <div className="mt-6 rounded-xl bg-fog p-4 text-xs text-black/60">
          Replace these placeholders with real API checks when services are
          wired.
        </div>
      </div>
    </section>
  );
}
