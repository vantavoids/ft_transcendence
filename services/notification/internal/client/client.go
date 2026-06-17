package client

import (
	"context"
	"encoding/json"
	"fmt"
	"net/http"
	"time"
)

type Client struct {
	baseURL    string
	httpClient *http.Client
}

func NewClient(baseURL string, httpClient *http.Client) (*Client, error) {
	return &Client{baseURL: baseURL, httpClient: httpClient}, nil
}

// IsBlockedBy takes two users, checks the relation ship between the two users and return if [targetID] has blocked [senderID].
func (client *Client) IsBlockedBy(ctx context.Context, targetID int64, senderID int64) (bool, error) {
	url := fmt.Sprintf("%s/internal/users/%d/relationship-with/%d", client.baseURL, targetID, senderID)

	req, err := http.NewRequestWithContext(ctx, http.MethodGet, url, nil)
	if err != nil {
		return false, err
	}

	resp, err := client.httpClient.Do(req)
	if err != nil {
		return false, err
	}
	defer resp.Body.Close()

	if resp.StatusCode == http.StatusNotFound {
		return false, nil
	}

	if resp.StatusCode != http.StatusOK {
		return false, fmt.Errorf("user service returned %d", resp.StatusCode)
	}

	var body struct {
		Status string    `json:"status"`
		Since  time.Time `json:"since"`
	}
	if err := json.NewDecoder(resp.Body).Decode(&body); err != nil {
		return false, err
	}

	return body.Status == "blocked_by_me", nil
}
