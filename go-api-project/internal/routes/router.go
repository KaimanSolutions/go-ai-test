package routes

import (
	"go-api-project/internal/handlers"
	"net/http"
)

// SetupRoutes initializes the API routes and associates them with their respective handlers.
func SetupRoutes() *http.ServeMux {
	mux := http.NewServeMux()

	// Define the API routes
	mux.HandleFunc("GET /example", handlers.ExampleHandler)

	return mux
}
