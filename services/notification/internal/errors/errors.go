package errors

import "errors"

var (
	ErrorPermanent = errors.New("permanent error")
	ErrorTemporary = errors.New("temporary error")
)
