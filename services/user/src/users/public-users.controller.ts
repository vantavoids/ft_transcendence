import { Controller, Get, NotFoundException, Param } from '@nestjs/common';
import { ParseSnowflakePipe } from '../common/pipes/parse-snowflake.pipe';
import { UserProfileResponse, UsersService } from './users.service';

@Controller('v1/users')
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
}
