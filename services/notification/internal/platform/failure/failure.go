package failure

import "errors"

var (
	FailPermanent = errors.New("permanent error")
	FailTemporary = errors.New("temporary error")
)

var (
	ErrNotFound  = errors.New("not found error")
	ErrForbidden = errors.New("forbidden error")
)
