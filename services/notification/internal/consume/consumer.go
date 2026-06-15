package consume

import (
	"fmt"
	"os"

	amqp "github.com/rabbitmq/amqp091-go"
)

const (
	autoAck      = false
	queueName    = "notifications"
	exchange     = "events"
	exchangeType = "topic"
)

var events = []string{
	"chat.dm_sent",
	"chat.message_sent",
	"call.incoming",
	"friend.request_sent",
	"guild.invite_created",
	"guild.member_joined",
	"user.deleted",
}

type Consumer struct {
	conn    *amqp.Connection
	channel *amqp.Channel
	tag     string
	done    chan error
}

func NewConsumer() (*Consumer, error) {
	var err error
	c := &Consumer{
		conn:    nil,
		channel: nil,
		tag:     "",
		done:    make(chan error),
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
	deliveries, err := c.channel.Consume(
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

	go handle(deliveries, c.done)

	return c, nil
}

func handle(deliveries <-chan amqp.Delivery, done chan error) {
	cleanup := func() {
		fmt.Printf("Handler: deliveries channel closed")
		done <- nil
	}
	defer cleanup()

	for d := range deliveries {
		// TODO: logique metier mtn
		// if err := process(d); err != nil {
		// 	d.Nack(false, true)
		// 	continue
		// }
		if autoAck == false {
			d.Ack(false)
		}
	}
}
