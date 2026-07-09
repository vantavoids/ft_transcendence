import {
  Inject,
  Injectable,
  Logger,
  OnApplicationBootstrap,
  OnApplicationShutdown,
} from '@nestjs/common';
import { Gauge, Registry } from 'prom-client';
import { DatabaseService } from '../database/database.service';
import { METRICS_REGISTRY } from './telemetry.constants';

const COLLECT_INTERVAL_MS = 20_000;
const FRIENDSHIP_STATUSES = ['pending', 'accepted', 'blocked'] as const;

// Low-cardinality domain (business) gauges for the User service, mirroring Guild's
// background collector: counts are snapshotted every ~20s OFF the scrape path, so a
// Prometheus scrape never triggers a DB query. Totals only, never a per-entity id.
@Injectable()
export class DomainMetricsCollector
  implements OnApplicationBootstrap, OnApplicationShutdown
{
  private readonly logger = new Logger(DomainMetricsCollector.name);
  private timer?: ReturnType<typeof setInterval>;
  private readonly profiles: Gauge<string>;
  private readonly friendships: Gauge<string>;
  private readonly blocks: Gauge<string>;

  constructor(
    private readonly database: DatabaseService,
    @Inject(METRICS_REGISTRY) registry: Registry,
  ) {
    this.profiles = new Gauge({
      name: 'user_profiles',
      help: 'Total user profiles',
      registers: [registry],
    });
    this.friendships = new Gauge({
      name: 'user_friendships',
      help: 'Friendship rows by status',
      labelNames: ['status'],
      registers: [registry],
    });
    this.blocks = new Gauge({
      name: 'user_blocks',
      help: 'Total user block rows',
      registers: [registry],
    });
  }

  async onApplicationBootstrap(): Promise<void> {
    await this.collect();
    this.timer = setInterval(() => void this.collect(), COLLECT_INTERVAL_MS);
    this.timer.unref?.();
  }

  onApplicationShutdown(): void {
    if (this.timer) {
      clearInterval(this.timer);
    }
  }

  private async collect(): Promise<void> {
    try {
      const [profiles, friendships, blocks] = await Promise.all([
        this.database.client.query<{ count: string }>(
          'SELECT count(*) AS count FROM users_profile',
        ),
        this.database.client.query<{ status: string; count: string }>(
          'SELECT status::text AS status, count(*) AS count FROM friendships GROUP BY status',
        ),
        this.database.client.query<{ count: string }>(
          'SELECT count(*) AS count FROM user_blocks',
        ),
      ]);

      this.profiles.set(Number(profiles.rows[0]?.count ?? 0));
      this.blocks.set(Number(blocks.rows[0]?.count ?? 0));

      // keep every status series present (even at 0) so panels don't gap
      for (const status of FRIENDSHIP_STATUSES) {
        this.friendships.set({ status }, 0);
      }
      for (const row of friendships.rows) {
        this.friendships.set({ status: row.status }, Number(row.count));
      }
    } catch (error) {
      this.logger.warn(
        `domain metrics collection skipped: ${error instanceof Error ? error.message : String(error)}`,
      );
    }
  }
}
