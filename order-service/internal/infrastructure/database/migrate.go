package database

import (
	"fmt"

	"github.com/pressly/goose/v3"
	"gorm.io/gorm"

	"github.com/kitsada8621/distributed-product-management/order-service/migrations"
)

func Migrate(db *gorm.DB) error {
	sqlDB, err := db.DB()
	if err != nil {
		return err
	}
	goose.SetBaseFS(migrations.FS)
	if err := goose.SetDialect("sqlite3"); err != nil {
		return err
	}
	if err := goose.Up(sqlDB, "."); err != nil {
		return fmt.Errorf("run migrations: %w", err)
	}
	return nil
}
