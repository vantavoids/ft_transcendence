import { Injectable } from '@nestjs/common';
import { FriendAcceptedEvent, FriendRequestSentEvent } from '../users.types';

export abstract class RelationshipEventsPublisher {
  abstract publishFriendRequestSent(
    event: FriendRequestSentEvent,
  ): Promise<void>;
  abstract publishFriendAccepted(event: FriendAcceptedEvent): Promise<void>;
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
}
