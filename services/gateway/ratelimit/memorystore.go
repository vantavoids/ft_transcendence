// Package ratelimit provides rate limiting implementations.
package ratelimit

import (
	"strconv"
	"sync"
	"time"

	"github.com/vantavoids/ft_transcendence/services/gateway/logs"
	"golang.org/x/time/rate"
)

type Client struct {
	limiter  *rate.Limiter
	lastSeen time.Time
}

type MemoryStore struct {
	unchecked map[string]*Client
	checked   map[string]*Client
	maxIdle   time.Duration
	mu        sync.Mutex
	rate      rate.Limit
	burst     int
	which     string
}

func NewMemoryStore(idle time.Duration, rateVal float64, bucketSize int, label string) *MemoryStore {

	return &MemoryStore{
		unchecked: make(map[string]*Client),
		checked:   make(map[string]*Client),
		maxIdle:   idle,
		rate:      rate.Limit(rateVal),
		burst:     bucketSize,
		which:     label,
	}
}

func NewClient(store *MemoryStore) *Client {

	return &Client{
		limiter:  rate.NewLimiter(store.rate, store.burst),
		lastSeen: time.Now(),
	}
}

func (store *MemoryStore) Allow(identity string) bool {

	store.mu.Lock()

	client, ok := store.unchecked[identity]
	if !ok {
		client, ok = store.checked[identity]
		if !ok {
			client = NewClient(store)
			store.checked[identity] = client
		} else {
			client.lastSeen = time.Now()
		}
	} else {
		client.lastSeen = time.Now()
		store.transferLocked(identity)
	}

	// rate.Limiter is goroutine-safe, only the map access needs store.mu
	store.mu.Unlock()

	return client.limiter.Allow()
}

func (store *MemoryStore) CleanPartial() {

	logs.Debug(store.which+" Store", "starting cleaning")

	store.mu.Lock()

	lenUnchecked := len(store.unchecked)
	lenChecked := len(store.checked)

	if lenUnchecked == 0 {
		if lenChecked == 0 {
			store.mu.Unlock()
			logs.Debug(store.which+" Store", "nothing in store")
			return
		}
		temp := store.unchecked
		store.unchecked = store.checked
		store.checked = temp
	}

	toClean := lenUnchecked + lenChecked
	if toClean/10 > 0 {
		toClean /= 10
	}
	if toClean > 100 {
		toClean = 100
	}

	count := 0
	cleaned := 0
	for key, client := range store.unchecked {
		if count >= toClean {
			break
		}

		if time.Since(client.lastSeen) < store.maxIdle {
			store.checked[key] = store.unchecked[key]
		} else {
			cleaned++
		}
		delete(store.unchecked, key)

		count++
	}

	store.mu.Unlock()
	logs.Debug(store.which+" Store", "to check "+strconv.Itoa(toClean)+" entries")
	logs.Debug(store.which+" Store", "checked "+strconv.Itoa(count)+" entries")
	logs.Debug(store.which+" Store", "cleaned "+strconv.Itoa(cleaned)+" entries")
}

// transferLocked MUST be called AFTER holding the mutex lock
func (store *MemoryStore) transferLocked(identity string) {

	client, ok := store.unchecked[identity]
	if !ok {
		return
	}

	store.checked[identity] = client
	delete(store.unchecked, identity)
}
