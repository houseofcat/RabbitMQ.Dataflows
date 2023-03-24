SHELL := /bin/bash

build_tests:
	docker-compose --project-name hoc-tests build tests

run_tests: build_tests
	docker-compose --project-name hoc-tests up --no-build --exit-code-from tests tests

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
	-docker-compose --project-name hoc-tests down --remove-orphans
	-docker-compose --project-name hoc-tests rm -f

stop: stop_tests stop_rmq
