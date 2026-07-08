import { Module } from '@nestjs/common';
import { InternalUsersController } from './internal-users.controller';
import { ProfilesRepository } from './repositories/profiles.repository';
import { RelationshipsRepository } from './repositories/relationships.repository';
import { UsersLookupRepository } from './repositories/users-lookup.repository';
import { PublicUsersController } from './public-users.controller';
import { UsersService } from './users.service';

@Module({
  controllers: [InternalUsersController, PublicUsersController],
  providers: [
    UsersService,
    ProfilesRepository,
    RelationshipsRepository,
    UsersLookupRepository,
  ],
  exports: [UsersService],
})
export class UsersModule {}
