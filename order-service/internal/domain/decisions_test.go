package domain

import (
	"errors"
	"testing"
	"time"
)

type statePair struct {
	status Status
	stock  StockStatus
}

var allStates = []statePair{
	{Pending, Reserving}, {Pending, Reserved},
	{Confirmed, Reserved}, {Rejected, Reserving}, {Cancelled, Reserved}, {Cancelled, Reserving},
}

func orderIn(state statePair) *Order {
	return &Order{ID: "o1", Status: state.status, StockStatus: state.stock, CreatedAt: testNow, UpdatedAt: testNow}
}

func errorCode(err error) string {
	var apiErr *Error
	if errors.As(err, &apiErr) {
		return apiErr.Code
	}
	return ""
}

func TestCancel(t *testing.T) {
	for _, state := range allStates {
		o := orderIn(state)
		err := o.Cancel(testNow.Add(time.Minute))
		if state.status == Pending {
			if err != nil || o.Status != Cancelled || !o.UpdatedAt.Equal(testNow.Add(time.Minute)) {
				t.Errorf("%v: err=%v order=%+v", state, err, o)
			}
			continue
		}
		if errorCode(err) != CodeCannotCancel || o.Status != state.status {
			t.Errorf("%v: err=%v status=%s", state, err, o.Status)
		}
	}
}

func TestConfirm(t *testing.T) {
	for _, state := range allStates {
		o := orderIn(state)
		err := o.Confirm(testNow)
		if state.status == Pending && state.stock == Reserved {
			if err != nil || o.Status != Confirmed {
				t.Errorf("%v: err=%v status=%s", state, err, o.Status)
			}
			continue
		}
		if errorCode(err) != CodeInvalidState || o.Status != state.status {
			t.Errorf("%v: err=%v status=%s", state, err, o.Status)
		}
	}
}

func TestReject(t *testing.T) {
	for _, state := range allStates {
		o := orderIn(state)
		err := o.Reject("Item damaged", testNow)
		if state.status == Pending && state.stock == Reserved {
			if err != nil || o.Status != Rejected || o.Rejection == nil ||
				o.Rejection.Type != AdminRejected || *o.Rejection.Message != "Item damaged" {
				t.Errorf("%v: err=%v order=%+v", state, err, o)
			}
			continue
		}
		if errorCode(err) != CodeInvalidState || o.Status != state.status || o.Rejection != nil {
			t.Errorf("%v: err=%v order=%+v", state, err, o)
		}
	}
}

func TestRejectBlankReasonIsValidationEvenInWrongState(t *testing.T) {
	for _, state := range allStates {
		o := orderIn(state)
		if err := o.Reject("   ", testNow); errorCode(err) != CodeValidation {
			t.Errorf("%v: err=%v", state, err)
		}
		if o.Status != state.status || o.Rejection != nil {
			t.Errorf("%v: order mutated: %+v", state, o)
		}
	}
}
