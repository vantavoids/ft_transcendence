import { Injectable } from '@nestjs/common';
import { ProfilesRepository } from './repositories/profiles.repository';
import { RelationshipsRepository } from './repositories/relationships.repository';
import { UsersLookupRepository } from './repositories/users-lookup.repository';
import type {
  RelationshipResponse,
  UpdateUserProfileInput,
  UserProfileResponse,
  UserSummaryResponse,
} from './users.types';
export {
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
}
