package middleware

import (
	"errors"
	"log/slog"
	"net/http"

	"github.com/gofiber/fiber/v3"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/delivery/http/dto"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
	"github.com/kitsada8621/distributed-product-management/order-service/pkg/correlation"
)

var statusByCode = map[string]int{
	domain.CodeValidation:             http.StatusUnprocessableEntity,
	domain.CodeUnauthorized:           http.StatusUnauthorized,
	domain.CodeForbidden:              http.StatusForbidden,
	domain.CodeNotFound:               http.StatusNotFound,
	domain.CodeCannotCancel:           http.StatusConflict,
	domain.CodeInvalidState:           http.StatusConflict,
	domain.CodeIdempotencyKeyReused:   http.StatusConflict,
	domain.CodeIdempotencyKeyRequired: http.StatusBadRequest,
}

func ErrorHandler(c fiber.Ctx, err error) error {
	var domainErr *domain.Error
	if errors.As(err, &domainErr) {
		if status, known := statusByCode[domainErr.Code]; known {
			return dto.Fail(c, status, domainErr.Code, domainErr.Message, fieldErrors(domainErr.Fields))
		}
	}

	var bindErr *fiber.BindError
	if errors.As(err, &bindErr) {
		return dto.Fail(c, http.StatusBadRequest, "BAD_REQUEST", "Malformed request", nil)
	}

	var fiberErr *fiber.Error
	if errors.As(err, &fiberErr) {
		switch fiberErr.Code {
		case http.StatusBadRequest:
			return dto.Fail(c, http.StatusBadRequest, "BAD_REQUEST", "Malformed request", nil)
		case http.StatusNotFound, http.StatusMethodNotAllowed:
			return dto.Fail(c, http.StatusNotFound, "NOT_FOUND", "Resource not found", nil)
		}
	}

	slog.ErrorContext(c.Context(), "unexpected error",
		"error", err, "correlationId", correlation.From(c.Context()))
	return dto.Fail(c, http.StatusInternalServerError, "INTERNAL_ERROR", "Unexpected error", nil)
}

func fieldErrors(fields []domain.FieldError) []dto.FieldError {
	if len(fields) == 0 {
		return nil
	}
	result := make([]dto.FieldError, 0, len(fields))
	for _, field := range fields {
		result = append(result, dto.FieldError{Field: field.Field, Code: field.Code, Message: field.Message})
	}
	return result
}
