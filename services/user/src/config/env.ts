type EnvInput = Record<string, string | undefined>;

export type AppEnv = 'development' | 'test' | 'production';

export interface ValidatedEnv {
  PORT: number;
  APP_ENV: AppEnv;
  DATABASE_URL: string;
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
  };
}
