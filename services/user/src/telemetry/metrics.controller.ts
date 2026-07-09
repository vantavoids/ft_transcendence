import { Controller, Get, Header, Inject } from '@nestjs/common';
import type { Registry } from 'prom-client';
import { METRICS_REGISTRY } from './telemetry.constants';

// GET /metrics - Prometheus scrape endpoint, unauthenticated and internal-only
// (the gateway only forwards /api/{service}/vN/..., never /metrics).
@Controller()
export class MetricsController {
  constructor(@Inject(METRICS_REGISTRY) private readonly registry: Registry) {}

  @Get('metrics')
  @Header('Content-Type', 'text/plain; version=0.0.4')
  metrics(): Promise<string> {
    return this.registry.metrics();
  }
}
