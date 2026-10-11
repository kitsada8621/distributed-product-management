package database

import (
	"fmt"

	"gorm.io/driver/sqlite"
	"gorm.io/gorm"
	"gorm.io/gorm/logger"
)

const pragmas = "_foreign_keys=on&_journal_mode=WAL&_busy_timeout=5000"

func Open(path string) (*gorm.DB, error) {
	db, err := gorm.Open(sqlite.Open("file:"+path+"?"+pragmas), &gorm.Config{Logger: logger.Discard})
	if err != nil {
		return nil, fmt.Errorf("open sqlite %s: %w", path, err)
	}
	return db, nil
}
