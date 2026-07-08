import { Matches } from 'class-validator';

export class CreateFriendRequestDto {
  @Matches(/^\d+$/, {
    message: 'addressee_id must be a positive integer',
  })
  addressee_id!: string;
}
