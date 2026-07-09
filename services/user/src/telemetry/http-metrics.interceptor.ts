import {
  CallHandler,
  ExecutionContext,
  Inject,
  Injectable,
  NestInterceptor,
} from '@nestjs/common';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import type { Histogram } from 'prom-client';
import { HTTP_REQUEST_DURATION } from './telemetry.constants';

// Records the RED baseline metric (http_server_request_duration_seconds) for every
// inbound request, labelled by method / route template / status per the monitoring
// contract (docs/monitoring-metrics.md). The scrape endpoint itself is skipped so
// polling doesn't pollute the histogram.
@Injectable()
export class HttpMetricsInterceptor implements NestInterceptor {
  constructor(
    @Inject(HTTP_REQUEST_DURATION) private readonly duration: Histogram<string>,
  ) {}

  intercept(context: ExecutionContext, next: CallHandler): Observable<unknown> {
    if (context.getType() !== 'http') {
      return next.handle();
    }

    const http = context.switchToHttp();
    const request = http.getRequest<{
      method: string;
      route?: { path?: string };
      originalUrl?: string;
    }>();
    const response = http.getResponse<{ statusCode: number }>();

    const rawPath = request.originalUrl?.split('?')[0] ?? '';
    if (rawPath === '/metrics') {
      return next.handle();
    }

    const start = process.hrtime.bigint();
    return next.handle().pipe(
      finalize(() => {
        const seconds = Number(process.hrtime.bigint() - start) / 1e9;
        // route template (e.g. /v1/users/:userId) keeps label cardinality bounded;
        // fall back to the raw path for unmatched routes (404s)
        const route = request.route?.path ?? rawPath ?? 'unknown';
        this.duration.observe(
          {
            http_request_method: request.method,
            http_route: route,
            http_response_status_code: response.statusCode,
          },
          seconds,
        );
      }),
    );
  }
}
