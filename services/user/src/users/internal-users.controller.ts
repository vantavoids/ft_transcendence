import {
  BadRequestException,
  Controller,
  Get,
  NotFoundException,
  Param,
} from '@nestjs/common';
import { ParseSnowflakePipe } from '../common/pipes/parse-snowflake.pipe';
import {
  RelationshipResponse,
  UsersService,
  UserDataExportResponse,
  UserProfileResponse,
} from './users.service';

@Controller('internal/users')
export class InternalUsersController {
  constructor(private readonly users: UsersService) {}

  @Get(':userId')
  async getUser(
    @Param('userId', ParseSnowflakePipe) userId: string,
  ): Promise<UserProfileResponse> {
    const profile = await this.users.getInternalProfile(userId);
    if (!profile) {
      throw new NotFoundException('User not found');
    }

    return profile;
  }

  @Get(':userId/relationship-with/:otherUserId')
  async getRelationship(
    @Param('userId', ParseSnowflakePipe) userId: string,
    @Param('otherUserId', ParseSnowflakePipe) otherUserId: string,
  ): Promise<RelationshipResponse> {
    if (userId === otherUserId) {
      throw new BadRequestException('userId must be different from otherUserId');
    }

    const relationship = await this.users.getRelationshipPerspective(userId, otherUserId);
    if (!relationship) {
      throw new NotFoundException('User not found');
    }

    return relationship;
  }

  @Get(':userId/data-export')
  async getDataExport(
    @Param('userId', ParseSnowflakePipe) userId: string,
  ): Promise<UserDataExportResponse> {
    return this.users.getInternalDataExport(userId);
  }

  // accepted-friend ids for the Chat Service's real-time fan-out (presence,
  // profile, social). ids are quoted strings to match the wire policy.
  @Get(':userId/friend-ids')
  async getFriendIds(
    @Param('userId', ParseSnowflakePipe) userId: string,
  ): Promise<string[]> {
    return this.users.listFriendIds(userId);
  }
}
