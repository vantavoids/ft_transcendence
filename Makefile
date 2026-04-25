NAME         := ft_transcendence
ANNOUNCER    := Announcer

COMPOSE      ?= podman-compose
DOCKER       ?= podman

ENV_FILES := .env \
             services/auth/.env \
             services/gateway/.env \
             services/guild/.env \
             services/notification/.env \
             services/user/.env

get_color = $(if $(filter Purple,$(1)),$(shell tput setaf 5),$(if $(filter Red,$(1)),$(shell tput setaf 1),$(if $(filter Cyan,$(1)),$(shell tput setaf 6),$(if $(filter Blue,$(1)),$(shell tput setaf 4),$(if $(filter Yellow,$(1)),$(shell tput setaf 3),$(if $(filter Green,$(1)),$(shell tput setaf 2),$(shell tput sgr0)))))))
ann = $(call get_color,$(1))[$(call get_color,Off)$(ANNOUNCER)$(call get_color,$(1))]$(call get_color,Off)

.PHONY: all build up down re clean fclean logs ps login dev check-env _build _up

all: check-env _build _up

build: check-env _build
	@echo "$(call ann,Green) It works on my machine™"

up: check-env _up
	@echo "$(call ann,Green) $(call get_color,Purple)$(NAME)$(call get_color,Off): All systems go (famous last words)"

down:
	@echo "$(call ann,Yellow) Going dark. At least you won't need to download more RAM today :)"
	@$(COMPOSE) down

re:
	@echo "$(call ann,Yellow) Scorched earth and rebuild. Classic."
	@$(MAKE) fclean all

clean: down
	@echo "$(call ann,Yellow) Those images knew too much anyway"
	@$(DOCKER) image prune -f
	@echo "$(call ann,Green) Poof. Like it never happened :O"

fclean: down
	@echo "$(call ann,Red) docker system prune but make it personal"
	@$(DOCKER) container rm -af 2>/dev/null || true
	@$(DOCKER) image rm -f $$($(DOCKER) image ls -q) 2>/dev/null || true
	@$(DOCKER) system prune -a --volumes -f
	@echo "$(call ann,Red) It's all gone. You won't have to download extra storage (no need to thank me) :)"

logs:
	@echo "$(call ann,Cyan) Looking at the containers' yapping... (Ctrl+C to look away)"
	@$(COMPOSE) logs -f

ps:
	@$(COMPOSE) ps

login:
	@echo "$(call ann,Cyan) Exchanging dignity for pull access"
	@$(DOCKER) login docker.io
	@echo "$(call ann,Green) You're in. The rate limiter is watching"

dev: check-env
	@echo "$(call ann,Cyan) Go do wonders. We believe in you (we don't have a choice)"
	@tilt up

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

_build:
	@echo "$(call ann,Cyan) Turning caffeine and regret into Docker layers."
	@$(COMPOSE) build

_up:
	@echo "$(call ann,Cyan) Rise and shine, you beautiful disasters!"
	@$(COMPOSE) up -d
