package handler

import (
	"context"
	"encoding/json"
	"fmt"
	"log"
	"net/http"
	"strconv"
	"strings"
	"time"
)

var httpClient = &http.Client{Timeout: 5 * time.Second}

var httpMethods = map[string]bool{
	"get": true, "put": true, "post": true, "delete": true,
	"options": true, "head": true, "patch": true, "trace": true,
}

type specResult struct {
	name string
	spec map[string]any
	err  error
}

func fetchSpec(ctx context.Context, name, baseURL string) specResult {
	req, err := http.NewRequestWithContext(ctx, http.MethodGet, baseURL+"/openapi/v1.json", nil)
	if err != nil {
		return specResult{name: name, err: err}
	}

	resp, err := httpClient.Do(req)
	if err != nil {
		return specResult{name: name, err: err}
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return specResult{name: name, err: fmt.Errorf("status %d", resp.StatusCode)}
	}

	var spec map[string]any
	if err := json.NewDecoder(resp.Body).Decode(&spec); err != nil {
		return specResult{name: name, err: err}
	}

	return specResult{name: name, spec: spec}
}

func tagName(service string) string {
	return strings.ToUpper(service[:1]) + service[1:]
}

func requiresAuth(path string) bool {
	parts := strings.Split(path, "/")
	// /api/<service>/v1/...
	if len(parts) < 5 || parts[1] != "api" {
		return false
	}
	if parts[2] == "auth" {
		return false
	}
	if !strings.HasPrefix(parts[3], "v") {
		return false
	}
	_, err := strconv.Atoi(parts[3][1:])
	return err == nil
}

func rewriteRefs(node any, refTranslations map[string]string) any {
	switch typedNodes := node.(type) {
	case map[string]any:
		updatedObject := make(map[string]any, len(typedNodes))
		for key, childNode := range typedNodes {
			if key == "$ref" {
				if refString, isString := childNode.(string); isString {
					if newRefString, exists := refTranslations[refString]; exists {
						updatedObject[key] = newRefString
						continue
					}
				}
			}
			updatedObject[key] = rewriteRefs(childNode, refTranslations)
		}
		return updatedObject
	case []any:
		updatedArray := make([]any, len(typedNodes))
		for i, childNode := range typedNodes {
			updatedArray[i] = rewriteRefs(childNode, refTranslations)
		}
		return updatedArray
	default:
		return node
	}
}

func processOperations(pathItem map[string]any, tag, fullPath string) {
	secured := requiresAuth(fullPath)
	for method, op := range pathItem {
		if !httpMethods[method] {
			continue
		}
		opMap, ok := op.(map[string]any)
		if !ok {
			continue
		}
		opMap["tags"] = []string{tag}
		if secured {
			opMap["security"] = []map[string]any{{"bearerAuth": []any{}}}
		} else {
			opMap["security"] = []map[string]any{}
		}
	}
}

func AggregateOpenAPI(w http.ResponseWriter, r *http.Request) {
	ctx, cancel := context.WithTimeout(r.Context(), 5*time.Second)
	defer cancel()

	ch := make(chan specResult, len(serviceURLs))
	for name, url := range serviceURLs {
		go func(name, url string) {
			ch <- fetchSpec(ctx, name, url)
		}(name, url)
	}

	paths := map[string]any{}
	schemas := map[string]any{}
	var tags []map[string]any

	for range serviceURLs {
		res := <-ch
		if res.err != nil {
			log.Printf("openapi: skipping %s: %v", res.name, res.err)
			continue
		}

		tag := tagName(res.name)
		tags = append(tags, map[string]any{"name": tag})

		if components, ok := res.spec["components"].(map[string]any); ok {
			if svcSchemas, ok := components["schemas"].(map[string]any); ok {
				renames := make(map[string]string, len(svcSchemas))
				for name := range svcSchemas {
					renames["#/components/schemas/"+name] = "#/components/schemas/" + tag + name
				}
				res.spec = rewriteRefs(res.spec, renames).(map[string]any)

				if components, ok := res.spec["components"].(map[string]any); ok {
					if svcSchemas, ok := components["schemas"].(map[string]any); ok {
						for name, value := range svcSchemas {
							schemas[tag+name] = value
						}
					}
				}
			}
		}

		if svcPaths, ok := res.spec["paths"].(map[string]any); ok {
			for path, item := range svcPaths {
				if pathItem, ok := item.(map[string]any); ok {
					fullPath := "/api/" + res.name + path
					processOperations(pathItem, tag, fullPath)
					paths[fullPath] = pathItem
				}
			}
		}
	}

	merged := map[string]any{
		"openapi": "3.0.0",
		"info": map[string]any{
			"title":   "ft_transcendence",
			"version": "1.0.0",
		},
		"servers": []map[string]any{{"url": "/"}},
		"tags":    tags,
		"paths":   paths,
		"components": map[string]any{
			"schemas": schemas,
			"securitySchemes": map[string]any{
				"bearerAuth": map[string]any{
					"type":         "http",
					"scheme":       "bearer",
					"bearerFormat": "JWT",
				},
			},
		},
	}

	w.Header().Set("Content-Type", "application/json")
	if err := json.NewEncoder(w).Encode(merged); err != nil {
		log.Printf("openapi: encode error: %v", err)
	}
}
