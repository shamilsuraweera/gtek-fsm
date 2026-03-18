SHELL := /bin/bash

.PHONY: dev-db-init dev-db-reset dev-db-seed dev-up dev-down dev-logs dev-reset

dev-db-init:
	./database/scripts/dev-db-init.sh

dev-db-reset:
	./database/scripts/dev-db-reset.sh

dev-db-seed:
	./database/scripts/dev-db-seed.sh

dev-up:
	./deploy/scripts/dev-up.sh

dev-down:
	./deploy/scripts/dev-down.sh

dev-logs:
	./deploy/scripts/dev-logs.sh

dev-reset:
	./deploy/scripts/dev-reset.sh
