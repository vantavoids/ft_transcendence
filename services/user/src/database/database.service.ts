import { Inject, Injectable, OnApplicationShutdown } from '@nestjs/common';
import { Pool } from 'pg';
import { DATABASE_POOL } from './database.module';

@Injectable()
export class DatabaseService implements OnApplicationShutdown {
  constructor(@Inject(DATABASE_POOL) private readonly pool: Pool) {}

  get client(): Pool {
    return this.pool;
  }

  async ping(): Promise<boolean> {
    await this.pool.query('SELECT 1');
    return true;
  }

  async onApplicationShutdown(): Promise<void> {
    await this.pool.end();
  }
}
