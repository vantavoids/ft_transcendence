package snowflake

import (
	"fmt"
	"sync"
	"time"
)

const (
	workerIdBits = 10
	sequenceBits = 12

	maxWorkerId = (1 << workerIdBits) - 1
	maxSequence = (1 << sequenceBits) - 1

	epoch = 1704067200000
)

type Generator struct {
	workerID  int64
	processID int64

	mu            sync.Mutex
	sequence      int64
	lastTimestamp int64
}

func NewGenerator(workerID, processID int64) (*Generator, error) {
	if workerID < 0 || maxWorkerId < workerID {
		return nil, fmt.Errorf("WorkerID must be between 0 and %d, but got %d", maxWorkerId, workerID)
	}
	if processID < 0 || 31 < processID {
		return nil, fmt.Errorf("ProcessID must be between 0 and 31, but got %d", processID)
	}
	return &Generator{workerID: workerID, processID: processID}, nil
}

func (g *Generator) Generate() (int64, error) {
	g.mu.Lock()
	defer g.mu.Unlock()

	now := time.Now().UnixMilli()

	if now < g.lastTimestamp {
		return -1, fmt.Errorf("Clock moved backwards. Refusing to generate ID for timestamp %d ms. Last known timestamp was %d ms.", now, g.lastTimestamp)
	}

	if now == g.lastTimestamp {
		g.sequence = (g.sequence + 1) & maxSequence
		if g.sequence == 0 {
			now = waitUntilNextMillisec(g.lastTimestamp)
		}
	} else if now > g.lastTimestamp {
		g.sequence = 0
	}

	g.lastTimestamp = now

	id := ((now - epoch) << (workerIdBits + sequenceBits)) | (g.workerID << sequenceBits) | g.sequence
	return id, nil
}

func waitUntilNextMillisec(lastTimestamp int64) int64 {
	now := time.Now().UnixMilli()
	for now <= lastTimestamp {
		now = time.Now().UnixMilli()
	}
	return now
}
