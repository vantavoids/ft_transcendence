import { Injectable, OnApplicationShutdown } from '@nestjs/common';
import { ChannelModel, ConfirmChannel, connect } from 'amqplib';
import { FriendAcceptedEvent, FriendRequestSentEvent } from '../users.types';
import { RelationshipEventsPublisher } from './relationship-events.publisher';

@Injectable()
export class RabbitMqRelationshipEventsPublisher
  extends RelationshipEventsPublisher
  implements OnApplicationShutdown
{
  private static readonly exchangeName = 'events';
  private connection?: ChannelModel;
  private channel?: ConfirmChannel;
  private connecting?: Promise<void>;

  async publishFriendRequestSent(
    event: FriendRequestSentEvent,
  ): Promise<void> {
    await this.publish('friend.request_sent', event);
  }

  async publishFriendAccepted(event: FriendAcceptedEvent): Promise<void> {
    await this.publish('friend.accepted', event);
  }

  private async publish(routingKey: string, event: unknown): Promise<void> {
    const channel = await this.ensureChannel();
    channel.publish(
      RabbitMqRelationshipEventsPublisher.exchangeName,
      routingKey,
      Buffer.from(JSON.stringify(event), 'utf8'),
      {
        contentType: 'application/json',
        persistent: true,
      },
    );
    await channel.waitForConfirms();
  }

  async onApplicationShutdown(): Promise<void> {
    if (this.channel) {
      await this.channel.close().catch(() => undefined);
    }
    if (this.connection) {
      await this.connection.close().catch(() => undefined);
    }
  }

  private async ensureChannel(): Promise<ConfirmChannel> {
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
      throw new Error('RabbitMQ relationship publisher is not connected');
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
        });

        this.channel = await this.connection.createConfirmChannel();
        await this.channel.assertExchange(
          RabbitMqRelationshipEventsPublisher.exchangeName,
          'direct',
          { durable: true },
        );
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
