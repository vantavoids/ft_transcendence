package failure

import "errors"

var (
	FailPermanent = errors.New("permanent error")
	FailTemporary = errors.New("temporary error")

	Discarded = errors.New("discarded notif")
)

var (
	ErrNotFound  = errors.New("not found error")
	ErrForbidden = errors.New("forbidden error")
)
