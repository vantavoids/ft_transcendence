COMPOSE ?= podman-compose

.PHONY: all up down build re clean fclean logs ps

all: build up

up:
	$(COMPOSE) up -d

build:
	$(COMPOSE) build

down:
	$(COMPOSE) down

re: fclean up

clean: down
	podman image prune -f

fclean: down
	podman system prune -af
	podman volume prune -f

logs:
	$(COMPOSE) logs -f

ps:
	$(COMPOSE) ps
