RED    = \033[38;2;255;97;136m
ORANGE = \033[38;2;252;152;103m
YELLOW = \033[38;2;255;216;102m
GREEN  = \033[38;2;169;220;118m
BLUE   = \033[38;2;120;220;232m
RESET  = \033[0m

TARGET = gateway_binary
SRCS = $(shell find . -name '*.go')

all: build

run: $(TARGET)
	@printf '$(BLUE)Running...\n$(RESET)'
	@./$^

build: $(TARGET)

$(TARGET): $(SRCS)
	@$(MAKE) --no-print-directory fmt
	@$(MAKE) --no-print-directory vet
	@printf '$(GREEN)Building...\n$(RESET)'
	@go build -o $@ ./cmd/...

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
	docker compose -f compose.dev.yml up -d

down:
	docker compose down

ps:
	docker ps -a

.PHONY: all build run re clean fclean up down ps
