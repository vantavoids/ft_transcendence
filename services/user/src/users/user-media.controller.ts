import {
  Controller,
  ForbiddenException,
  HttpCode,
  HttpStatus,
  NotFoundException,
  Param,
  ParseFilePipeBuilder,
  UploadedFile,
  UseGuards,
  UseInterceptors,
  Post,
  Delete,
} from '@nestjs/common';
import { FileInterceptor } from '@nestjs/platform-express';
import { CurrentUserId } from '../auth/current-user.decorator';
import { JwtAuthGuard } from '../auth/jwt-auth.guard';
import { ParseSnowflakePipe } from '../common/pipes/parse-snowflake.pipe';
import { UsersService } from './users.service';

const avatarUploadPipe = new ParseFilePipeBuilder()
  .addFileTypeValidator({ fileType: /^image\/(jpeg|png|webp)$/ })
  .addMaxSizeValidator({ maxSize: 5 * 1024 * 1024 })
  .build({ errorHttpStatusCode: HttpStatus.BAD_REQUEST });

const bannerUploadPipe = new ParseFilePipeBuilder()
  .addFileTypeValidator({ fileType: /^image\/(jpeg|png|webp)$/ })
  .addMaxSizeValidator({ maxSize: 8 * 1024 * 1024 })
  .build({ errorHttpStatusCode: HttpStatus.BAD_REQUEST });

@Controller('v1/users')
@UseGuards(JwtAuthGuard)
export class UserMediaController {
  constructor(private readonly users: UsersService) {}

  @Post(':userId/avatar')
  @UseInterceptors(FileInterceptor('avatar'))
  async uploadAvatar(
    @CurrentUserId() currentUserId: string,
    @Param('userId', ParseSnowflakePipe) userId: string,
    @UploadedFile(avatarUploadPipe) file: { buffer: Buffer; mimetype: string },
  ): Promise<{ avatar_url: string }> {
    this.assertOwnProfile(currentUserId, userId);

    const avatarUrl = await this.users.uploadAvatar(userId, file);
    if (avatarUrl === 'not_found') {
      throw new NotFoundException('User not found');
    }

    return { avatar_url: avatarUrl };
  }

  @Delete(':userId/avatar')
  @HttpCode(HttpStatus.NO_CONTENT)
  async deleteAvatar(
    @CurrentUserId() currentUserId: string,
    @Param('userId', ParseSnowflakePipe) userId: string,
  ): Promise<void> {
    this.assertOwnProfile(currentUserId, userId);

    const result = await this.users.deleteAvatar(userId);
    if (result === 'not_found') {
      throw new NotFoundException('Avatar not found');
    }
  }

  @Post(':userId/banner')
  @UseInterceptors(FileInterceptor('banner'))
  async uploadBanner(
    @CurrentUserId() currentUserId: string,
    @Param('userId', ParseSnowflakePipe) userId: string,
    @UploadedFile(bannerUploadPipe) file: { buffer: Buffer; mimetype: string },
  ): Promise<{ banner_url: string }> {
    this.assertOwnProfile(currentUserId, userId);

    const bannerUrl = await this.users.uploadBanner(userId, file);
    if (bannerUrl === 'not_found') {
      throw new NotFoundException('User not found');
    }

    return { banner_url: bannerUrl };
  }

  @Delete(':userId/banner')
  @HttpCode(HttpStatus.NO_CONTENT)
  async deleteBanner(
    @CurrentUserId() currentUserId: string,
    @Param('userId', ParseSnowflakePipe) userId: string,
  ): Promise<void> {
    this.assertOwnProfile(currentUserId, userId);

    const result = await this.users.deleteBanner(userId);
    if (result === 'not_found') {
      throw new NotFoundException('Banner not found');
    }
  }

  private assertOwnProfile(currentUserId: string, userId: string): void {
    if (currentUserId !== userId) {
      throw new ForbiddenException('Not your profile');
    }
  }
}
