RED    = \033[38;2;255;97;136m
ORANGE = \033[38;2;252;152;103m
YELLOW = \033[38;2;255;216;102m
GREEN  = \033[38;2;169;220;118m
BLUE   = \033[38;2;120;220;232m
RESET  = \033[0m

TARGET = gateway_binary
SRCS = $(shell find . -name '*.go')

# container engine: podman preferred, docker fallback; override with ENGINE=docker
ENGINE ?= $(shell if command -v podman >/dev/null 2>&1; then echo podman; elif command -v docker >/dev/null 2>&1; then echo docker; fi)
ifeq ($(ENGINE),docker)
    COMPOSE := docker compose
    DOCKER  := docker
else
    COMPOSE := podman-compose
    DOCKER  := podman
endif

all: build

run: $(TARGET)
	@printf '$(BLUE)Running...\n$(RESET)'
	@./$^

build: $(TARGET)

$(TARGET): $(SRCS)
	@$(MAKE) --no-print-directory fmt
	@$(MAKE) --no-print-directory vet
	@printf '$(GREEN)Building...\n$(RESET)'
	@go build -o $@ ./cmd/gateway

fmt:
	@printf '$(ORANGE)Formatting...\n$(RESET)'
	@go fmt ./...

vet:
	@printf '$(YELLOW)Vetting...\n$(RESET)'
	@go vet ./...

re:
	@$(MAKE) --no-print-directory clean
	@$(MAKE) --no-print-directory build

clean:
	@printf '$(RED)Delete binary...\n$(RESET)'
	@rm -rf $(TARGET)

fclean: clean

up:
	$(COMPOSE) up -d

down:
	$(COMPOSE) down

ps:
	$(DOCKER) ps -a

.PHONY: all build run re clean fclean up down ps
