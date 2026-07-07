package email

import (
	"fmt"
	"os"
	"strconv"

	mail "github.com/wneessen/go-mail"
)

func newClient() (*mail.Client, error) {
	host := os.Getenv("SMTP_HOST")
	if host == "" {
		return nil, fmt.Errorf("missing stmp host")
	}

	port, err := strconv.Atoi(os.Getenv("SMTP_PORT"))
	if err != nil {
		return nil, fmt.Errorf("invalid stmp port")
	}

	user := os.Getenv("SMTP_USER")

	pass := os.Getenv("SMTP_PASS")

	return mail.NewClient(host,
		mail.WithPort(port),
		mail.WithTLSPolicy(mail.NoTLS),
		mail.WithUsername(user),
		mail.WithPassword(pass),
	)
}
