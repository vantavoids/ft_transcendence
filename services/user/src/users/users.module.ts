import { Module } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { SnowflakeIdGenerator } from '../common/snowflake-id.generator';
import { RelationshipEventsPublisher } from './events/relationship-events.publisher';
import { NoopRelationshipEventsPublisher } from './events/relationship-events.publisher';
import { RabbitMqRelationshipEventsPublisher } from './events/rabbitmq-relationship-events.publisher';
import {
  NoopUserEventsConsumer,
  RabbitMqUserEventsConsumer,
  UserEventsConsumer,
} from './events/user-events.consumer';
import { InternalUsersController } from './internal-users.controller';
import { DataExportController } from './data-export.controller';
import { DataExportEventsPublisher, NoopDataExportEventsPublisher, RabbitMqDataExportEventsPublisher } from './data-export.publisher';
import { DataExportService } from './data-export.service';
import { DataExportStorageService } from './data-export.storage';
import { FriendshipsController } from './friendships.controller';
import { BlocksRepository } from './repositories/blocks.repository';
import { DataExportRepository } from './repositories/data-export.repository';
import { FriendshipsRepository } from './repositories/friendships.repository';
import { ProfileMediaRepository } from './repositories/profile-media.repository';
import { ProfilesRepository } from './repositories/profiles.repository';
import { RelationshipsRepository } from './repositories/relationships.repository';
import { UsersLookupRepository } from './repositories/users-lookup.repository';
import { ProfileMediaStorageService } from './media/profile-media.storage';
import { PublicUsersController } from './public-users.controller';
import { UserMediaController } from './user-media.controller';
import { UserSocialController } from './user-social.controller';
import { UsersService } from './users.service';

@Module({
  controllers: [
    InternalUsersController,
    DataExportController,
    PublicUsersController,
    UserMediaController,
    FriendshipsController,
    UserSocialController,
  ],
  providers: [
    UsersService,
    SnowflakeIdGenerator,
    {
      provide: RelationshipEventsPublisher,
      inject: [ConfigService],
      useFactory: (config: ConfigService) =>
        config.get<'development' | 'test' | 'production'>('APP_ENV') === 'test'
          ? new NoopRelationshipEventsPublisher()
          : new RabbitMqRelationshipEventsPublisher(),
    },
    {
      provide: UserEventsConsumer,
      inject: [ConfigService, ProfilesRepository],
      useFactory: (config: ConfigService, profilesRepository: ProfilesRepository) =>
        config.get<'development' | 'test' | 'production'>('APP_ENV') === 'test'
          ? new NoopUserEventsConsumer()
          : new RabbitMqUserEventsConsumer(profilesRepository),
    },
    ProfilesRepository,
    RelationshipsRepository,
    UsersLookupRepository,
    FriendshipsRepository,
    BlocksRepository,
    DataExportRepository,
    ProfileMediaRepository,
    ProfileMediaStorageService,
    DataExportStorageService,
    DataExportService,
    {
      provide: DataExportEventsPublisher,
      inject: [ConfigService],
      useFactory: (config: ConfigService) =>
        config.get<'development' | 'test' | 'production'>('APP_ENV') === 'test'
          ? new NoopDataExportEventsPublisher()
          : new RabbitMqDataExportEventsPublisher(),
    },
  ],
  exports: [UsersService],
})
export class UsersModule {}
