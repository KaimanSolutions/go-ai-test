package handlers

import (
    "net/http"
)

// ExampleHandler godoc
// @Summary Example endpoint
// @Description This is an example endpoint that returns a simple message.
// @Tags example
// @Accept  json
// @Produce  json
// @Success 200 {object} map[string]string
// @Router /example [get]
func ExampleHandler(w http.ResponseWriter, r *http.Request) {
    w.Header().Set("Content-Type", "application/json")
    w.WriteHeader(http.StatusOK)
    w.Write([]byte(`{"message": "Hello, World!"}`))
}