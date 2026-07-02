package observability

import (
	"context"
	"net/http"

	"github.com/prometheus/client_golang/prometheus"
	"github.com/prometheus/client_golang/prometheus/promhttp"
	"github.com/vantavoids/ft_transcendence/services/gateway/ratelimit"
	"go.opentelemetry.io/contrib/instrumentation/runtime"
	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/attribute"
	otelprom "go.opentelemetry.io/otel/exporters/prometheus"
	"go.opentelemetry.io/otel/metric"
	sdkmetric "go.opentelemetry.io/otel/sdk/metric"
	"go.opentelemetry.io/otel/sdk/resource"
)

// setup wires OpenTelemetry metrics for Prometheus and returns the handler to
// mount at /metrics: it registers the global meter provider (so otelhttp emits
// the http.server RED metrics), go runtime metrics, and a gauge of the clients
// the rate limiter is tracking, per bucket. scrape model, not OTLP push, to
// match the fleet contract.
func Setup(rateLimitStores map[string]*ratelimit.MemoryStore) (http.Handler, error) {
	reg := prometheus.NewRegistry()

	exporter, err := otelprom.New(otelprom.WithRegisterer(reg))
	if err != nil {
		return nil, err
	}

	provider := sdkmetric.NewMeterProvider(
		sdkmetric.WithReader(exporter),
		sdkmetric.WithResource(resource.NewSchemaless(attribute.String("service.name", "gateway"))),
	)
	otel.SetMeterProvider(provider)

	// go runtime metrics (GC, goroutines, heap)
	if err := runtime.Start(runtime.WithMeterProvider(provider)); err != nil {
		return nil, err
	}

	if err := registerGauges(provider, rateLimitStores); err != nil {
		return nil, err
	}

	return promhttp.HandlerFor(reg, promhttp.HandlerOpts{}), nil
}

func registerGauges(provider metric.MeterProvider, stores map[string]*ratelimit.MemoryStore) error {
	meter := provider.Meter("gateway.domain")

	// live in-memory read of the rate-limit stores; no background collector needed
	clients, err := meter.Int64ObservableGauge("gateway.ratelimit.clients",
		metric.WithDescription("clients currently tracked by the rate limiter, per bucket"))
	if err != nil {
		return err
	}

	_, err = meter.RegisterCallback(
		func(_ context.Context, o metric.Observer) error {
			for bucket, store := range stores {
				o.ObserveInt64(clients, int64(store.Size()), metric.WithAttributes(attribute.String("bucket", bucket)))
			}
			return nil
		},
		clients,
	)
	return err
}
