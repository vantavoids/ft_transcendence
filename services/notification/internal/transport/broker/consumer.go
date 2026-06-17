package broker

import (
	"context"
	"errors"
	"fmt"
	"log"
	"os"
	"time"

	amqp "github.com/rabbitmq/amqp091-go"
	er "github.com/vantavoids/ft_transcendence/services/notification/internal/errors"
	notif "github.com/vantavoids/ft_transcendence/services/notification/internal/notification"
)

const (
	queueName    = "notifications"
	exchange     = "events"
	exchangeType = "topic"
)

var events = []string{
	"chat.message_sent",
	"chat.dm_sent",
	"call.incoming",
	"friend.request_sent",
	"guild.invite_created",
	"guild.member_joined",
	"user.deleted",
}

type Consumer struct {
	conn       *amqp.Connection
	channel    *amqp.Channel
	deliveries <-chan amqp.Delivery
	tag        string
	done       chan error
}

func NewConsumer() (*Consumer, error) {
	var err error
	c := &Consumer{
		conn:       nil,
		channel:    nil,
		deliveries: nil,
		tag:        "",
		done:       make(chan error),
	}

	// Try and retries to connect to the rabbitmq connection
	var conn *amqp.Connection
	for attempt := 1; attempt <= 10; attempt++ {
		conn, err = amqp.Dial(os.Getenv("AMQP_URL"))
		if err == nil {
			break
		}
		log.Printf("rabbitmq not ready (attempt %d/10): %v", attempt, err)
		time.Sleep(3 * time.Second)
	}
	if err != nil {
		return nil, fmt.Errorf("dial after retries: %w", err)
	}
	log.Printf("rabbitmq ready")
	c.conn = conn

	// Open a channel to the said connection
	c.channel, err = c.conn.Channel()
	if err != nil {
		return nil, fmt.Errorf("Channel: %s", err)
	}

	// Declare the mail sorting office (On est inscrit au bureau de tri)
	if err = c.channel.ExchangeDeclare(
		exchange,     // name of the exchange
		exchangeType, // type
		true,         // durable
		false,        // delete when complete
		false,        // internal
		false,        // noWait
		nil,          // arguments
	); err != nil {
		return nil, fmt.Errorf("Exchange Declare: %s", err)
	}

	// Declare my letter box (On a declarer qu on possede une boite au lettre)
	queue, err := c.channel.QueueDeclare(
		queueName, // name of the queue
		true,      // durable
		false,     // delete when unused
		false,     // exclusive
		false,     // noWait
		nil,       // arguments
	)
	if err != nil {
		return nil, fmt.Errorf("Queue Declare: %s", err)
	}

	// Ready to sort only these events into my letterbox (On veut que recevoir ce type de lettre)
	for _, key := range events {
		if err = c.channel.QueueBind(
			queue.Name, // name of the queue
			key,        // bindingKey
			exchange,   // sourceExchange
			false,      // noWait
			nil,        // arguments
		); err != nil {
			return nil, fmt.Errorf("Queue Bind: %s", err)
		}
	}

	// Ready to open these events in my letterbox
	c.deliveries, err = c.channel.Consume(
		queue.Name, // name
		c.tag,      // consumerTag,
		false,      // autoAck
		false,      // exclusive
		false,      // noLocal
		false,      // noWait
		nil,        // arguments
	)
	if err != nil {
		return nil, err
	}

	return c, nil
}

func (c *Consumer) Run(svc *notif.Service) {
	defer func() {
		fmt.Printf("Run: deliveries channel closed\n")
		c.done <- nil
	}()

	// TODO: for the moment this is a single worker working on event one by one,
	// in the future if needed, we can readapt this to add a goroutine per worker
	for d := range c.deliveries {
		handle(svc, d)
	}
}

func handle(svc *notif.Service, d amqp.Delivery) {
	ctx, cancel := context.WithTimeout(context.Background(), 30*time.Second)
	defer cancel()

	err := Dispatch(ctx, svc, d)
	if err == nil {
		d.Ack(false)
	}

	if errors.Is(err, er.ErrorPermanent) {
		log.Printf("permanent error, dropping: %v", err)
		d.Nack(false, false)
		return
	}
	d.Nack(false, true) // If there is an error but not a permanent one, just retry
}
