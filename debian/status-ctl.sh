#!/bin/bash

# EXIT CODES
SUCCESS=0
GENERIC_ERROR=1
DEPENDENCY_MISSING=2
CONFIG_INVALID=3
BAD_PARAMETERS=4

# CONSTANTS
CONFIG_DIR=/etc/status-site
PROJECT="status-site"
VERSION="1.2.0"
BRANCH="master"

function usage {
	printf "Usage: \t\t$0 [options] <command>\n\n"

	printf "Example: \t$0 start\n"
	printf "Example: \t$0 -b some-other-branch stop\n\n"

	printf "options:\n"

	printf "\t-b branch \t (development only) branch to use when deploying status site.\n\n"

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
		
		exit $DEPENDENCY_MISSING
	}

	command -v docker-compose >/dev/null 2>&1 || { 
		echo >&2 "docker-compose is required to run status-site."
		echo >&2 "See https://docs.docker.com/compose/install/"
		echo >&2 "Aborting."
		
		exit $DEPENDENCY_MISSING
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
		
		exit $CONFIG_INVALID
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

printf "\n\nDOTNET_TAG=$BRANCH" >> .env

while :; do
	case "$1" in
		-b)
			[[ -n "$2" ]] || {
				echo "$1 requires a non-empty option argument"

				exit $BAD_PARAMETERS
			}

			BRANCH="$2"

			printf "\n\nDOTNET_TAG=$BRANCH" >> .env

			echo "Branch is set to $BRANCH"

			shift

			;;

		start)
			start

			exit $SUCCESS

			;;
			
		stop)
			stop

			exit $SUCCESS

			;;
			
		reconfigure)

			reconfigure

			exit $SUCCESS

			;;

		check-config)

			check-config

			exit $SUCCESS

			;;

		upgrade)
			
			upgrade

			exit $SUCCESS

			;;

		upgrade-clean)

			upgrade-clean

			exit $SUCCESS

			;;
		
		logs)

			logs

			exit $SUCCESS

			;;

		info)

			info

			exit $SUCCESS

			;;

		help)

			help

			exit $SUCCESS

			;;

		version)

			version

			exit $SUCCESS

			;;

		*)

			help

			exit $GENERIC_ERROR
	
	esac

	shift
done

exit $GENERIC_ERROR
