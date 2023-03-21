SHELL := /bin/bash

build_tests:
	docker-compose --project-name tests build

run_tests: build_tests
	docker-compose --project-name tests up --no-build --exit-code-from tests tests

rmq:
	-docker network create $(if ${INTERNAL},--internal) -d bridge houseofcat
	docker-compose -f docker-compose-rmq.yml --project-name hoc-rmq up -d

tests: INTERNAL ?= true
tests: rmq run_tests

stop_rmq:
	docker-compose -f docker-compose-rmq.yml --project-name hoc-rmq down
	docker-compose -f docker-compose-rmq.yml --project-name hoc-rmq rm -f
	-docker network rm houseofcat

stop_tests:
	-docker-compose --project-name tests down --remove-orphans
	-docker-compose --project-name tests rm -f

stop: stop_tests stop_rmq
