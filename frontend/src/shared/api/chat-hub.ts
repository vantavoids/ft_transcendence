import * as signalR from '@microsoft/signalr';
import { API_BASE_URL } from '../config/env';
import { getAccessToken } from '../lib/session';
import type { SendChannelMessageResponse, SendDirectMessageResponse } from './chat';

export type MessageEditedEvent = {
  id: string;
  channel_id: string;
  content: string;
  edited_at: string;
};

export type MessageDeletedEvent = {
  message_id: string;
  channel_id: string;
};

export type DirectMessageEditedEvent = {
  id: string;
  conversation_id: string;
  content: string;
  edited_at: string;
};

export type DirectMessageDeletedEvent = {
  message_id: string;
  conversation_id: string;
};

export type ReactionEvent = {
  message_id: string;
  channel_id: string;
  user_id: string;
  emoji: string;
  count: number;
};

export type ReadStateUpdatedEvent = {
  channel_id: string;
  last_read_message_id: string | null;
  unread_count: number;
};

export type DmReadStateUpdatedEvent = {
  partner_id: string;
  last_read_message_id: string;
  unread_count: number;
};

export type TypingStartedEvent = {
  user_id: string;
  scope: 'channel' | 'dm';
  id: string;
  expires_at: string;
};

export type UserPresenceEvent = {
  user_id: string;
  status: 'online' | 'idle' | 'dnd' | 'offline';
};

export type GuildJoinedEvent = {
  guild_id: string;
  guild_name: string;
};

export type GuildLeftEvent = {
  guild_id: string;
};

export type ChatHubErrorEvent = {
  code: string;
  message: string;
};

export type ChatHubEvents = {
  ReceiveMessage: (event: SendChannelMessageResponse) => void;
  ReceiveDirectMessage: (event: SendDirectMessageResponse) => void;
  MessageEdited: (event: MessageEditedEvent) => void;
  MessageDeleted: (event: MessageDeletedEvent) => void;
  DirectMessageEdited: (event: DirectMessageEditedEvent) => void;
  DirectMessageDeleted: (event: DirectMessageDeletedEvent) => void;
  ReactionAdded: (event: ReactionEvent) => void;
  ReactionRemoved: (event: ReactionEvent) => void;
  ReadStateUpdated: (event: ReadStateUpdatedEvent) => void;
  DmReadStateUpdated: (event: DmReadStateUpdatedEvent) => void;
  TypingStarted: (event: TypingStartedEvent) => void;
  UserPresence: (event: UserPresenceEvent) => void;
  GuildJoined: (event: GuildJoinedEvent) => void;
  GuildLeft: (event: GuildLeftEvent) => void;
  Error: (event: ChatHubErrorEvent) => void;
};

// signalR's connection.onreconnected() has no matching "off" - it only ever
// accumulates handlers - so route through one registration and our own
// removable Set instead of calling onreconnected() per subscriber.
const reconnectHandlers = new Set<() => void>();

function buildConnection(): signalR.HubConnection {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${API_BASE_URL}/chat/v1/hubs/chat`, {
      accessTokenFactory: () => getAccessToken() ?? ''
    })
    .withAutomaticReconnect()
    .build();

  connection.onreconnected(() => {
    reconnectHandlers.forEach((handler) => handler());
  });

  // no dedicated UI for hub-level errors
  connection.on('Error', (event: ChatHubErrorEvent) => {
    console.error(`[chat-hub] ${event.code}: ${event.message}`);
  });

  return connection;
}

let connectionPromise: Promise<signalR.HubConnection> | null = null;

// one connection for the whole tab's chat session - not tied to any single
// component's lifecycle, so switching channels/DMs doesn't reconnect.
function getChatHubConnection(): Promise<signalR.HubConnection> {
  if (!connectionPromise) {
    const connection = buildConnection();
    connectionPromise = connection.start().then(() => connection);
  }
  return connectionPromise;
}

export function stopChatHub(): void {
  if (connectionPromise) {
    connectionPromise.then((connection) => connection.stop()).catch(() => {});
    connectionPromise = null;
  }
}

export async function joinChatChannel(channelId: string): Promise<void> {
  const connection = await getChatHubConnection();
  await connection.invoke('JoinChannel', channelId);
}

export async function leaveChatChannel(channelId: string): Promise<void> {
  const connection = await getChatHubConnection();
  await connection.invoke('LeaveChannel', channelId);
}

export function onChatHubEvent<K extends keyof ChatHubEvents>(
  event: K,
  handler: ChatHubEvents[K]
): () => void {
  let cancelled = false;
  let subscribedConnection: signalR.HubConnection | null = null;
  const boundHandler = handler as (...args: unknown[]) => void;

  getChatHubConnection().then((connection) => {
    if (cancelled) {
      return;
    }
    subscribedConnection = connection;
    connection.on(event, boundHandler);
  });

  return () => {
    cancelled = true;
    subscribedConnection?.off(event, boundHandler);
  };
}

// re-join whatever channel the client cares about after the connection drops
// and comes back - SignalR groups don't survive a reconnect since it's a new
// connection id server-side (docs/frontend-backend-backlog.md: "reconnect
// cleanly after transport loss").
export function onChatHubReconnected(handler: () => void): () => void {
  reconnectHandlers.add(handler);
  return () => {
    reconnectHandlers.delete(handler);
  };
}
