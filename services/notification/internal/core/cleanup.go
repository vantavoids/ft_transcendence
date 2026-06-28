package core

import (
	"context"
	"log"
	"time"
)

func RunCleanupLoop(ctx context.Context, orch *Orchestrator) {

	ticker := time.NewTicker(24 * time.Hour)

	go func() {
		defer ticker.Stop()

		if err := orch.DeleteOlder(ctx); err != nil {
			log.Printf("cleanup notifs: %v", err)
		}

		for {
			select {
			case <-ticker.C:
				if err := orch.DeleteOlder(ctx); err != nil {
					log.Printf("cleanup notifs: %v", err)
				}
			case <-ctx.Done():
				return
			}
		}
	}()
}
