package main

import (
    "log"
    "net/http"
    "go-api-project/internal/routes"
)

func main() {
    mux := http.NewServeMux()
    routes.SetupRoutes(mux)

    server := &http.Server{
        Addr:    ":8080",
        Handler: mux,
    }

    log.Println("Starting server on :8080")
    if err := server.ListenAndServe(); err != nil {
        log.Fatalf("Could not start server: %s\n", err)
    }
}