package middleware

import (
	"strings"

	"github.com/gofiber/fiber/v3"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
)

type TokenVerifier interface {
	Verify(token string) (domain.Caller, error)
}

type callerKey struct{}

func Authenticate(v TokenVerifier) fiber.Handler {
	return func(c fiber.Ctx) error {
		header := c.Get(fiber.HeaderAuthorization)
		token, found := strings.CutPrefix(header, "Bearer ")
		if !found || token == "" {
			return unauthorized()
		}
		caller, err := v.Verify(token)
		if err != nil {
			return unauthorized()
		}
		c.Locals(callerKey{}, caller)
		return c.Next()
	}
}

func RequireAdmin() fiber.Handler {
	return func(c fiber.Ctx) error {
		if !CurrentCaller(c).Role.IsAdmin() {
			return &domain.Error{Code: domain.CodeForbidden, Message: "You do not have permission"}
		}
		return c.Next()
	}
}

func CurrentCaller(c fiber.Ctx) domain.Caller {
	caller, _ := c.Locals(callerKey{}).(domain.Caller)
	return caller
}

func unauthorized() error {
	return &domain.Error{Code: domain.CodeUnauthorized, Message: "Authentication required"}
}
