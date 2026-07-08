import {
  Body,
  Controller,
  BadRequestException,
  ForbiddenException,
  Get,
  NotFoundException,
  Param,
  Query,
  Patch,
  UseGuards,
} from '@nestjs/common';
import { ParseSnowflakePipe } from '../common/pipes/parse-snowflake.pipe';
import { CurrentUserId } from '../auth/current-user.decorator';
import { JwtAuthGuard } from '../auth/jwt-auth.guard';
import {
  UserProfileResponse,
  UserSummaryResponse,
  UsersService,
} from './users.service';
import { UpdateUserProfileDto } from './update-user-profile.dto';
import {
  ListUsersQueryDto,
  SearchUsersQueryDto,
} from './users-query.dto';

@Controller('v1/users')
@UseGuards(JwtAuthGuard)
export class PublicUsersController {
  constructor(private readonly users: UsersService) {}

  @Get('me')
  async getMe(@CurrentUserId() userId: string): Promise<UserProfileResponse> {
    const profile = await this.users.getInternalProfile(userId);
    if (!profile) {
      throw new NotFoundException('User not found');
    }

    return profile;
  }

  @Get()
  async getUsers(
    @CurrentUserId() userId: string,
    @Query() query: ListUsersQueryDto,
  ): Promise<UserSummaryResponse[]> {
    const ids = this.parseSnowflakeList(query.ids);
    return this.users.getUsersByIds(userId, ids);
  }

  @Get('search')
  async searchUsers(
    @CurrentUserId() userId: string,
    @Query() query: SearchUsersQueryDto,
  ): Promise<UserSummaryResponse[]> {
    const searchTerm = query.q.trim();
    if (searchTerm.length < 2) {
      throw new BadRequestException('q must be at least 2 characters long');
    }

    const limit = this.parseSearchLimit(query.limit);
    return this.users.searchUsers(userId, searchTerm, limit);
  }

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

  @Patch(':userId')
  async updateUser(
    @CurrentUserId() currentUserId: string,
    @Param('userId', ParseSnowflakePipe) userId: string,
    @Body() body: UpdateUserProfileDto,
  ): Promise<UserProfileResponse> {
    if (currentUserId !== userId) {
      throw new ForbiddenException('Trying to update another user profile');
    }

    const profile = await this.users.updateInternalProfile(userId, body);
    if (!profile) {
      throw new NotFoundException('User not found');
    }

    return profile;
  }

  private parseSnowflakeList(value: string | undefined): string[] {
    if (!value) {
      throw new BadRequestException('ids query parameter is required');
    }

    const ids = value
      .split(',')
      .map((id) => id.trim())
      .filter((id) => id.length > 0);

    if (ids.length === 0) {
      throw new BadRequestException('ids query parameter is required');
    }

    for (const id of ids) {
      if (!/^\d+$/.test(id)) {
        throw new BadRequestException('ids query parameter must contain snowflakes only');
      }
    }

    if (ids.length > 100) {
      throw new BadRequestException('ids query parameter must not contain more than 100 ids');
    }

    return ids;
  }

  private parseSearchLimit(value: number | undefined): number {
    if (value === undefined) {
      return 20;
    }

    if (!Number.isInteger(value) || value < 1 || value > 50) {
      throw new BadRequestException('limit must be between 1 and 50');
    }

    return value;
  }
}
