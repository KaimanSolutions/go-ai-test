package database

import (
	"database/sql"
	"fmt"
	"log"
	"os"
)

var db *sql.DB

// InitDB initializes the database connection.
func InitDB() error {
	connStr := os.Getenv("DATABASE_URL")
	if connStr == "" {
		connStr = "postgresql://user:password@localhost:5432/dbname?sslmode=disable" // Replace with your local PostgreSQL connection string
		log.Println("DATABASE_URL environment variable not set, using default connection string.  THIS IS ONLY FOR LOCAL DEVELOPMENT.")
	}

	var err error
	db, err = sql.Open("postgres", connStr)
	if err != nil {
		return fmt.Errorf("failed to open database: %w", err)
	}

	err = db.Ping()
	if err != nil {
		return fmt.Errorf("failed to ping database: %w", err)
	}

	log.Println("Successfully connected to the database!")
	return nil
}

// GetDB returns the database connection.
func GetDB() *sql.DB {
	return db
}

// CloseDB closes the database connection.
func CloseDB() error {
	if db != nil {
		return db.Close()
	}
	return nil
}

// GetDatasets retrieves all records from the datasets table.
func GetDatasets() ([]map[string]interface{}, error) {
	rows, err := db.Query("SELECT * FROM datasets")
	if err != nil {
		return nil, fmt.Errorf("failed to query datasets: %w", err)
	}
	defer rows.Close()

	columns, err := rows.Columns()
	if err != nil {
		return nil, fmt.Errorf("failed to get column names: %w", err)
	}

	var datasets []map[string]interface{}
	for rows.Next() {
		values := make([]interface{}, len(columns))
		scanArgs := make([]interface{}, len(columns))
		for i := range values {
			scanArgs[i] = &values[i]
		}

		err = rows.Scan(scanArgs...)
		if err != nil {
			return nil, fmt.Errorf("failed to scan row: %w", err)
		}

		dataset := make(map[string]interface{})
		for i, col := range columns {
			val := values[i]
			// Handle different data types appropriately
			switch v := val.(type) {
			case []byte:
				dataset[col] = string(v) // Convert byte arrays to strings
			default:
				dataset[col] = v
			}
		}
		datasets = append(datasets, dataset)
	}

	if err = rows.Err(); err != nil {
		return nil, fmt.Errorf("error during rows iteration: %w", err)
	}

	return datasets, nil
}
