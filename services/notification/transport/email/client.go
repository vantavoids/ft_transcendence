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
		return nil, fmt.Errorf("missing smtp host")
	}

	port, err := strconv.Atoi(os.Getenv("SMTP_PORT"))
	if err != nil {
		return nil, fmt.Errorf("invalid smtp port")
	}

	return mail.NewClient(host,
		mail.WithPort(port),
		mail.WithTLSPolicy(mail.NoTLS),
	)
}

func Send(name string, to string, subject string, data any) error {

	text, html, err := render(name, data)
	if err != nil {
		return fmt.Errorf("render %s: %w", name, err)
	}

	msg := mail.NewMsg()

	if err := msg.From(os.Getenv("SMTP_FROM")); err != nil {
		return fmt.Errorf("failed to set FROM address: %w", err)
	}

	if err := msg.To(to); err != nil {
		return fmt.Errorf("failed to set TO address: %s", err)
	}

	msg.Subject(subject)
	msg.SetBodyString(mail.TypeTextPlain, text)
	msg.AddAlternativeString(mail.TypeTextHTML, html)

	client, err := newClient()
	if err != nil {
		return fmt.Errorf("failed to create smtp client: %w", err)
	}

	return client.DialAndSend(msg)
}
