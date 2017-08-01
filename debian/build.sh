#!/bin/bash

# For description of this script, refer to the README

# Abort if any command returns non-zero code
set -e

# Prints a message (required first parameter) in blue color  
function echo_info {
	COLOR="\033[0;36m"
	NOCOLOR="\033[0m"

	printf "${COLOR}${1}${NOCOLOR}\n"
}

# Prints a message (required first parameter) in red color
function echo_success {
	COLOR="\033[0;32m"
	NOCOLOR="\033[0m"

	printf "${COLOR}${1}${NOCOLOR}\n"
}

# Prints the error message into the error stream and exists with non-zero code
# Requires first argumnent - error message
# Second argument is optional - exit code
function die {
    [[ $1 ]] || {
        printf >&2 -- 'Usage:\n\tdie <message> [return code]\n'
        [[ $- == *i* ]] && return 1 || exit 1
    }

	COLOR="\033[0;31m"
	NOCOLOR="\033[0m"

    printf >&2 -- "${COLOR}${1}${NOCOLOR}\n"
	usage
    exit ${2:-1}
}

function usage { 
	NOCOLOR="\033[0m"
	CYAN="\033[0;36m"
	GREEN="\033[0;32m"
	RED="\033[0;31m"

	printf "Usage: ${RED}$0${NOCOLOR}\n"
	
	printf "Example: ${RED}$0${NOCOLOR}\n"
	
	exit 1
}

function build_package {
	
	echo_info "(Re)creating a temporary directory for building..."
	rm -rf build/ \
	&& mkdir -p build/status-ctl/ \
	|| die "Could not recreate tmp directory"

	echo_info "Copying .jar file, wrapper and scripts..."
	cp status-ctl.sh build/status-ctl/ \
	&& cp -r Makefile debian/ build/status-ctl \
	|| die "Could not copy files"

	echo_info "Building debian package..."
	cd build/status-ctl \
	&& debuild -us -uc \
	|| die "Could not build a package"
	
	echo_info "Cleaning tmp files..."
	cd ../.. \
	&& rm -rf build/status-ctl \
	|| die "Could not clean tmp files"
}

# Process command line arguments
while getopts "j:b:m:" o; do
	case "${o}" in
		h)
			usage
			;;
	esac
done
shift $((OPTIND-1))

build_package

echo_success "Debian package has been built!"
exit 0
