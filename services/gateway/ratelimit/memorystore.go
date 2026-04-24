// Package ratelimit provides rate limiting implementations.
package ratelimit

import (
	"golang.org/x/time/rate"
	"sync"
)

type MemoryStore struct {
	clients map[string]*rate.Limiter
	mu      sync.Mutex
	rate    rate.Limit
	burst   int
}

func NewMemoryStore(r float64, b int) *MemoryStore {

	return &MemoryStore{
		clients: make(map[string]*rate.Limiter),
		rate:    rate.Limit(r),
		burst:   b,
	}
}

func (store *MemoryStore) Allow(identity string) bool {

	store.mu.Lock()
	defer store.mu.Unlock()

	limiter, ok := store.clients[identity]
	if !ok {
		limiter = rate.NewLimiter(store.rate, store.burst)
		store.clients[identity] = limiter
	}

	return limiter.Allow()
}
