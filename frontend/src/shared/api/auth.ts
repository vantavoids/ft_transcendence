export type AuthPayload = {
  username: string;
  password: string;
};

export type RegisterPayload = AuthPayload & {
  confirm: string;
};

type FakeAuthResponse = {
  username: string;
};

async function simulateAuth<T>(result: T): Promise<T> {
  await new Promise((resolve) => setTimeout(resolve, 150));
  return result;
}

export async function login(payload: AuthPayload) {
  return simulateAuth<FakeAuthResponse>({
    username: payload.username.trim()
  });
}

export async function register(payload: RegisterPayload) {
  return simulateAuth<FakeAuthResponse>({
    username: payload.username.trim()
  });
}
