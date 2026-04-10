type Service = "auth" | "guild" | "user" | "notification" | "chat";

const ENV_KEYS: Record<Service, string> = {
  auth: "NEXT_PUBLIC_API_AUTH_URL",
  guild: "NEXT_PUBLIC_API_GUILD_URL",
  user: "NEXT_PUBLIC_API_USER_URL",
  notification: "NEXT_PUBLIC_API_NOTIFICATION_URL",
  chat: "NEXT_PUBLIC_API_CHAT_URL",
};

export function getPublicApiBase(service: Service): string {
  const key = ENV_KEYS[service];
  const value = process.env[key];
  if (!value) {
    throw new Error(`Missing ${key}. Define it in .env (see .env.example).`);
  }
  return value.replace(/\/$/, "");
}
