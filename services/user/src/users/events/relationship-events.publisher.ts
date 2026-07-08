import { Injectable } from '@nestjs/common';
import { FriendRequestSentEvent } from '../users.types';

export abstract class RelationshipEventsPublisher {
  abstract publishFriendRequestSent(
    event: FriendRequestSentEvent,
  ): Promise<void>;
}

@Injectable()
export class NoopRelationshipEventsPublisher
  implements RelationshipEventsPublisher
{
  async publishFriendRequestSent(): Promise<void> {
    return;
  }
}
