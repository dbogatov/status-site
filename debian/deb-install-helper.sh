#!/bin/bash

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

	printf "Usage: ${RED}$0${NOCOLOR} -t ${GREEN}<string | gitlab-access-token>${NOCOLOR} [-b ${CYAN}<string | branch>${NOCOLOR}]\n"

	printf "Example: ${RED}$0${NOCOLOR} -t ${GREEN}slfkSKDF-asdasas-879${NOCOLOR} -b ${CYAN}263-complete-bundling-process${NOCOLOR}\n"

	printf "where:\n"

	printf "\t-t ${GREEN}gitlab-access-token${NOCOLOR} is gitlab access token needed to access the mono repo.\n"
	printf "\t-b ${CYAN}branch${NOCOLOR} is a branch name from whcih to download artifacts.\n"

	exit 1
}

BRANCH="master"

# Process command line arguments
while getopts "t:b:" o; do
	case "${o}" in
		t)
			TOKEN=$OPTARG
			;;
		b)
			BRANCH=$OPTARG
			;;
		*)
			usage
			;;
	esac
done
shift $((OPTIND-1))

[[ -n "$TOKEN" ]] || die "-t is required"

PROJECT_ID="47" # lookup in repo settings
JOB="release-debian" # change if necessary

echo_info "Downloading artifacts into temporary directory"
cd `mktemp -d`
curl \
	--header "PRIVATE-TOKEN: $TOKEN" \
	"https://git.dbogatov.org/api/v4/projects/$PROJECT_ID/jobs/artifacts/$BRANCH/download?job=$JOB" \
> artifacts.zip \
|| die "Could not download artifacts"

echo_info "Extracting files"
unzip artifacts.zip \
|| die "Could not extract files"

echo_info "Removing .deb"
reprepro -b /var/repositories/ remove trusty status-ctl \
|| die "Could not remove .deb from reprepro"

echo_info "Installing .deb"
reprepro -b /var/repositories includedeb trusty debian/build/*.deb \
|| die "Could not add .deb to reprepro"

echo_success "Added package to the repository"
