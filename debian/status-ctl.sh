#!/bin/bash

# CONSTANTS
CONFIG_DIR=/etc/status-site
PROJECT="status-site"
VERSION="0.1.0"

function usage {
	printf "Usage: $0 command\n"

	printf "Example: $0 stop\n"

	printf "commands:\n"

	printf "\tstart \t\truns the composition; pulls latest version if none is installed.\n"
	printf "\tstop \t\tstops the composition if it is running.\n"
	printf "\treconfigure \trestarts the app to apply config chnages.\n"
	printf "\tcheck-config \tverifies that the config file is valid.\n"
	printf "\tupgrade \tstops the composition, pulls latest version and starts the compostion.\n"
	printf "\tupgrade-clean \tstops the composition, removes all containers (dataabse in particular), pulls latest version and starts the compostion.\n"
	printf "\tlogs \t\tshows app's log messages.\n"
	printf "\tinfo \t\tshows status-site's information (eq. path to config).\n"
	printf "\tversion \tshows this tool's version.\n"
}

function check-dependencies {
	command -v docker >/dev/null 2>&1 || { 
		echo >&2 "docker is required to run status-site."
		echo >&2 "See https://docs.docker.com/engine/installation/"
		echo >&2 "Aborting."
		
		exit 1
	}

	command -v docker-compose >/dev/null 2>&1 || { 
		echo >&2 "docker-compose is required to run status-site."
		echo >&2 "See https://docs.docker.com/compose/install/"
		echo >&2 "Aborting."
		
		exit 1
	}
}

function start {
	echo "STARTING STATUS-SITE"

	docker-compose -p $PROJECT up -d --remove-orphans
}

function stop {
	echo "STOPPING STATUS-SITE"

	docker-compose -p $PROJECT stop
}

function reconfigure {
	echo "RECONFIGURING STATUS-SITE"

	check-config
	stop
	start
}

function check-config {
	echo "Configuration check is not implemented yet."
	echo "It always passes"

	true || { 
		echo >&2 "CONFIGURATION CHECK FAILED."
		echo >&2 "See log messages above."
		echo >&2 "Aborting."
		
		exit 1
	}
}

function upgrade {
	echo "UPGRADING STATUS-SITE"

	stop
	docker-compose -p $PROJECT pull
	start
}

function upgrade-clean {
	echo "UPGRADING STATUS-SITE WITH CLEAN DATABASE"

	stop
	docker-compose -p $PROJECT rm -f
	docker-compose -p $PROJECT pull
	start
}

function logs {
	echo "SHOWING LOG MESSAGES"

	docker-compose -p $PROJECT logs
}

function info {
	printf "Config path:\t$CONFIG_DIR/appsettings.yml\n"
	printf "Env path:\t$CONFIG_DIR/.env\n"
	printf "Compose path:\t$CONFIG_DIR/docker-compose.yml\n"
}

function help {
	usage
}

function version {
	echo "status-ctl $VERSION"
}

check-dependencies

cd $CONFIG_DIR

case "$1" in
	start)
		start
		;;
		
	stop)
		stop
		;;
		
	reconfigure)
		reconfigure
		;;

	check-config)
		check-config
		;;

	upgrade)
		upgrade
		;;

	upgrade-clean)
		upgrade-clean
		;;
	
	logs)
		logs
		;;

	info)
		info
		;;

	help)
		help
		;;

	version)
		version
		;;

	*)
		help
		exit 1
 
esac

exit 0
