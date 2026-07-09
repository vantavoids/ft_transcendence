import { Module } from '@nestjs/common';
import { ConfigModule } from '@nestjs/config';
import { AuthModule } from './auth/auth.module';
import { HealthController } from './health.controller';
import { validateEnv } from './config/env';
import { DatabaseModule } from './database/database.module';
import { TelemetryModule } from './telemetry/telemetry.module';
import { UsersModule } from './users/users.module';
import { V1Controller } from './v1.controller';

@Module({
  imports: [
    ConfigModule.forRoot({
      isGlobal: true,
      cache: true,
      validate: validateEnv,
    }),
    TelemetryModule,
    AuthModule,
    DatabaseModule,
    UsersModule,
  ],
  controllers: [HealthController, V1Controller],
})
export class AppModule {}
