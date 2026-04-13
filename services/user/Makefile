COMPOSE ?= docker compose

.PHONY: up down build logs ps restart

up:
	$(COMPOSE) up -d

build:
	$(COMPOSE) build

down:
	$(COMPOSE) down

logs:
	$(COMPOSE) logs -f

ps:
	$(COMPOSE) ps

restart:
	$(COMPOSE) down
	$(COMPOSE) up -d
