package routes

import (
    "net/http"
    "go-api-project/internal/handlers"
)

// SetupRoutes initializes the API routes and associates them with their respective handlers.
func SetupRoutes() *http.ServeMux {
    mux := http.NewServeMux()
    
    // Define the API routes
    mux.HandleFunc("/example", handlers.ExampleHandler)

    return mux
}