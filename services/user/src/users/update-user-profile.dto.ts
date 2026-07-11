import { Transform } from 'class-transformer';
import { IsIn, IsOptional, IsString, MaxLength, MinLength } from 'class-validator';

export class UpdateUserProfileDto {
  @IsOptional()
  @IsString()
  @Transform(({ value }) => (typeof value === 'string' ? value.trim() : value))
  @MinLength(1, { message: 'Display name is required.' })
  @MaxLength(64)
  display_name?: string;

  @IsOptional()
  @IsString()
  @Transform(({ value }) => (typeof value === 'string' ? value.trim() : value))
  @MaxLength(280)
  bio?: string;

  @IsOptional()
  @IsIn(['online', 'idle', 'dnd', 'offline'])
  status?: 'online' | 'idle' | 'dnd' | 'offline';
}
