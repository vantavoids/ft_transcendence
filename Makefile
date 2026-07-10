NAME         := ft_transcendence
ANNOUNCER    := Announcer

# engine autodetect (podman preferred, docker fallback), override with
# `make ENGINE=docker` to force docker, or set DOCKER / COMPOSE directly
ENGINE       ?= $(shell if command -v podman >/dev/null 2>&1; then echo podman; elif command -v docker >/dev/null 2>&1; then echo docker; fi)
ifeq ($(ENGINE),docker)
    DOCKER   := docker
    COMPOSE  := docker compose
else
    DOCKER   := podman
    COMPOSE  := podman-compose
endif

MIGRATE_SERVICES = $(filter-out $(shell $(COMPOSE) config --services 2>/dev/null),$(shell $(COMPOSE) --profile migrate config --services 2>/dev/null))
DB_SERVICES      = $(filter %-db,$(shell $(COMPOSE) config --services 2>/dev/null))
BUILT_IMAGES     = $(shell $(COMPOSE) --profile migrate config 2>/dev/null | awk '/^  [a-z].*:$$/{if(b&&i)print i;b=0;i=""} /^    build:/{b=1} /^    image:/{i=$$2} END{if(b&&i)print i}')

ENV_FILES := .env \
             frontend/.env \
             services/auth/.env \
             services/chat/.env \
             services/gateway/.env \
             services/guild/.env \
             services/notification/.env \
             services/user/.env

SECRETMANAGER_DIR := tools/secretman

# each env file paired with the name of its encrypted counterpart under
# infra/secretman/secrets/<name>.env.crypt. the names are irregular (root <- .env,
# front <- frontend/.env), so map them explicitly. check-env warns on key drift
# between an env file and its secret (SOPS keeps the keys in plaintext).
SECRETS_DIR := infra/secretman/secrets
ENV_CRYPT_PAIRS := \
    .env:root \
    frontend/.env:front \
    services/auth/.env:auth \
    services/chat/.env:chat \
    services/gateway/.env:gateway \
    services/guild/.env:guild \
    services/notification/.env:notification \
    services/user/.env:user

get_color = $(if $(filter Purple,$(1)),$(shell tput setaf 5),$(if $(filter Red,$(1)),$(shell tput setaf 1),$(if $(filter Cyan,$(1)),$(shell tput setaf 6),$(if $(filter Blue,$(1)),$(shell tput setaf 4),$(if $(filter Yellow,$(1)),$(shell tput setaf 3),$(if $(filter Green,$(1)),$(shell tput setaf 2),$(shell tput sgr0)))))))
ann = $(call get_color,$(1))[$(call get_color,Off)$(ANNOUNCER)$(call get_color,$(1))]$(call get_color,Off)

.PHONY: all help build up down re clean fclean logs ps login dev check-env _build _up _certs _migrate secrets-setup secrets-decrypt secrets-encrypt secrets-refresh

all: check-env _build up ## Build all images and start the full stack (default)

help: ## Show this help
	@echo "$(call ann,Cyan) $(call get_color,Purple)$(NAME)$(call get_color,Off) - available targets:"
	@grep -hE '^[a-zA-Z][a-zA-Z_-]*:.*## ' $(MAKEFILE_LIST) | sort | \
		awk 'BEGIN{FS=":.*## "}{printf "  $(call get_color,Green)%-16s$(call get_color,Off) %s\n", $$1, $$2}'

build: check-env _build ## Build all service images
	@echo "$(call ann,Green) All service images built. It works on my machine™"

up: check-env _up ## Start the stack (assumes images are already built)
	@echo "$(call ann,Green) $(call get_color,Purple)$(NAME)$(call get_color,Off) is up at https://localhost:1443. All systems go (famous last words)"

down: ## Stop and remove all containers
	@echo "$(call ann,Yellow) Stopping and removing all containers. Going dark, at least you won't need to download more RAM today :)"
	@$(COMPOSE) down

re: ## Full clean, then rebuild and start from scratch
	@echo "$(call ann,Yellow) Full clean, then rebuild and start from scratch. Scorched earth. Classic."
	@$(MAKE) fclean all

clean: down ## Stop the stack and prune dangling images
	@echo "$(call ann,Yellow) Pruning dangling images. They knew too much anyway"
	@$(DOCKER) image prune -f
	@echo "$(call ann,Green) Poof. Like it never happened :O"

fclean: ## Remove containers + our built images (prompts before deleting DB volumes)
	@echo "$(call ann,Red) Tearing down $(NAME): containers + our built images (base images and cache kept)"
	@printf "$(call ann,Yellow) Also delete database volumes? This permanently destroys all data [y/N] "; \
	read ans; \
	case "$$ans" in \
		[yY]|[yY][eE][sS]) vols="--volumes"; echo "$(call ann,Red) Nuking volumes too, hope you meant it";; \
		*) vols=""; echo "$(call ann,Green) Keeping volumes (your data lives another day)";; \
	esac; \
	$(COMPOSE) --profile migrate down $$vols --remove-orphans
	@$(DOCKER) image rm -f $(BUILT_IMAGES) 2>/dev/null || true
	@echo "$(call ann,Red) Done. Base images and build cache untouched :)"

ifeq (logs,$(firstword $(MAKECMDGOALS)))
  LOG_ARGS := $(wordlist 2,$(words $(MAKECMDGOALS)),$(MAKECMDGOALS))
  $(eval $(LOG_ARGS):;@:)
endif

logs: ## Tail logs of all containers, or specific ones: make logs auth chat
	@echo "$(call ann,Cyan) Tuning into the logs ($(if $(LOG_ARGS),$(LOG_ARGS),everything)). Ctrl+C to look away"
	@$(COMPOSE) logs -f $(LOG_ARGS)

ps: ## List the stack's containers
	@$(COMPOSE) ps

login: ## Log in to docker.io (raise pull rate limits)
	@echo "$(call ann,Cyan) Exchanging dignity for pull access"
	@$(DOCKER) login docker.io
	@echo "$(call ann,Green) You're in. The rate limiter is watching"

secrets-setup: ## Set up SOPS secret management
	@$(MAKE) --no-print-directory -C $(SECRETMANAGER_DIR) setup

secrets-decrypt: ## Decrypt the encrypted .env files
	@$(MAKE) --no-print-directory -C $(SECRETMANAGER_DIR) decrypt

secrets-encrypt: ## Encrypt the .env files
	@$(MAKE) --no-print-directory -C $(SECRETMANAGER_DIR) encrypt

secrets-refresh: ## Re-encrypt secrets with the current recipients
	@$(MAKE) --no-print-directory -C $(SECRETMANAGER_DIR) refresh

dev: check-env ## Start the Tilt hot-reload dev environment
	@echo "$(call ann,Cyan) Starting the Tilt dev environment (hot-reload). Go do wonders, we believe in you (we don't have a choice)"
	@ENGINE="$(ENGINE)" tilt up

check-env:
	@for f in $(ENV_FILES); do \
		if [ ! -f "$$f" ]; then \
			echo "$(call ann,Red) Caught you slipping. $$f doesn't exist (copy from $$f.example and fill in values) :)"; \
			exit 1; \
		fi; \
		if ! grep -qE '^[A-Za-z_][A-Za-z_0-9]*=.+' "$$f"; then \
			echo "$(call ann,Yellow) Creating $$f with no values is just creating a file. That's not how it works. Check $$f.example :)"; \
			exit 1; \
		fi; \
	done
	@drift=''; \
	for pair in $(ENV_CRYPT_PAIRS); do \
		f=$${pair%%:*}; crypt="$(SECRETS_DIR)/$${pair##*:}.env.crypt"; \
		{ [ -f "$$f" ] && [ -f "$$crypt" ]; } || continue; \
		ekf=$$(mktemp); ckf=$$(mktemp); \
		grep -E '^[A-Za-z_][A-Za-z_0-9]*=' "$$f" | sed 's/=.*//' | sort -u > "$$ekf"; \
		awk -F'"' '/^\t"/{print $$2}' "$$crypt" | grep -vx sops | sort -u > "$$ckf"; \
		missing=$$(comm -13 "$$ekf" "$$ckf"); \
		extra=$$(comm -23 "$$ekf" "$$ckf"); \
		rm -f "$$ekf" "$$ckf"; \
		if [ -n "$$missing" ] || [ -n "$$extra" ]; then \
			drift=1; \
			echo "$(call ann,Yellow) $$f wandered off from its secret ($$crypt). The keys don't line up:"; \
			[ -n "$$missing" ] && echo "    only in the secret: $$(echo $$missing | tr '\n' ' ')"; \
			[ -n "$$extra" ] && echo "    only in the env: $$(echo $$extra | tr '\n' ' ')"; \
		fi; \
	done; \
	if [ -n "$$drift" ]; then \
		printf "$(call ann,Yellow) your envs and secrets are living separate lives (secrets-encrypt / secrets-decrypt to reconcile). Send it anyway? [y/N] "; \
		read ans; \
		case "$$ans" in [yY]|[yY][eE][sS]) : ;; *) echo "$(call ann,Red) Aborting. Go patch things up with your secrets first :)"; exit 1;; esac; \
	fi

_build:
	@echo "$(call ann,Cyan) Building all service images. Turning caffeine and regret into Docker layers."
	@$(COMPOSE) --profile migrate build

_certs:
	@echo "$(call ann,Cyan) Minting TLS certs host-side, before the containers wake up. Self-signed and proud (your browser won't be)"
	@sh infra/cert-gen/cert-gen.sh

_migrate:
	@echo "$(call ann,Cyan) Waking the databases and waiting for them to be healthy (no operating on a patient without a pulse)"
	@$(COMPOSE) up -d $(DB_SERVICES)
	@for db in $(DB_SERVICES); do \
		echo "$(call ann,Blue) checking $$db for a pulse ..."; \
		ok=; \
		for i in $$(seq 1 120); do \
			hs=$$($(DOCKER) inspect -f '{{.State.Health.Status}}' "$$db" 2>/dev/null || echo missing); \
			[ "$$hs" = "healthy" ] && { ok=1; break; }; \
			sleep 2; \
		done; \
		[ -n "$$ok" ] || { echo "$(call ann,Red) $$db flatlined (never became healthy). Calling it, aborting"; exit 1; }; \
	done
	@echo "$(call ann,Cyan) Applying database migrations one-shot, before the apps wake up asking for tables that don't exist"
	@for m in $(MIGRATE_SERVICES); do \
		echo "$(call ann,Blue) running migration: $$m (hold still)"; \
		$(COMPOSE) --profile migrate run --rm $$m || { echo "$(call ann,Red) migration $$m belly-flopped. Aborting"; exit 1; }; \
	done

_up: _certs _migrate
	@echo "$(call ann,Cyan) Starting the application containers. Rise and shine, you beautiful disasters!"
	@$(COMPOSE) up -d
