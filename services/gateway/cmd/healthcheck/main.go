package main

import (
	"crypto/hmac"
	"crypto/sha256"
	"encoding/base64"
	"encoding/json"
	"fmt"
	"net/http"
	"os"
	"strings"
	"time"
)

func b64url(data []byte) string {
	return strings.TrimRight(base64.URLEncoding.EncodeToString(data), "=")
}

func makeJWT(secret string) string {
	header, _ := json.Marshal(map[string]string{"alg": "HS256", "typ": "JWT"})
	payload, _ := json.Marshal(map[string]any{"sub": "healthcheck", "iat": time.Now().Unix()})

	unsigned := b64url(header) + "." + b64url(payload)
	mac := hmac.New(sha256.New, []byte(secret))
	mac.Write([]byte(unsigned))
	return unsigned + "." + b64url(mac.Sum(nil))
}

func main() {
	secret := os.Getenv("JWT_SECRET")
	if secret == "" {
		fmt.Fprintln(os.Stderr, "JWT_SECRET not set")
		os.Exit(1)
	}

	token := makeJWT(secret)
	client := &http.Client{Timeout: 5 * time.Second}

	services := []string{"auth", "guild", "user", "chat", "notification"}

	for _, svc := range services {
		url := fmt.Sprintf("http://localhost:8080/api/%s/v1/hello-world", svc)
		req, _ := http.NewRequest(http.MethodGet, url, nil)
		if svc != "auth" {
			req.Header.Set("Authorization", "Bearer "+token)
		}
		resp, err := client.Do(req)
		if err != nil {
			fmt.Fprintf(os.Stderr, "KO %s: %v\n", svc, err)
			os.Exit(1)
		}
		resp.Body.Close()
		if resp.StatusCode >= 400 {
			fmt.Fprintf(os.Stderr, "KO %s: HTTP %d\n", svc, resp.StatusCode)
			os.Exit(1)
		}
	}

	fmt.Println("OK :D")
}
