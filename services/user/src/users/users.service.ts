import { Injectable } from '@nestjs/common';
import { SnowflakeIdGenerator } from '../common/snowflake-id.generator';
import { RelationshipEventsPublisher } from './events/relationship-events.publisher';
import { ProfileMediaStorageService, type ProfileMediaKind, type UploadFile } from './media/profile-media.storage';
import { BlocksRepository } from './repositories/blocks.repository';
import { FriendshipsRepository } from './repositories/friendships.repository';
import { ProfileMediaRepository } from './repositories/profile-media.repository';
import { ProfilesRepository } from './repositories/profiles.repository';
import { RelationshipsRepository } from './repositories/relationships.repository';
import { UsersLookupRepository } from './repositories/users-lookup.repository';
import type {
  BlockListItemResponse,
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
  private readonly blocksRepository: BlocksRepository,
  private readonly profileMediaRepository: ProfileMediaRepository,
  private readonly profileMediaStorageService: ProfileMediaStorageService,
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

  async uploadAvatar(
    userId: string,
    file: UploadFile,
  ): Promise<string | 'not_found'> {
    return this.uploadProfileMedia('avatar', userId, file);
  }

  async deleteAvatar(userId: string): Promise<'deleted' | 'not_found'> {
    return this.deleteProfileMedia('avatar', userId);
  }

  async uploadBanner(
    userId: string,
    file: UploadFile,
  ): Promise<string | 'not_found'> {
    return this.uploadProfileMedia('banner', userId, file);
  }

  async deleteBanner(userId: string): Promise<'deleted' | 'not_found'> {
    return this.deleteProfileMedia('banner', userId);
  }

  private async uploadProfileMedia(
    kind: ProfileMediaKind,
    userId: string,
    file: UploadFile,
  ): Promise<string | 'not_found'> {
    const media = await this.profileMediaRepository.getMediaById(userId);
    if (!media) {
      return 'not_found';
    }

    const uploadId = this.snowflakeIdGenerator.nextId();
    const url = await this.profileMediaStorageService.upload(
      kind,
      userId,
      uploadId,
      file,
    );

    const updated = await this.setProfileMediaUrl(kind, userId, url);
    if (!updated) {
      void this.profileMediaStorageService
        .delete(kind, userId, uploadId)
        .catch(() => undefined);
      return 'not_found';
    }

    const previousUrl = this.getCurrentMediaUrl(kind, media);
    if (previousUrl) {
      const previousKey = this.profileMediaStorageService.extractKeyFromUrl(
        previousUrl,
        kind,
        userId,
      );
      if (previousKey) {
        void this.profileMediaStorageService
          .deleteByKey(previousKey)
          .catch(() => undefined);
      }
    }

    return url;
  }

  private async deleteProfileMedia(
    kind: ProfileMediaKind,
    userId: string,
  ): Promise<'deleted' | 'not_found'> {
    const media = await this.profileMediaRepository.getMediaById(userId);
    if (!media) {
      return 'not_found';
    }

    const currentUrl = this.getCurrentMediaUrl(kind, media);
    if (!currentUrl) {
      return 'not_found';
    }

    const cleared = await this.clearProfileMediaUrl(kind, userId);
    if (!cleared) {
      return 'not_found';
    }

    const key = this.profileMediaStorageService.extractKeyFromUrl(
      currentUrl,
      kind,
      userId,
    );
    if (key) {
      void this.profileMediaStorageService
        .deleteByKey(key)
        .catch(() => undefined);
    }

    return 'deleted';
  }

  private async setProfileMediaUrl(
    kind: ProfileMediaKind,
    userId: string,
    url: string,
  ): Promise<boolean> {
    if (kind === 'avatar') {
      return this.profileMediaRepository.setAvatarUrl(userId, url);
    }

    return this.profileMediaRepository.setBannerUrl(userId, url);
  }

  private async clearProfileMediaUrl(
    kind: ProfileMediaKind,
    userId: string,
  ): Promise<boolean> {
    if (kind === 'avatar') {
      return this.profileMediaRepository.clearAvatarUrl(userId);
    }

    return this.profileMediaRepository.clearBannerUrl(userId);
  }

  private getCurrentMediaUrl(
    kind: ProfileMediaKind,
    media: { avatar_url: string | null; banner_url: string | null },
  ): string | null {
    return kind === 'avatar' ? media.avatar_url : media.banner_url;
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

  async listBlockedUsers(viewerId: string): Promise<BlockListItemResponse[]> {
    return this.blocksRepository.listBlockedUsers(viewerId);
  }

  async blockUser(
    viewerId: string,
    blockedId: string,
  ): Promise<'not_found' | 'conflict' | 'blocked'> {
    return this.blocksRepository.blockUser(viewerId, blockedId);
  }

  async unblockUser(
    viewerId: string,
    blockedId: string,
  ): Promise<'not_found' | 'deleted'> {
    return this.blocksRepository.unblockUser(viewerId, blockedId);
  }
}
