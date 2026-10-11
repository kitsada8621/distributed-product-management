package domain

import (
	"fmt"
	"time"
)

type ReservedItem struct {
	ProductID      string
	Name           string
	UnitPriceMinor int64
}

type FailedItem struct {
	ProductID string
	Reason    FailureReason
	Requested int
	Available int
}

func (o *Order) ApplyReserved(items []ReservedItem, now time.Time) (applied bool, err error) {
	if o.Status != Pending || o.StockStatus == Reserved {
		return false, nil
	}
	if len(items) != len(o.Items) {
		return false, fmt.Errorf("order %s has %d items but stock.reserved has %d", o.ID, len(o.Items), len(items))
	}

	byProduct := make(map[string]ReservedItem, len(items))
	for _, item := range items {
		byProduct[item.ProductID] = item
	}
	for _, item := range o.Items {
		if _, ok := byProduct[item.ProductID]; !ok {
			return false, fmt.Errorf("order %s item %s missing in stock.reserved", o.ID, item.ProductID)
		}
	}

	var total int64
	for i := range o.Items {
		reserved := byProduct[o.Items[i].ProductID]
		name := reserved.Name
		price := reserved.UnitPriceMinor
		o.Items[i].Name = &name
		o.Items[i].UnitPriceMinor = &price
		total += price * int64(o.Items[i].Quantity)
	}
	o.TotalMinor = &total
	o.StockStatus = Reserved
	o.UpdatedAt = now.UTC()
	return true, nil
}

func (o *Order) ApplyRejected(failed []FailedItem, now time.Time) (applied bool, err error) {
	if o.Status != Pending || o.StockStatus != Reserving {
		return false, nil
	}
	index := make(map[string]int, len(o.Items))
	for i, item := range o.Items {
		index[item.ProductID] = i
	}
	for _, item := range failed {
		if _, ok := index[item.ProductID]; !ok {
			return false, fmt.Errorf("order %s has no item %s", o.ID, item.ProductID)
		}
	}

	for _, item := range failed {
		o.Items[index[item.ProductID]].Failure = &ItemFailure{
			Reason:    item.Reason,
			Requested: item.Requested,
			Available: item.Available,
		}
	}
	o.Status = Rejected
	o.Rejection = &Rejection{Type: StockUnavailable}
	o.UpdatedAt = now.UTC()
	return true, nil
}

func (o *Order) Timeout(now time.Time) bool {
	if o.Status != Pending || o.StockStatus != Reserving {
		return false
	}
	o.Status = Rejected
	o.Rejection = &Rejection{Type: TimeoutRejection}
	o.UpdatedAt = now.UTC()
	return true
}
