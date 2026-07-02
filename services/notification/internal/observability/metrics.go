package observability

import (
	"context"
	"log"
	"net/http"
	"sync/atomic"
	"time"

	"github.com/jackc/pgx/v5/pgxpool"
	"github.com/prometheus/client_golang/prometheus"
	"github.com/prometheus/client_golang/prometheus/promhttp"
	"go.opentelemetry.io/contrib/instrumentation/runtime"
	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/attribute"
	otelprom "go.opentelemetry.io/otel/exporters/prometheus"
	"go.opentelemetry.io/otel/metric"
	sdkmetric "go.opentelemetry.io/otel/sdk/metric"
	"go.opentelemetry.io/otel/sdk/resource"
)

// hubStats exposes live SSE connection counts (implemented by core.Hub).
type hubStats interface {
	Stats() (users, streams int)
}

// Setup wires OpenTelemetry metrics for Prometheus and returns the handler to
// mount at /metrics. it registers the global meter provider (so otelhttp emits
// the http.server RED metrics), go runtime metrics, and the notification domain
// gauges. scrape model, not OTLP push, to match the fleet contract.
func Setup(pool *pgxpool.Pool, hub hubStats) (http.Handler, error) {
	reg := prometheus.NewRegistry()

	exporter, err := otelprom.New(otelprom.WithRegisterer(reg))
	if err != nil {
		return nil, err
	}

	provider := sdkmetric.NewMeterProvider(
		sdkmetric.WithReader(exporter),
		sdkmetric.WithResource(resource.NewSchemaless(attribute.String("service.name", "notification"))),
	)
	otel.SetMeterProvider(provider)

	// go runtime metrics (GC, goroutines, heap)
	if err := runtime.Start(runtime.WithMeterProvider(provider)); err != nil {
		return nil, err
	}

	if err := registerDomainGauges(provider, pool, hub); err != nil {
		return nil, err
	}

	return promhttp.HandlerFor(reg, promhttp.HandlerOpts{}), nil
}

func registerDomainGauges(provider metric.MeterProvider, pool *pgxpool.Pool, hub hubStats) error {
	meter := provider.Meter("notification.domain")

	// db-backed counts are refreshed off the scrape path by a poller into these
	// atomics; the observable callback just reads them. the realtime-clients
	// gauge reads the in-memory hub directly.
	var unread, mutes atomic.Int64

	unreadGauge, err := meter.Int64ObservableGauge("notification.unread",
		metric.WithDescription("unread, non-dismissed notifications"))
	if err != nil {
		return err
	}
	mutesGauge, err := meter.Int64ObservableGauge("notification.mutes.active",
		metric.WithDescription("active (non-expired) mute preferences"))
	if err != nil {
		return err
	}
	clientsGauge, err := meter.Int64ObservableGauge("notification.realtime.clients",
		metric.WithDescription("users with an open SSE stream"))
	if err != nil {
		return err
	}

	_, err = meter.RegisterCallback(
		func(_ context.Context, o metric.Observer) error {
			o.ObserveInt64(unreadGauge, unread.Load())
			o.ObserveInt64(mutesGauge, mutes.Load())
			users, _ := hub.Stats()
			o.ObserveInt64(clientsGauge, int64(users))
			return nil
		},
		unreadGauge, mutesGauge, clientsGauge,
	)
	if err != nil {
		return err
	}

	go pollCounts(pool, &unread, &mutes)
	return nil
}

// pollCounts refreshes the db-backed gauges every 20s, off the scrape path so a
// Prometheus scrape never triggers a query.
func pollCounts(pool *pgxpool.Pool, unread, mutes *atomic.Int64) {
	const interval = 20 * time.Second
	for {
		refresh(pool, unread, mutes)
		time.Sleep(interval)
	}
}

func refresh(pool *pgxpool.Pool, unread, mutes *atomic.Int64) {
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	var u int64
	if err := pool.QueryRow(ctx,
		`SELECT count(*) FROM notifications WHERE read_at IS NULL AND dismissed_at IS NULL`,
	).Scan(&u); err != nil {
		log.Printf("metrics: unread count failed: %v", err)
	} else {
		unread.Store(u)
	}

	var m int64
	if err := pool.QueryRow(ctx,
		`SELECT count(*) FROM notification_preferences WHERE muted AND (muted_until IS NULL OR muted_until > now())`,
	).Scan(&m); err != nil {
		log.Printf("metrics: mutes count failed: %v", err)
	} else {
		mutes.Store(m)
	}
}
