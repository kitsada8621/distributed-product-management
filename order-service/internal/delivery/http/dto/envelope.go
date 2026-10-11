package dto

import "github.com/gofiber/fiber/v3"

type FieldError struct {
	Field   string `json:"field"`
	Code    string `json:"code"`
	Message string `json:"message"`
}

type PageMeta struct {
	Page       int `json:"page"`
	PageSize   int `json:"pageSize"`
	Total      int `json:"total"`
	TotalPages int `json:"totalPages"`
}

type Response struct {
	Status  int          `json:"status"`
	Success bool         `json:"success"`
	Code    string       `json:"code"`
	Message string       `json:"message"`
	Data    any          `json:"data"`
	Meta    any          `json:"meta"`
	Errors  []FieldError `json:"errors"`
}

func Success(c fiber.Ctx, status int, message string, data any, meta any) error {
	return c.Status(status).JSON(Response{
		Status:  status,
		Success: true,
		Code:    "SUCCESS",
		Message: message,
		Data:    data,
		Meta:    meta,
	})
}

func Fail(c fiber.Ctx, status int, code, message string, errs []FieldError) error {
	return c.Status(status).JSON(Response{
		Status:  status,
		Success: false,
		Code:    code,
		Message: message,
		Errors:  errs,
	})
}
