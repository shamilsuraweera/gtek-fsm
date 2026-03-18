SHELL := /bin/bash

.PHONY: dev-db-init dev-db-reset dev-db-seed

dev-db-init:
	./database/scripts/dev-db-init.sh

dev-db-reset:
	./database/scripts/dev-db-reset.sh

dev-db-seed:
	./database/scripts/dev-db-seed.sh
