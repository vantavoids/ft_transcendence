import { API_BASE_URL } from '../config/env';
import { getAccessToken, invalidateSession, setAccessToken } from '../lib/session';

export type Service = 'auth' | 'user' | 'guild' | 'chat' | 'notification';

export type ApiOptions = Omit<RequestInit, 'body'> & {
  body?: unknown;
  query?: Record<string, string | number | boolean | null | undefined>;
  // 401 on these is a real credential failure, not an expired token; don't retry
  skipAuthRefresh?: boolean;
};

export class ApiError extends Error {
  readonly service: Service;
  readonly status: number;
  readonly body: unknown;

  constructor(service: Service, status: number, message: string, body: unknown) {
    super(message);
    this.name = 'ApiError';
    this.service = service;
    this.status = status;
    this.body = body;
  }
}

function buildUrl(service: Service, path: string, query?: ApiOptions['query']): string {
  const cleanPath = path.startsWith('/') ? path : `/${path}`;
  let url = `${API_BASE_URL}/${service}/v1${cleanPath}`;

  if (query) {
    const params = new URLSearchParams();
    for (const [key, value] of Object.entries(query)) {
      if (value === undefined || value === null) continue;
      params.set(key, String(value));
    }
    const qs = params.toString();
    if (qs) url += `?${qs}`;
  }

  return url;
}

function performRequest(
  service: Service,
  path: string,
  options: ApiOptions,
  overrideToken?: string
): Promise<Response> {
  const { body, query, skipAuthRefresh: _skip, headers: rawHeaders, ...rest } = options;
  const headers = new Headers(rawHeaders);
  const token = overrideToken ?? getAccessToken();
  // FormData must be sent as-is: the browser sets Content-Type itself
  // (multipart/form-data with the right boundary) - JSON.stringify-ing it
  // or forcing application/json would both silently corrupt the upload.
  const isFormData = body instanceof FormData;

  if (token && !headers.has('Authorization')) {
    headers.set('Authorization', `Bearer ${token}`);
  }
  if (body !== undefined && !isFormData && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  return fetch(buildUrl(service, path, query), {
    ...rest,
    headers,
    credentials: 'include',
    body: body === undefined || isFormData ? body : JSON.stringify(body)
  });
}

// single-flight: concurrent 401s share one refresh instead of stampeding
let refreshInFlight: Promise<string | null> | null = null;

export function refreshAccessToken(): Promise<string | null> {
  if (!refreshInFlight) {
    refreshInFlight = (async () => {
      try {
        const res = await fetch(`${API_BASE_URL}/auth/v1/refresh`, {
          method: 'POST',
          credentials: 'include'
        });
        if (!res.ok) return null;
        const data = (await res.json()) as { access_token: string };
        setAccessToken(data.access_token);
        return data.access_token;
      } catch {
        return null;
      } finally {
        refreshInFlight = null;
      }
    })();
  }
  return refreshInFlight;
}

// fetches an already-authenticated, same-origin resource (e.g. a chat attachment
// URL the backend handed back) with the Bearer token attached and the same
// single-flight 401 refresh as apiFetch, returning the raw Response. apiFetch
// parses JSON/text, which is wrong for binary payloads like images, so callers
// that need a blob/stream use this instead.
export async function fetchAuthedResource(url: string, init?: RequestInit): Promise<Response> {
  const send = (token: string | null) => {
    const headers = new Headers(init?.headers);
    if (token && !headers.has('Authorization')) {
      headers.set('Authorization', `Bearer ${token}`);
    }
    return fetch(url, { ...init, headers, credentials: 'include' });
  };

  let res = await send(getAccessToken());
  if (res.status === 401) {
    const newToken = await refreshAccessToken();
    if (newToken) {
      res = await send(newToken);
    } else {
      invalidateSession();
    }
  }
  return res;
}

async function parseResponse<T>(service: Service, res: Response): Promise<T> {
  if (!res.ok) {
    let message = res.statusText;
    let body: unknown;
    const text = await res.text().catch(() => '');

    if (text) {
      try {
        body = JSON.parse(text);
        if (body && typeof body === 'object' && 'error' in body) {
          message = String((body as { error: unknown }).error);
        }
      } catch {
        message = text;
      }
    }

    throw new ApiError(service, res.status, message, body);
  }

  if (res.status === 204) {
    return undefined as T;
  }

  const contentType = res.headers.get('content-type') ?? '';
  if (contentType.includes('application/json')) {
    return (await res.json()) as T;
  }

  const text = await res.text();
  return (text || undefined) as T;
}

export async function apiFetch<T>(
  service: Service,
  path: string,
  options: ApiOptions = {}
): Promise<T> {
  let res = await performRequest(service, path, options);

  if (res.status === 401 && !options.skipAuthRefresh) {
    const newToken = await refreshAccessToken();
    if (newToken) {
      res = await performRequest(service, path, options, newToken);
    } else {
      invalidateSession();
    }
  }

  return parseResponse<T>(service, res);
}
