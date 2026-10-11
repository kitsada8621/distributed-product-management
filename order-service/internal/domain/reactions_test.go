package domain

import "testing"

func twoItemOrder(t *testing.T) *Order {
	t.Helper()
	o, err := New(testCaller, []ItemInput{{"p1", 2}, {"p2", 3}}, testAddress, CashOnDelivery, testNow)
	if err != nil {
		t.Fatal(err)
	}
	return o
}

func TestApplyReserved(t *testing.T) {
	o := twoItemOrder(t)
	applied, err := o.ApplyReserved([]ReservedItem{
		{"p2", "Mouse", 50000},
		{"p1", "Keyboard", 129000},
	}, testNow)
	if !applied || err != nil {
		t.Fatalf("applied=%v err=%v", applied, err)
	}
	if o.StockStatus != Reserved || o.Status != Pending {
		t.Errorf("state = %s/%s", o.Status, o.StockStatus)
	}
	if *o.Items[0].Name != "Keyboard" || *o.Items[0].UnitPriceMinor != 129000 || *o.Items[1].Name != "Mouse" {
		t.Errorf("items = %+v", o.Items)
	}
	if want := int64(129000*2 + 50000*3); *o.TotalMinor != want {
		t.Errorf("total = %d, want %d", *o.TotalMinor, want)
	}
}

func TestApplyReservedSkipsLateOrRepeated(t *testing.T) {
	items := []ReservedItem{{"p1", "A", 100}, {"p2", "B", 200}}
	for _, state := range allStates {
		if state.status == Pending && state.stock == Reserving {
			continue
		}
		o := orderIn(state)
		o.Items = twoItemOrder(t).Items
		applied, err := o.ApplyReserved(items, testNow)
		if applied || err != nil || o.Items[0].Name != nil {
			t.Errorf("%v: applied=%v err=%v", state, applied, err)
		}
	}
}

func TestApplyReservedItemMismatch(t *testing.T) {
	cases := map[string][]ReservedItem{
		"unknown product": {{"p1", "A", 1}, {"px", "B", 2}},
		"missing product": {{"p1", "A", 1}},
	}
	for name, items := range cases {
		o := twoItemOrder(t)
		applied, err := o.ApplyReserved(items, testNow)
		if applied || err == nil || o.StockStatus != Reserving || o.Items[0].Name != nil {
			t.Errorf("%s: applied=%v err=%v order=%+v", name, applied, err, o)
		}
	}
}

func TestApplyRejected(t *testing.T) {
	o := twoItemOrder(t)
	applied, err := o.ApplyRejected([]FailedItem{{"p2", InsufficientStock, 3, 1}}, testNow)
	if !applied || err != nil {
		t.Fatalf("applied=%v err=%v", applied, err)
	}
	if o.Status != Rejected || o.Rejection.Type != StockUnavailable || o.Rejection.Message != nil {
		t.Errorf("order = %+v", o)
	}
	if o.Items[0].Failure != nil || o.Items[1].Failure.Requested != 3 || o.Items[1].Failure.Available != 1 {
		t.Errorf("items = %+v", o.Items)
	}
}

func TestApplyRejectedSkips(t *testing.T) {
	for _, state := range allStates {
		if state.status == Pending && state.stock == Reserving {
			continue
		}
		o := orderIn(state)
		applied, err := o.ApplyRejected(nil, testNow)
		if applied || err != nil || o.Status != state.status {
			t.Errorf("%v: applied=%v err=%v status=%s", state, applied, err, o.Status)
		}
	}
}

func TestApplyRejectedUnknownProduct(t *testing.T) {
	o := twoItemOrder(t)
	applied, err := o.ApplyRejected([]FailedItem{{"px", InsufficientStock, 1, 0}}, testNow)
	if applied || err == nil || o.Status != Pending {
		t.Errorf("applied=%v err=%v status=%s", applied, err, o.Status)
	}
}

func TestTimeout(t *testing.T) {
	for _, state := range allStates {
		o := orderIn(state)
		got := o.Timeout(testNow)
		want := state.status == Pending && state.stock == Reserving
		if got != want {
			t.Errorf("%v: got %v want %v", state, got, want)
		}
		if want && (o.Status != Rejected || o.Rejection.Type != TimeoutRejection) {
			t.Errorf("%v: order = %+v", state, o)
		}
		if !want && o.Status != state.status {
			t.Errorf("%v: status changed to %s", state, o.Status)
		}
	}
}
