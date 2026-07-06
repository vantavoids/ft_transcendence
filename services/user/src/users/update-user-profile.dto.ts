import { IsIn, IsOptional, IsString, MaxLength } from 'class-validator';

export class UpdateUserProfileDto {
  @IsOptional()
  @IsString()
  @MaxLength(64)
  display_name?: string;

  @IsOptional()
  @IsString()
  bio?: string;

  @IsOptional()
  @IsIn(['online', 'idle', 'dnd', 'offline'])
  status?: 'online' | 'idle' | 'dnd' | 'offline';
}
