// Package healthcheck gets status code of all backend services
package healthcheck

import (
	"crypto/hmac"
	"crypto/sha256"
	"encoding/base64"
	"encoding/json"
	"fmt"
	"net/http"
	"strings"
	"time"

	"github.com/vantavoids/ft_transcendence/services/gateway/config"
)

type StatusCode int

func ProbeServices(cfg *config.Config) map[string]StatusCode {

	token := makeJWT(cfg.JWTSecret)
	client := &http.Client{Timeout: 5 * time.Second}
	services := cfg.Services.Slice()
	statusMap := make(map[string]StatusCode, len(services))

	for _, svc := range services {
		url := fmt.Sprintf("http://localhost:8080/api/%s/v1/hello-world", svc)
		req, _ := http.NewRequest(http.MethodGet, url, nil)
		if svc != "auth" {
			req.Header.Set("Authorization", "Bearer "+token)
		}
		resp, err := client.Do(req)
		if err != nil {
			statusMap[svc] = 0
			continue
		}
		resp.Body.Close()
		statusMap[svc] = StatusCode(resp.StatusCode)
	}

	return statusMap
}

func makeJWT(secret string) string {

	header, _ := json.Marshal(map[string]string{"alg": "HS256", "typ": "JWT"})
	payload, _ := json.Marshal(map[string]any{"sub": "healthcheck", "iat": time.Now().Unix()})

	unsigned := b64url(header) + "." + b64url(payload)
	mac := hmac.New(sha256.New, []byte(secret))
	mac.Write([]byte(unsigned))
	return unsigned + "." + b64url(mac.Sum(nil))
}

func b64url(data []byte) string {

	return strings.TrimRight(base64.URLEncoding.EncodeToString(data), "=")
}
