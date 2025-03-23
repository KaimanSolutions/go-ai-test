package main

import (
	"go-api-project/internal/routes"
	"log"
	"net/http"
)

func main() {
	mux := http.NewServeMux()
	routes.SetupRoutes()

	server := &http.Server{
		Addr:    ":8080",
		Handler: mux,
	}

	log.Println("Starting server on :8080")
	if err := server.ListenAndServe(); err != nil {
		log.Fatalf("Could not start server: %s\n", err)
	}
}
