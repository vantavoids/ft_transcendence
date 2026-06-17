package broker

import (
	"context"
	"fmt"
	"os"
	"time"

	amqp "github.com/rabbitmq/amqp091-go"
	notif "github.com/vantavoids/ft_transcendence/services/notification/internal/notification"
)

const (
	autoAck      = false
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

	c.conn, err = amqp.Dial(os.Getenv("AMQP_URL"))
	if err != nil {
		return nil, fmt.Errorf("Dial: %s", err)
	}

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
		autoAck,    // autoAck
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

	if err := Dispatch(ctx, svc, d); err != nil {
		// TODO: If the json is corrupted this will go on an infinite retry
		// implement a err fail check with precise err type returned in Dispatch (See service.go)
		d.Nack(false, true)
		return
	}

	if !autoAck {
		d.Ack(false)
	}
}
