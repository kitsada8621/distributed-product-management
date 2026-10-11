package main

import (
	"context"
	"fmt"
	"log/slog"
	"net"
	"sync"
	"time"

	"github.com/gofiber/fiber/v3"
	"gorm.io/gorm"

	"github.com/kitsada8621/distributed-product-management/order-service/config"
	httpserver "github.com/kitsada8621/distributed-product-management/order-service/internal/delivery/http"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/delivery/http/handler"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/delivery/http/middleware"
	deliverykafka "github.com/kitsada8621/distributed-product-management/order-service/internal/delivery/kafka"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain/event"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/infrastructure/database"
	infrakafka "github.com/kitsada8621/distributed-product-management/order-service/internal/infrastructure/kafka"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/infrastructure/repository"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/infrastructure/token"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/infrastructure/worker"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/usecase"
)

const (
	shutdownTimeout  = 5 * time.Second
	consumerAttempts = 3
	consumerDelay    = 200 * time.Millisecond
)

func run(ctx context.Context, cfg config.Config, logger *slog.Logger, ln net.Listener) error {
	db, err := database.Open(cfg.DBPath)
	if err != nil {
		return err
	}
	defer closeDB(db)
	if err := database.Migrate(db); err != nil {
		return err
	}

	store := repository.NewStore(db)
	service := usecase.NewService(store, time.Now)

	app, err := newHTTPApp(cfg, logger, service)
	if err != nil {
		return err
	}

	workerCtx, stopWorkers := context.WithCancel(ctx)
	defer stopWorkers()
	var workers sync.WaitGroup
	startSweeper(workerCtx, &workers, cfg, logger, service)
	if cfg.KafkaEnabled {
		closeKafka, err := startKafka(workerCtx, &workers, cfg, logger, store, service)
		if err != nil {
			return err
		}
		defer closeKafka()
	}

	serverErr := make(chan error, 1)
	go func() {
		serverErr <- app.Listener(ln, fiber.ListenConfig{DisableStartupMessage: true})
	}()
	logger.Info("starting", "addr", ln.Addr().String(), "kafka", cfg.KafkaEnabled)

	select {
	case err := <-serverErr:
		stopWorkers()
		workers.Wait()
		return err
	case <-ctx.Done():
	}

	err = app.ShutdownWithTimeout(shutdownTimeout)
	stopWorkers()
	workers.Wait()
	logger.Info("stopped")
	return err
}

func newHTTPApp(cfg config.Config, logger *slog.Logger, service *usecase.Service) (*fiber.App, error) {
	key, err := token.LoadPublicKey(cfg.JWTPublicKeyPath)
	if err != nil {
		return nil, err
	}
	verifier := token.NewVerifier(key, cfg.JWTIssuer)

	app := httpserver.NewApp(logger)
	httpserver.Register(app, handler.NewHandler(service), middleware.Authenticate(verifier))
	return app, nil
}

func startSweeper(ctx context.Context, workers *sync.WaitGroup, cfg config.Config, logger *slog.Logger, service *usecase.Service) {
	sweep := func(ctx context.Context) error {
		_, err := service.RejectStale(ctx, cfg.ReserveTimeout)
		return err
	}
	goWorker(workers, func() { worker.Every(ctx, "order-sweeper", cfg.SweeperInterval, logger, sweep) })
}

func startKafka(ctx context.Context, workers *sync.WaitGroup, cfg config.Config, logger *slog.Logger, store *repository.Store, service *usecase.Service) (func(), error) {
	client, err := infrakafka.NewClient(cfg.KafkaBrokers)
	if err != nil {
		return nil, err
	}
	if err := infrakafka.EnsureTopics(ctx, client, infrakafka.OrderTopics()); err != nil {
		client.Close()
		return nil, err
	}

	relay := usecase.NewRelay(store, infrakafka.NewPublisher(client), time.Now)
	goWorker(workers, func() { worker.Every(ctx, "outbox-relay", cfg.RelayInterval, logger, relay.RunOnce) })

	dispatcher := deliverykafka.NewDispatcher(service)
	processor := deliverykafka.NewProcessor(dispatcher, store, consumerAttempts, consumerDelay, time.Now)
	topics := []string{event.TopicStockReserved, event.TopicStockRejected}
	stockConsumer, err := deliverykafka.NewConsumer(cfg.KafkaBrokers, cfg.KafkaGroupID, topics, processor, logger)
	if err != nil {
		client.Close()
		return nil, err
	}
	goWorker(workers, func() {
		if err := stockConsumer.Run(ctx); err != nil {
			logger.Error("stock consumer stopped", "error", err)
		}
	})

	return func() {
		stockConsumer.Close()
		client.Close()
	}, nil
}

func goWorker(workers *sync.WaitGroup, fn func()) {
	workers.Add(1)
	go func() {
		defer workers.Done()
		fn()
	}()
}

func closeDB(db *gorm.DB) {
	if sqlDB, err := db.DB(); err == nil {
		if err := sqlDB.Close(); err != nil {
			fmt.Println("close database:", err)
		}
	}
}
