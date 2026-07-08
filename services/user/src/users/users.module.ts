import { Module } from '@nestjs/common';
import { SnowflakeIdGenerator } from '../common/snowflake-id.generator';
import { RelationshipEventsPublisher } from './events/relationship-events.publisher';
import { RabbitMqRelationshipEventsPublisher } from './events/rabbitmq-relationship-events.publisher';
import { InternalUsersController } from './internal-users.controller';
import { FriendshipsController } from './friendships.controller';
import { BlocksRepository } from './repositories/blocks.repository';
import { FriendshipsRepository } from './repositories/friendships.repository';
import { ProfilesRepository } from './repositories/profiles.repository';
import { RelationshipsRepository } from './repositories/relationships.repository';
import { UsersLookupRepository } from './repositories/users-lookup.repository';
import { PublicUsersController } from './public-users.controller';
import { UserSocialController } from './user-social.controller';
import { UsersService } from './users.service';

@Module({
  controllers: [
    InternalUsersController,
    PublicUsersController,
    FriendshipsController,
    UserSocialController,
  ],
  providers: [
    UsersService,
    SnowflakeIdGenerator,
    {
      provide: RelationshipEventsPublisher,
      useClass: RabbitMqRelationshipEventsPublisher,
    },
    ProfilesRepository,
    RelationshipsRepository,
    UsersLookupRepository,
    FriendshipsRepository,
    BlocksRepository,
  ],
  exports: [UsersService],
})
export class UsersModule {}
