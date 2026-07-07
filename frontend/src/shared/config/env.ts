// single api entrypoint; relative by default so it stays same-origin behind nginx
export const API_BASE_URL = (process.env.NEXT_PUBLIC_API_URL ?? "/api").replace(/\/$/, "");
