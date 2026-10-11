package dto

type CreateRequest struct {
	Items []struct {
		ProductID string `json:"productId"`

		Quantity float64 `json:"quantity"`
	} `json:"items"`
	ShippingAddress ShippingAddress `json:"shippingAddress"`
	PaymentMethod   string          `json:"paymentMethod"`
}

type ShippingAddress struct {
	RecipientName string `json:"recipientName"`
	Phone         string `json:"phone"`
	AddressLine   string `json:"addressLine"`
	Province      string `json:"province"`
	PostalCode    string `json:"postalCode"`
}

type RejectRequest struct {
	Reason string `json:"reason"`
}
