import { Controller, Get, HttpStatus, Res } from '@nestjs/common';
import { DatabaseService } from './database/database.service';

interface HealthCheckEntry {
  name: string;
  status: 'Healthy' | 'Unhealthy';
  description: string | null;
}

interface HealthReport {
  status: 'Healthy' | 'Unhealthy';
  checks: HealthCheckEntry[];
}

@Controller()
export class HealthController {
  constructor(private readonly database: DatabaseService) {}

  @Get('healthz')
  async healthz(@Res({ passthrough: true }) response: any): Promise<HealthReport> {
    try {
      await this.database.ping();
      response.status(HttpStatus.OK);
      return {
        status: 'Healthy',
        checks: [
          {
            name: 'postgres',
            status: 'Healthy',
            description: null,
          },
        ],
      };
    } catch (error) {
      response.status(HttpStatus.SERVICE_UNAVAILABLE);
      return {
        status: 'Unhealthy',
        checks: [
          {
            name: 'postgres',
            status: 'Unhealthy',
            description:
              error instanceof Error
                ? `postgres unreachable: ${error.message}`
                : 'postgres unreachable',
          },
        ],
      };
    }
  }
}
