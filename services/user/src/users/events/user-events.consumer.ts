import {
  Injectable,
  Logger,
  OnApplicationBootstrap,
  OnApplicationShutdown,
} from '@nestjs/common';
import {
  Channel,
  ChannelModel,
  ConsumeMessage,
  connect,
} from 'amqplib';
import { ProfilesRepository } from '../repositories/profiles.repository';

type UserRegisteredEvent = {
  user_id?: string;
  email?: string;
};

type UserLifecycleEvent = {
  user_id?: string;
};

class PermanentEventError extends Error {}

export abstract class UserEventsConsumer
  implements OnApplicationBootstrap, OnApplicationShutdown
{
  abstract onApplicationBootstrap(): Promise<void>;
  abstract onApplicationShutdown(): Promise<void>;
}

@Injectable()
export class NoopUserEventsConsumer extends UserEventsConsumer {
  async onApplicationBootstrap(): Promise<void> {
    return;
  }

  async onApplicationShutdown(): Promise<void> {
    return;
  }
}

@Injectable()
export class RabbitMqUserEventsConsumer extends UserEventsConsumer {
  private static readonly exchangeName = 'events';
  private static readonly queueName = 'user-service';
  private readonly logger = new Logger(RabbitMqUserEventsConsumer.name);
  private connection?: ChannelModel;
  private channel?: Channel;
  private connecting?: Promise<void>;
  private shuttingDown = false;

  constructor(private readonly profilesRepository: ProfilesRepository) {
    super();
  }

  async onApplicationBootstrap(): Promise<void> {
    await this.ensureChannel();
  }

  async onApplicationShutdown(): Promise<void> {
    this.shuttingDown = true;

    if (this.channel) {
      await this.channel.close().catch(() => undefined);
    }
    if (this.connection) {
      await this.connection.close().catch(() => undefined);
    }
  }

  private async ensureChannel(): Promise<Channel> {
    if (this.channel) {
      return this.channel;
    }

    if (!this.connecting) {
      this.connecting = this.createChannel();
    }

    try {
      await this.connecting;
    } finally {
      this.connecting = undefined;
    }

    if (!this.channel) {
      throw new Error('RabbitMQ user consumer is not connected');
    }

    return this.channel;
  }

  private async createChannel(): Promise<void> {
    const url = this.buildConnectionUrl();

    for (let attempt = 1; attempt <= 10; attempt += 1) {
      try {
        this.connection = await connect(url);
        this.connection.on('close', () => {
          this.connection = undefined;
          this.channel = undefined;

          if (!this.shuttingDown) {
            void this.ensureChannel().catch((error) => {
              this.logger.error(
                'Failed to reconnect RabbitMQ user consumer',
                error instanceof Error ? error.stack : String(error),
              );
            });
          }
        });

        this.channel = await this.connection.createChannel();
        await this.channel.assertExchange(
          RabbitMqUserEventsConsumer.exchangeName,
          'direct',
          { durable: true },
        );

        const queue = await this.channel.assertQueue(
          RabbitMqUserEventsConsumer.queueName,
          { durable: true },
        );

        for (const routingKey of [
          'user.registered',
          'user.online',
          'user.offline',
          'user.deleted',
        ]) {
          await this.channel.bindQueue(
            queue.queue,
            RabbitMqUserEventsConsumer.exchangeName,
            routingKey,
          );
        }

        await this.channel.prefetch(10);
        await this.channel.consume(queue.queue, (message) => {
          void this.handleMessage(message);
        });
        return;
      } catch (error) {
        this.connection = undefined;
        this.channel = undefined;
        if (attempt === 10) {
          throw error;
        }

        await this.delay(1000 * attempt);
      }
    }
  }

  private async handleMessage(message: ConsumeMessage | null): Promise<void> {
    const channel = this.channel;
    if (!message || !channel) {
      return;
    }

    try {
      const routingKey = message.fields.routingKey;
      let payload: UserRegisteredEvent | UserLifecycleEvent;
      try {
        payload = JSON.parse(message.content.toString('utf8')) as
          | UserRegisteredEvent
          | UserLifecycleEvent;
      } catch {
        throw new PermanentEventError('Invalid JSON payload');
      }

      switch (routingKey) {
        case 'user.registered':
          await this.handleUserRegistered(payload as UserRegisteredEvent);
          break;
        case 'user.online':
          await this.handleUserOnline(payload as UserLifecycleEvent);
          break;
        case 'user.offline':
          await this.handleUserOffline(payload as UserLifecycleEvent);
          break;
        case 'user.deleted':
          await this.handleUserDeleted(payload as UserLifecycleEvent);
          break;
        default:
          throw new PermanentEventError(`Unsupported routing key: ${routingKey}`);
      }

      channel.ack(message);
    } catch (error) {
      this.logger.error(
        'Failed to process user event',
        error instanceof Error ? error.stack : String(error),
      );

      try {
        channel.nack(message, false, error instanceof PermanentEventError ? false : true);
      } catch {
        return;
      }
    }
  }

  private async handleUserRegistered(
    payload: UserRegisteredEvent,
  ): Promise<void> {
    const userId = this.requireUserId(payload.user_id, 'user.registered');
    const email = this.requireEmail(payload.email, 'user.registered');
    const outcome = await this.profilesRepository.createProfileFromRegistration(
      userId,
      email,
    );

    if (outcome === 'conflict') {
      throw new PermanentEventError(
        'user.registered: username collision could not be resolved',
      );
    }
  }

  private async handleUserOnline(payload: UserLifecycleEvent): Promise<void> {
    const userId = this.requireUserId(payload.user_id, 'user.online');
    await this.profilesRepository.markUserOnline(userId);
  }

  private async handleUserOffline(payload: UserLifecycleEvent): Promise<void> {
    const userId = this.requireUserId(payload.user_id, 'user.offline');
    await this.profilesRepository.markUserOffline(userId);
  }

  private async handleUserDeleted(payload: UserLifecycleEvent): Promise<void> {
    const userId = this.requireUserId(payload.user_id, 'user.deleted');
    await this.profilesRepository.deleteProfileById(userId);
  }

  private requireUserId(
    value: string | undefined,
    eventName: string,
  ): string {
    if (!value || !/^\d+$/.test(value)) {
      throw new PermanentEventError(`${eventName}: missing or invalid user_id`);
    }

    return value;
  }

  private requireEmail(value: string | undefined, eventName: string): string {
    if (!value || value.trim() === '') {
      throw new PermanentEventError(`${eventName}: missing email`);
    }

    return value;
  }

  private buildConnectionUrl(): string {
    const user = encodeURIComponent(process.env.RABBITMQ_USER ?? 'user');
    const pass = encodeURIComponent(process.env.RABBITMQ_PASS ?? 'password');
    const host = process.env.RABBITMQ_HOST ?? 'rabbitmq';
    const port = Number(process.env.RABBITMQ_PORT ?? '5672');
    const vhost = encodeURIComponent(process.env.RABBITMQ_VHOST ?? '/');

    return `amqp://${user}:${pass}@${host}:${port}/${vhost}`;
  }

  private delay(ms: number): Promise<void> {
    return new Promise((resolve) => {
      setTimeout(resolve, ms);
    });
  }
}
