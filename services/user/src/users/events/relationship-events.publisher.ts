import { Injectable } from '@nestjs/common';
import {
  FriendAcceptedEvent,
  FriendRemovedEvent,
  FriendRequestSentEvent,
} from '../users.types';

export abstract class RelationshipEventsPublisher {
  abstract publishFriendRequestSent(
    event: FriendRequestSentEvent,
  ): Promise<void>;
  abstract publishFriendAccepted(event: FriendAcceptedEvent): Promise<void>;
  abstract publishFriendRemoved(event: FriendRemovedEvent): Promise<void>;
}

@Injectable()
export class NoopRelationshipEventsPublisher
  implements RelationshipEventsPublisher
{
  async publishFriendRequestSent(): Promise<void> {
    return;
  }

  async publishFriendAccepted(): Promise<void> {
    return;
  }

  async publishFriendRemoved(): Promise<void> {
    return;
  }
}
