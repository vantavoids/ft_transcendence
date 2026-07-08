import {
  BadRequestException,
  Body,
  Controller,
  Delete,
  ConflictException,
  HttpCode,
  ForbiddenException,
  NotFoundException,
  Param,
  Patch,
  Post,
  UseGuards,
} from '@nestjs/common';
import { CurrentUserId } from '../auth/current-user.decorator';
import { JwtAuthGuard } from '../auth/jwt-auth.guard';
import { ParseSnowflakePipe } from '../common/pipes/parse-snowflake.pipe';
import {
  FriendshipResponse,
  UsersService,
} from './users.service';
import { CreateFriendRequestDto } from './dto/create-friend-request.dto';
import { UpdateFriendRequestDto } from './dto/update-friend-request.dto';

@Controller('v1')
@UseGuards(JwtAuthGuard)
export class FriendshipsController {
  constructor(private readonly users: UsersService) {}

  @Post('friends')
  async createFriendRequest(
    @CurrentUserId() requesterId: string,
    @Body() body: CreateFriendRequestDto,
  ): Promise<FriendshipResponse> {
    if (requesterId === body.addressee_id) {
      throw new BadRequestException('Cannot send request to yourself');
    }

    const created = await this.users.createFriendRequest(
      requesterId,
      body.addressee_id,
    );

    if (created === 'not_found') {
      throw new NotFoundException('User not found');
    }

    if (created === 'conflict') {
      throw new ConflictException('Request already exists or users are already friends');
    }

    return created;
  }

  @Patch('friends/:friendshipId')
  async updateFriendRequest(
    @CurrentUserId() callerId: string,
    @Param('friendshipId', ParseSnowflakePipe) friendshipId: string,
    @Body() body: UpdateFriendRequestDto,
  ): Promise<FriendshipResponse> {
    const updated = await this.users.updateFriendRequest(
      friendshipId,
      callerId,
      body.status,
    );

    if (updated === 'not_found') {
      throw new NotFoundException('Friendship not found');
    }

    if (updated === 'forbidden') {
      throw new ForbiddenException('Not the addressee of this request');
    }

    if (updated === 'conflict') {
      throw new ConflictException('Friendship is not pending');
    }

    return updated;
  }

  @HttpCode(204)
  @Delete('friends/:friendshipId')
  async deleteFriendRequest(
    @CurrentUserId() callerId: string,
    @Param('friendshipId', ParseSnowflakePipe) friendshipId: string,
  ): Promise<void> {
    const deleted = await this.users.deleteFriendRequest(friendshipId, callerId);

    if (deleted === 'not_found') {
      throw new NotFoundException('Friendship not found');
    }

    if (deleted === 'forbidden') {
      throw new ForbiddenException('Not a participant in this friendship');
    }
  }
}
