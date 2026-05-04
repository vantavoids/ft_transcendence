COMPOSE ?= docker compose

.PHONY: up down build logs ps restart dummy-user

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

dummy-user:
	./scripts/create_dummy_user.sh
