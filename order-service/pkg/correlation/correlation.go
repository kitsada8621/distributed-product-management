package correlation

import (
	"context"

	"github.com/google/uuid"
)

const Header = "X-Correlation-Id"

type contextKey struct{}

func With(ctx context.Context, id string) context.Context {
	return context.WithValue(ctx, contextKey{}, id)
}

func From(ctx context.Context) string {
	id, _ := ctx.Value(contextKey{}).(string)
	return id
}

func NewID() string {
	return uuid.NewString()
}
