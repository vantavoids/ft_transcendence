import { Module } from '@nestjs/common';
import { APP_INTERCEPTOR } from '@nestjs/core';
import { collectDefaultMetrics, Histogram, Registry } from 'prom-client';
import { HttpMetricsInterceptor } from './http-metrics.interceptor';
import { MetricsController } from './metrics.controller';
import { HTTP_REQUEST_DURATION, METRICS_REGISTRY } from './telemetry.constants';

// OpenTelemetry-compatible Prometheus metrics (#229). A native client is used per
// the monitoring contract's allowance ("the contract is the names, not the tool");
// the RED histogram name/labels match the OTel http.server.request.duration
// semantic convention so the shared dashboards query it identically to the .NET
// and Go services.
@Module({
  controllers: [MetricsController],
  providers: [
    {
      provide: METRICS_REGISTRY,
      useFactory: (): Registry => {
        const registry = new Registry();
        // Node runtime metrics (heap, event loop, gc, ...) - recommended, per-service
        collectDefaultMetrics({ register: registry });
        return registry;
      },
    },
    {
      provide: HTTP_REQUEST_DURATION,
      inject: [METRICS_REGISTRY],
      useFactory: (registry: Registry) =>
        new Histogram({
          name: 'http_server_request_duration_seconds',
          help: 'Duration of inbound HTTP requests in seconds',
          labelNames: [
            'http_request_method',
            'http_route',
            'http_response_status_code',
          ],
          // OpenTelemetry default buckets for http.server.request.duration
          buckets: [
            0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5,
            10,
          ],
          registers: [registry],
        }),
    },
    {
      provide: APP_INTERCEPTOR,
      useClass: HttpMetricsInterceptor,
    },
  ],
})
export class TelemetryModule {}
