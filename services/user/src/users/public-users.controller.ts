import { Controller, Get, NotFoundException, Param, UseGuards } from '@nestjs/common';
import { ParseSnowflakePipe } from '../common/pipes/parse-snowflake.pipe';
import { CurrentUserId } from '../auth/current-user.decorator';
import { JwtAuthGuard } from '../auth/jwt-auth.guard';
import { UserProfileResponse, UsersService } from './users.service';

@Controller('v1/users')
@UseGuards(JwtAuthGuard)
export class PublicUsersController {
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

  @Get('me')
  async getMe(@CurrentUserId() userId: string): Promise<UserProfileResponse> {
    const profile = await this.users.getInternalProfile(userId);
    if (!profile) {
      throw new NotFoundException('User not found');
    }

    return profile;
  }
}
