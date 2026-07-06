import { Module } from '@nestjs/common';
import { InternalUsersController } from './internal-users.controller';
import { UsersService } from './users.service';

@Module({
  controllers: [InternalUsersController],
  providers: [UsersService],
  exports: [UsersService],
})
export class UsersModule {}
