package client

import (
	"context"
	"net/http"
)

type FakeClient struct {
	baseURL    string
	httpClient *http.Client
}

func NewFakeClient(baseURL string, httpClient *http.Client) (*FakeClient, error) {
	return &FakeClient{baseURL: baseURL, httpClient: httpClient}, nil
}

// IsBlockedBy takes two users, checks the relation ship between the two users and return if [targetID] has blocked [senderID].
func (client *FakeClient) IsBlockedBy(ctx context.Context, targetID int64, senderID int64) (bool, error) {
	return false, nil
}
