import { IsIn, IsOptional } from 'class-validator';

export class ListFriendRequestsQueryDto {
  @IsOptional()
  @IsIn(['incoming', 'outgoing', 'all'])
  direction?: 'incoming' | 'outgoing' | 'all';
}
