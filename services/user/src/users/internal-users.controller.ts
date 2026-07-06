import {
  BadRequestException,
  Controller,
  Get,
  NotFoundException,
  Param,
} from '@nestjs/common';
import { ParseSnowflakePipe } from '../common/pipes/parse-snowflake.pipe';
import { RelationshipResponse, UsersService, UserProfileResponse } from './users.service';

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
}
