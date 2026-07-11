type EnvInput = Record<string, string | undefined>;

export type AppEnv = 'development' | 'test' | 'production';

export interface ValidatedEnv {
  PORT: number;
  APP_ENV: AppEnv;
  DATABASE_URL: string;
  JWT_SECRET: string;
  BASE_URL: string;
  MINIO_ENDPOINT: string;
  MINIO_PUBLIC_ENDPOINT: string;
  MINIO_ACCESS_KEY: string;
  MINIO_SECRET_KEY: string;
  MINIO_USER_BUCKET: string;
  MINIO_EXPORTS_BUCKET: string;
  AUTH_INTERNAL_URL: string;
  GUILD_INTERNAL_URL: string;
  CHAT_INTERNAL_URL: string;
  NOTIFICATION_INTERNAL_URL: string;
}

function required(name: string, value: string | undefined): string {
  if (!value || value.trim() === '') {
    throw new Error(`${name} is missing`);
  }

  return value;
}

export function validateEnv(env: EnvInput): ValidatedEnv {
  const portRaw = env.PORT ?? '8080';
  const port = Number(portRaw);
  if (!Number.isInteger(port) || port <= 0) {
    throw new Error(`PORT must be a positive integer, got ${portRaw}`);
  }

  const appEnvRaw = env.APP_ENV ?? 'production';
  if (!['development', 'test', 'production'].includes(appEnvRaw)) {
    throw new Error(`APP_ENV must be development, test, or production`);
  }

  return {
    PORT: port,
    APP_ENV: appEnvRaw as AppEnv,
    DATABASE_URL: required('DATABASE_URL', env.DATABASE_URL),
    JWT_SECRET: required('JWT_SECRET', env.JWT_SECRET),
    BASE_URL: required('BASE_URL', env.BASE_URL),
    MINIO_ENDPOINT: required('MINIO_ENDPOINT', env.MINIO_ENDPOINT),
    MINIO_PUBLIC_ENDPOINT: required(
      'MINIO_PUBLIC_ENDPOINT',
      env.MINIO_PUBLIC_ENDPOINT,
    ),
    MINIO_ACCESS_KEY: required('MINIO_ACCESS_KEY', env.MINIO_ACCESS_KEY),
    MINIO_SECRET_KEY: required('MINIO_SECRET_KEY', env.MINIO_SECRET_KEY),
    MINIO_USER_BUCKET: required('MINIO_USER_BUCKET', env.MINIO_USER_BUCKET),
    MINIO_EXPORTS_BUCKET: required(
      'MINIO_EXPORTS_BUCKET',
      env.MINIO_EXPORTS_BUCKET,
    ),
    AUTH_INTERNAL_URL: required(
      'AUTH_INTERNAL_URL',
      env.AUTH_INTERNAL_URL,
    ),
    GUILD_INTERNAL_URL: required(
      'GUILD_INTERNAL_URL',
      env.GUILD_INTERNAL_URL,
    ),
    CHAT_INTERNAL_URL: required(
      'CHAT_INTERNAL_URL',
      env.CHAT_INTERNAL_URL,
    ),
    NOTIFICATION_INTERNAL_URL: required(
      'NOTIFICATION_INTERNAL_URL',
      env.NOTIFICATION_INTERNAL_URL,
    ),
  };
}
