'use client';

import { createContext, useContext, useEffect, useMemo, useRef, useState } from 'react';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel
} from '@microsoft/signalr';
import { API_BASE_URL } from '../config/env';
import { refreshAccessToken } from '../api/client';
import { getAccessToken, hasSession, invalidateSession } from '../lib/session';

export type SignalingHubStatus = 'disconnected' | 'connecting' | 'connected';

type HubHandler = (...args: unknown[]) => void;

type SignalingHubContextValue = {
  status: SignalingHubStatus;
  on: (event: string, handler: HubHandler) => void;
  off: (event: string, handler: HubHandler) => void;
  invoke: (method: string, ...args: unknown[]) => Promise<void>;
};

const SignalingHubContext = createContext<SignalingHubContextValue | null>(null);

// The WebRTC signaling methods (CallOffer/CallAnswer/IceCandidate/...) live on the
// chat service's SignalingHub, mapped at /v1/hubs/signaling (separate from the
// messaging ChatHub at /v1/hubs/chat). SignalR sends the token as ?access_token=
// for the WebSocket handshake and as a Bearer header on the negotiate POST;
// accessTokenFactory refreshes when no token is cached.
function buildConnection(): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(`${API_BASE_URL}/chat/v1/hubs/signaling`, {
      accessTokenFactory: async () => getAccessToken() ?? (await refreshAccessToken()) ?? ''
    })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build();
}

function isAuthFailure(error: unknown): boolean {
  const message = String(error).toLowerCase();
  return message.includes('401') || message.includes('unauthorized');
}

// Decode the JWT `exp` (no verification; the gateway is the authority). Treated as
// expired 5s early to avoid a doomed negotiate. Non-JWT / no exp -> assume valid.
function isTokenExpired(token: string | null): boolean {
  if (!token) {
    return true;
  }
  try {
    const payload = token.split('.')[1];
    const decoded = JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/')));
    return typeof decoded.exp === 'number' && decoded.exp * 1000 <= Date.now() + 5000;
  } catch {
    return false;
  }
}

// The /chat page is the first authenticated request after login, so the cached
// access token may already be expired. Refresh proactively to avoid a failed
// negotiate (which SignalR would log as a console error), and retry once as a
// fallback if the server still rejects it.
async function startWithRefresh(connection: HubConnection): Promise<void> {
  if (isTokenExpired(getAccessToken())) {
    const refreshed = await refreshAccessToken();
    if (!refreshed) {
      invalidateSession();
      throw new Error('Failed to refresh signaling hub session.');
    }
  }
  try {
    await connection.start();
  } catch (error) {
    if (isAuthFailure(error)) {
      const refreshed = await refreshAccessToken();
      if (refreshed) {
        await connection.start();
        return;
      }
      invalidateSession();
    }
    throw error;
  }
}

export function SignalingHubProvider({ children }: { children: React.ReactNode }) {
  const connectionRef = useRef<HubConnection | null>(null);
  const [status, setStatus] = useState<SignalingHubStatus>('disconnected');

  useEffect(() => {
    if (!hasSession()) {
      return;
    }

    const connection = buildConnection();
    connectionRef.current = connection;
    let cancelled = false;

    connection.onreconnecting(() => {
      if (!cancelled) setStatus('connecting');
    });
    connection.onreconnected(() => {
      if (!cancelled) setStatus('connected');
    });
    connection.onclose(() => {
      if (!cancelled) setStatus('disconnected');
    });

    setStatus('connecting');
    const startPromise = startWithRefresh(connection)
      .then(() => {
        if (!cancelled) {
          setStatus('connected');
        }
      })
      .catch(() => {
        // hub unreachable (backend down / expired session); stay disconnected, no throw
        if (!cancelled) {
          setStatus('disconnected');
        }
      });

    return () => {
      cancelled = true;
      connectionRef.current = null;
      // wait for the start attempt to settle before stopping, so we never abort an
      // in-flight negotiation (React StrictMode double-mounts this effect in dev)
      void startPromise.finally(() => connection.stop());
    };
  }, []);

  const value = useMemo<SignalingHubContextValue>(
    () => ({
      status,
      on: (event, handler) => connectionRef.current?.on(event, handler),
      off: (event, handler) => connectionRef.current?.off(event, handler),
      invoke: async (method, ...args) => {
        const connection = connectionRef.current;
        if (!connection || connection.state !== HubConnectionState.Connected) {
          throw new Error(`signaling hub not connected (cannot invoke ${method})`);
        }
        await connection.invoke(method, ...args);
      }
    }),
    [status]
  );

  return <SignalingHubContext.Provider value={value}>{children}</SignalingHubContext.Provider>;
}

export function useSignalingHub(): SignalingHubContextValue {
  const context = useContext(SignalingHubContext);
  if (!context) {
    throw new Error('useSignalingHub must be used within a SignalingHubProvider');
  }
  return context;
}
