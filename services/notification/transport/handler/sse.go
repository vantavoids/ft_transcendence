package handler

import (
	"encoding/json"
	"fmt"
	"log"
	"net/http"
	"time"

	core "github.com/vantavoids/ft_transcendence/services/notification/internal/core"
)

func sseHandler(hub *core.Hub) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		userID, ok := getUserIDFromContext(r.Context())
		if !ok {
			writeJSON(w, http.StatusUnauthorized, errorBody("unauthorized error"))
			return
		}

		w.Header().Set("Content-Type", "text/event-stream")
		w.Header().Set("Cache-Control", "no-cache")
		w.Header().Set("X-Accel-Buffering", "no")

		rc := http.NewResponseController(w)
		ch := hub.Subscribe(userID)
		defer hub.Unsubscribe(userID, ch)

		rc.Flush()
		ticker := time.NewTicker(15 * time.Second)
		defer ticker.Stop()

		for {
			select {
			case <-r.Context().Done():
				log.Printf("Client: %d disconnected", userID)
				return
			case notif := <-ch:
				data, err := json.Marshal(notif)
				if err != nil {
					log.Printf("failed to marshal notification: %v", err)
					continue
				}
				fmt.Fprintf(w, "event: ReceiveNotification\n")
				fmt.Fprintf(w, "data: %s\n\n", data)
				if err := rc.Flush(); err != nil {
					return
				}
			case <-ticker.C:
				if _, err := fmt.Fprintf(w, ": ping\n\n"); err != nil {
					return
				}
				if err := rc.Flush(); err != nil {
					return
				}
			}
		}
	}
}
