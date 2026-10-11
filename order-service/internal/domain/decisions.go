package domain

import (
	"strings"
	"time"
)

func (o *Order) Cancel(now time.Time) error {
	if o.Status != Pending {
		return &Error{Code: CodeCannotCancel, Message: "Order can no longer be cancelled"}
	}
	o.Status = Cancelled
	o.UpdatedAt = now.UTC()
	return nil
}

func (o *Order) Confirm(now time.Time) error {
	if err := o.requireReserved(); err != nil {
		return err
	}
	o.Status = Confirmed
	o.UpdatedAt = now.UTC()
	return nil
}

func (o *Order) Reject(reason string, now time.Time) error {
	reason = strings.TrimSpace(reason)
	if reason == "" {
		return NewValidationError(FieldError{Field: "reason", Code: "REQUIRED", Message: "reason is required"})
	}
	if err := o.requireReserved(); err != nil {
		return err
	}
	o.Status = Rejected
	o.Rejection = &Rejection{Type: AdminRejected, Message: &reason}
	o.UpdatedAt = now.UTC()
	return nil
}

func (o *Order) requireReserved() error {
	if o.Status != Pending || o.StockStatus != Reserved {
		return &Error{Code: CodeInvalidState, Message: "Order cannot be decided in its current state"}
	}
	return nil
}
