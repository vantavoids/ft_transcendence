package notification

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

func (h *Hub) Subscribe(userID int64) chan NotificationSSE {
	ch := make(chan NotificationSSE, 16)

	h.mu.Lock()
	h.subs[userID] = append(h.subs[userID], ch)
	h.mu.Unlock()

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
