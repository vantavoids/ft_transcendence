import {
  Body,
  Controller,
  ForbiddenException,
  Get,
  NotFoundException,
  Param,
  Patch,
  UseGuards,
} from '@nestjs/common';
import { ParseSnowflakePipe } from '../common/pipes/parse-snowflake.pipe';
import { CurrentUserId } from '../auth/current-user.decorator';
import { JwtAuthGuard } from '../auth/jwt-auth.guard';
import { UserProfileResponse, UsersService } from './users.service';
import { UpdateUserProfileDto } from './update-user-profile.dto';

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
}
