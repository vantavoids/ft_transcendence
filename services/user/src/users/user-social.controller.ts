import {
  BadRequestException,
  Controller,
  Get,
  NotFoundException,
  Param,
  Query,
  UseGuards,
} from '@nestjs/common';
import { CurrentUserId } from '../auth/current-user.decorator';
import { JwtAuthGuard } from '../auth/jwt-auth.guard';
import { ParseSnowflakePipe } from '../common/pipes/parse-snowflake.pipe';
import {
  FriendRequestListItemResponse,
  FriendRequestDirection,
  FriendSummaryResponse,
  RelationshipResponse,
  UsersService,
} from './users.service';
import { ListFriendRequestsQueryDto } from './dto/list-friend-requests.dto';

@Controller('v1/users')
@UseGuards(JwtAuthGuard)
export class UserSocialController {
  constructor(private readonly users: UsersService) {}

  @Get(':userId/friends')
  async listFriends(
    @CurrentUserId() viewerId: string,
    @Param('userId', ParseSnowflakePipe) userId: string,
  ): Promise<FriendSummaryResponse[]> {
    return this.users.listFriends(viewerId, userId);
  }

  @Get('me/friend-requests')
  async listFriendRequests(
    @CurrentUserId() viewerId: string,
    @Query() query: ListFriendRequestsQueryDto,
  ): Promise<FriendRequestListItemResponse[]> {
    return this.users.listFriendRequests(
      viewerId,
      (query.direction ?? 'all') as FriendRequestDirection,
    );
  }

  @Get('me/friendship/:userId')
  async getFriendship(
    @CurrentUserId() viewerId: string,
    @Param('userId', ParseSnowflakePipe) otherUserId: string,
  ): Promise<RelationshipResponse> {
    if (viewerId === otherUserId) {
      throw new BadRequestException('user_id must be different from caller');
    }

    const relationship = await this.users.getRelationshipPerspective(
      viewerId,
      otherUserId,
    );
    if (!relationship) {
      throw new NotFoundException('User not found');
    }

    return relationship;
  }
}
