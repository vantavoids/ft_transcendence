package main

import (
	"net/http"
	"os"

	"github.com/labstack/echo/v5"
	"github.com/labstack/echo/v5/middleware"
)

// ---- handlers ----

func handleBase(c *echo.Context) error {
	return (c.String(http.StatusOK, "Hello world"))
}

func handlePermissions(c *echo.Context) error {
	return (c.JSON(http.StatusOK, struct{ Status string }{Status: "Hello world"}))
}

// ------------------

func main() {
	e := echo.New()

	// Log all requests in stdout using middleware
	e.Use(middleware.RequestLogger())

	e.GET("/", handleBase)
	e.GET("/permissions", handlePermissions)

	// Find port in env or default to 1323
	httpPort := os.Getenv("PERM_PORT")
	if httpPort == "" {
		httpPort = "1323"
	}

	err := e.Start(":" + httpPort) // Start the webserv
	if err != nil {
		e.Logger.Error("failed to start server", "error", err)
		os.Exit(1)
	}
}
