#!/bin/bash

# CONSTANTS
CONFIG_DIR=/etc/status-site

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
	printf "\thelp \t\tshows this help message.\n"
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
	echo "started"
}

function stop {
	echo "stoped"
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
	echo "upgrade"
}

function upgrade-clean {
	echo "upgrade-cleaned"
}

function help {
	echo "helped"

	usage
}

function version {
	echo "version"

	echo "0.0.1"
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
