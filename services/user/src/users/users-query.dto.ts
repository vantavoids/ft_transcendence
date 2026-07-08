import { Type } from 'class-transformer';
import { IsInt, IsOptional, IsString, Max, Min, MinLength } from 'class-validator';

export class ListUsersQueryDto {
  @IsOptional()
  @IsString()
  ids?: string;
}

export class SearchUsersQueryDto {
  @IsString()
  @MinLength(2)
  q!: string;

  @IsOptional()
  @Type(() => Number)
  @IsInt()
  @Min(1)
  @Max(50)
  limit?: number;
}
