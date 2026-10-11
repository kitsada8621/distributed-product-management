package domain

import "errors"

const (
	CodeValidation             = "VALIDATION_ERROR"
	CodeUnauthorized           = "UNAUTHORIZED"
	CodeForbidden              = "FORBIDDEN"
	CodeNotFound               = "NOT_FOUND"
	CodeCannotCancel           = "CANNOT_CANCEL"
	CodeInvalidState           = "INVALID_STATE"
	CodeIdempotencyKeyReused   = "IDEMPOTENCY_KEY_REUSED"
	CodeIdempotencyKeyRequired = "IDEMPOTENCY_KEY_REQUIRED"
)

var (
	ErrNotFound     = errors.New("order not found")
	ErrDuplicateKey = errors.New("idempotency key already used")
	ErrStateChanged = errors.New("order state changed")
)

type FieldError struct {
	Field   string
	Code    string
	Message string
}

type Error struct {
	Code    string
	Message string
	Fields  []FieldError
}

func (e *Error) Error() string {
	return e.Code + ": " + e.Message
}

func NewValidationError(fields ...FieldError) *Error {
	return &Error{Code: CodeValidation, Message: "Invalid input", Fields: fields}
}
