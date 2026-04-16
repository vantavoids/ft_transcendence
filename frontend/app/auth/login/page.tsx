export default function LoginPage() {
  return (
    <section className="card p-8">
      <h1 className="text-3xl font-semibold">Login</h1>
      <p className="mt-2 text-sm text-black/60">
        Placeholder login screen. Wire this to Auth service.
      </p>
      <div className="mt-6 grid gap-4">
        <input
          className="rounded-xl border border-black/10 px-4 py-3 text-sm"
          placeholder="Email"
          type="email"
        />
        <input
          className="rounded-xl border border-black/10 px-4 py-3 text-sm"
          placeholder="Password"
          type="password"
        />
        <button className="rounded-xl bg-accent px-4 py-3 text-sm font-semibold text-white">
          Sign in
        </button>
      </div>
    </section>
  );
}
