package dto

import (
	"strconv"

	"github.com/gofiber/fiber/v3"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
)

const (
	defaultPage     = 1
	defaultPageSize = 20
	maxPageSize     = 100
)

func ParsePaging(c fiber.Ctx) (page, pageSize int, err error) {
	page, pageErr := parseNumber(c.Query("page"), "page", defaultPage, 1)
	pageSize, sizeErr := parseNumber(c.Query("pageSize"), "pageSize", defaultPageSize, 1)
	if pageSize > maxPageSize {
		sizeErr = invalidField("pageSize", "OUT_OF_RANGE", "pageSize must be at most 100")
	}

	var fields []domain.FieldError
	for _, problem := range []*domain.Error{pageErr, sizeErr} {
		if problem != nil {
			fields = append(fields, problem.Fields...)
		}
	}
	if len(fields) > 0 {
		return 0, 0, &domain.Error{Code: domain.CodeValidation, Message: "Invalid input", Fields: fields}
	}
	return page, pageSize, nil
}

func parseNumber(value, field string, fallback, min int) (int, *domain.Error) {
	if value == "" {
		return fallback, nil
	}
	number, err := strconv.Atoi(value)
	if err != nil {
		return 0, invalidField(field, "INVALID_FORMAT", field+" must be an integer")
	}
	if number < min {
		return 0, invalidField(field, "OUT_OF_RANGE", field+" must be at least 1")
	}
	return number, nil
}

func invalidField(field, code, message string) *domain.Error {
	return &domain.Error{Code: domain.CodeValidation, Fields: []domain.FieldError{{Field: field, Code: code, Message: message}}}
}

func NewMeta(page, pageSize, total int) PageMeta {
	totalPages := (total + pageSize - 1) / pageSize
	return PageMeta{Page: page, PageSize: pageSize, Total: total, TotalPages: totalPages}
}
