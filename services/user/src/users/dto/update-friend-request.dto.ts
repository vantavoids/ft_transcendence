import { IsIn } from 'class-validator';

export class UpdateFriendRequestDto {
  @IsIn(['accepted', 'blocked'])
  status!: 'accepted' | 'blocked';
}
