package tunnel

import (
	"context"
	"net/http"
)

type FakeTunnel struct {
	baseURL    string
	httpClient *http.Client
}

func NewFakeTunnel(baseURL string, httpClient *http.Client) (*FakeTunnel, error) {
	return &FakeTunnel{baseURL: baseURL, httpClient: httpClient}, nil
}

// IsBlockedBy takes two users, checks the relation ship between the two users and return if [targetID] has blocked [senderID].
func (client *FakeTunnel) IsBlockedBy(ctx context.Context, targetID int64, senderID int64) (bool, error) {
	return false, nil
}
