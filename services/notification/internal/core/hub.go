package core

import (
	"log"
	"sync"
)

type Hub struct {
	mu   sync.RWMutex
	subs map[int64][]chan NotificationSSE
}

func NewHub() (*Hub, error) {
	return &Hub{subs: make(map[int64][]chan NotificationSSE)}, nil
}

// stats returns the number of users with at least one open SSE stream and the
// total number of open streams (a user may have several tabs). cheap read under
// the shared lock, safe to call from a metrics scrape callback.
func (h *Hub) Stats() (users, streams int) {
	h.mu.RLock()
	defer h.mu.RUnlock()

	users = len(h.subs)
	for _, cs := range h.subs {
		streams += len(cs)
	}
	return users, streams
}

func (h *Hub) Subscribe(userID int64) chan NotificationSSE {
	ch := make(chan NotificationSSE, 16)

	h.mu.Lock()
	h.subs[userID] = append(h.subs[userID], ch)
	h.mu.Unlock()

	log.Printf("client user=%d connected", userID)

	return ch
}

func (h *Hub) Unsubscribe(userID int64, ch chan NotificationSSE) {
	h.mu.Lock()
	defer h.mu.Unlock()

	subs := h.subs[userID]
	filtered := subs[:0]

	for _, c := range subs {
		if ch != c {
			filtered = append(filtered, c)
		}
	}

	for i := len(filtered); i < len(subs); i++ {
		subs[i] = nil
	}

	if len(filtered) == 0 {
		delete(h.subs, userID)
	} else {
		h.subs[userID] = filtered
	}

	log.Printf("client user=%d disconnected", userID)

	close(ch)
}

// TODO: si jamais la connection cliente est trop lente, et qu il n arrive pas a traiter assez d event notif envoye,
// on peut augmenter la taille du buffer d'un chan pour augmenter sa capacite, et
func (h *Hub) Push(userID int64, notif NotificationSSE) {

	h.mu.RLock()
	defer h.mu.RUnlock()

	for _, c := range h.subs[userID] {
		select {
		case c <- notif:
			continue
		default:
			log.Printf("notification dropped for slow client user=%d id=%s", userID, notif.ID)
		}
	}

}
