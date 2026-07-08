import { Injectable } from '@nestjs/common';
import { SnowflakeIdGenerator } from '../common/snowflake-id.generator';
import { RelationshipEventsPublisher } from './events/relationship-events.publisher';
import { FriendshipsRepository } from './repositories/friendships.repository';
import { ProfilesRepository } from './repositories/profiles.repository';
import { RelationshipsRepository } from './repositories/relationships.repository';
import { UsersLookupRepository } from './repositories/users-lookup.repository';
import type {
  FriendRequestDirection,
  FriendRequestListItemResponse,
  FriendRequestSentEvent,
  FriendSummaryResponse,
  FriendshipResponse,
  RelationshipResponse,
  UpdateUserProfileInput,
  UserProfileResponse,
  UserSummaryResponse,
} from './users.types';
export {
  type BlockListItemResponse,
  type FriendRequestDirection,
  type FriendRequestListItemResponse,
  type FriendRequestSentEvent,
  type FriendSummaryResponse,
  type FriendshipResponse,
  type RelationshipStatus,
  type RelationshipResponse,
  type UpdateUserProfileInput,
  type UserProfileResponse,
  type UserSummaryResponse,
} from './users.types';

@Injectable()
export class UsersService {
  constructor(
  private readonly profilesRepository: ProfilesRepository,
  private readonly relationshipsRepository: RelationshipsRepository,
  private readonly usersLookupRepository: UsersLookupRepository,
  private readonly friendshipsRepository: FriendshipsRepository,
  private readonly snowflakeIdGenerator: SnowflakeIdGenerator,
  private readonly relationshipEventsPublisher: RelationshipEventsPublisher,
) {}

  async getInternalProfile(userId: string): Promise<UserProfileResponse | null> {
    return this.profilesRepository.getProfileById(userId);
  }

  async getUsersByIds(
    viewerId: string,
    userIds: string[],
  ): Promise<UserSummaryResponse[]> {
    return this.usersLookupRepository.getUsersByIds(viewerId, userIds);
  }

  async searchUsers(
    viewerId: string,
    query: string,
    limit: number,
  ): Promise<UserSummaryResponse[]> {
    return this.usersLookupRepository.searchUsers(viewerId, query, limit);
  }

  async updateInternalProfile(
    userId: string,
    changes: UpdateUserProfileInput,
  ): Promise<UserProfileResponse | null> {
    return this.profilesRepository.updateProfileById(userId, changes);
  }

  async getRelationshipPerspective(
    callerId: string,
    otherUserId: string,
  ): Promise<RelationshipResponse | null> {
    return this.relationshipsRepository.getRelationshipPerspective(
      callerId,
      otherUserId,
    );
  }

  async listFriends(
    viewerId: string,
    userId: string,
  ): Promise<FriendSummaryResponse[]> {
    return this.friendshipsRepository.listFriends(viewerId, userId);
  }

  async listFriendRequests(
    viewerId: string,
    direction: FriendRequestDirection,
  ): Promise<FriendRequestListItemResponse[]> {
    return this.friendshipsRepository.listFriendRequests(viewerId, direction);
  }

  async createFriendRequest(
    requesterId: string,
    addresseeId: string,
  ): Promise<FriendshipResponse | 'not_found' | 'conflict'> {
    const friendshipId = this.snowflakeIdGenerator.nextId();
    const created = await this.friendshipsRepository.createFriendRequest(
      friendshipId,
      requesterId,
      addresseeId,
    );

    if (
      created !== 'not_found' &&
      created !== 'conflict'
    ) {
      await this.relationshipEventsPublisher.publishFriendRequestSent({
        friendship_id: created.id,
        requester_id: created.requester_id,
        addressee_id: created.addressee_id,
      } satisfies FriendRequestSentEvent);
    }

    return created;
  }

  async updateFriendRequest(
    friendshipId: string,
    callerId: string,
    status: 'accepted' | 'blocked',
  ): Promise<FriendshipResponse | 'not_found' | 'forbidden' | 'conflict'> {
    return this.friendshipsRepository.updateFriendRequest(
      friendshipId,
      callerId,
      status,
    );
  }

  async deleteFriendRequest(
    friendshipId: string,
    callerId: string,
  ): Promise<'not_found' | 'forbidden' | 'deleted'> {
    return this.friendshipsRepository.deleteFriendRequest(friendshipId, callerId);
  }
}
