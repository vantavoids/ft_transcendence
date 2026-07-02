# Monitoring: per-service metrics contract

Contract for the per-service OpenTelemetry EPIC (#227). Six services, three
languages, one Prometheus + Grafana stack (coming with #264). The shared dashboards
query metrics **by name**, so consistency across languages is the whole point. This
doc is the source of truth each service issue (#228-#233) implements against.
Guild (#230) is the reference implementation; see
`services/guild/src/Guild.Presentation/Program.cs`.

## The invariant

Every service MUST expose a Prometheus scrape endpoint:

- **Path:** `GET /metrics`
- **Port:** `8080` (the service's existing HTTP port)
- **Format:** Prometheus text (`text/plain; version=0.0.4`)
- **Auth:** none. `/metrics` is unauthenticated and internal-only: the API
  Gateway forwards only `/api/{service}/vN/...`, so `/metrics` is reachable
  solely over the internal network, exactly like `/healthz`.

## Required metrics (the fleet RED baseline)

The shared dashboards depend on one metric family, present in every service:

| Concern | Metric (Prometheus name) | Notes |
|---------|--------------------------|-------|
| Latency + rate + errors | `http_server_request_duration_seconds` (histogram: `_bucket` / `_sum` / `_count`) | Rate = `rate(..._count[5m])`; errors filter on the status label; latency from the buckets |

Required labels on that metric:

- `http_request_method`
- `http_route` (the route template, not the raw path, to keep cardinality bounded)
- `http_response_status_code`

Plus a `service` label identifying the source (see Prometheus config below).

This name is not arbitrary: it is the OpenTelemetry HTTP semantic-convention
metric `http.server.request.duration` (unit seconds), and every OTel Prometheus
exporter applies the same OTel-to-Prometheus naming rules (dots to underscores,
unit suffix, `_total`/`_bucket`). So .NET, Go, and Rust exporters all land on the
identical Prometheus name without coordination. That determinism is why we route
through OTel rather than six hand-rolled Prometheus clients.

## Recommended (per-service, not cross-language)

Language-specific runtime metrics are encouraged for per-service panels but are
NOT part of the cross-language contract (their names differ by runtime):

- .NET: `AddRuntimeInstrumentation()` (GC, threadpool, exceptions)
- Go: the Go collector (goroutines, GC, heap (vive la stack))
- Rust: process/runtime metrics as available

Services may also expose **domain (business) gauges** via a custom `Meter`, again
per-service and not part of the cross-language contract. Guild does this with a
`Guild.Domain` meter and a background collector: `guild_guilds`, `guild_members`,
`guild_channels`, `guild_roles`, `guild_invites_active`, `guild_bans`. The counts
are polled off the scrape path (an `IHostedService` every ~20s writes a snapshot;
the observable gauges just read it), so a scrape never triggers a DB query. Keep
these low-cardinality: totals only, never a per-entity id label.

Note: the `MassTransit` meter does **not** work on MassTransit 8.x (its metrics
are the legacy prometheus-net package, not the `Meter` API); event-side counters,
if wanted, need a custom meter.

## Per-language implementation

| Service(s) | Language | Libraries |
|------------|----------|-----------|
| Auth, Guild, Chat | .NET | `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Runtime`, `OpenTelemetry.Exporter.Prometheus.AspNetCore` |
| Gateway, Notification | Go | `go.opentelemetry.io/otel` SDK, `otelhttp` middleware, `go.opentelemetry.io/otel/exporters/prometheus` |
| User | Rust | `opentelemetry` + `opentelemetry-prometheus`. Rust OTel metrics is the least mature link; if it misbehaves, a native Prometheus client (e.g. `metrics-exporter-prometheus`) is acceptable **provided it emits the same metric name and labels above**. The contract is the names, not the tool. |

### .NET reference (Guild)

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("guild"))
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

// ... after Build():
app.MapPrometheusScrapingEndpoint(); // GET /metrics, anonymous
```

`ConfigureResource(... AddService("<name>"))` sets `service.name`, which the
exporter surfaces as a `target_info` metric. Prefer the `service` scrape label
below for dashboard queries; keep the resource name for provenance.

## Prometheus scrape config (#264)

`infra/prometheus/prometheus.yml` already carries the target job, commented out
until instrumentation lands, with a per-service `service` label to match the
`healthz` job convention:

```yaml
  - job_name: services
    metrics_path: /metrics
    static_configs:
      - targets: ["guild:8080"]
        labels: { service: guild }
      # ... one entry per service as each lands
```

As each service ships its `/metrics`, uncomment its target (and add the
`service` label). Leaving a target enabled before its endpoint exists makes it
sit permanently `DOWN`.

## Checklist per service

- [ ] `/metrics` on `:8080`, Prometheus text, unauthenticated
- [ ] emits `http_server_request_duration_seconds` with method / route / status labels
- [ ] functional test: `/metrics` is 200 without a token and exposes the RED metric
- [ ] add the `service`-labelled target to `prometheus.yml` (coordinate with #264)
