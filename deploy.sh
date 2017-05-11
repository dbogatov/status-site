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

	printf "Usage: ${RED}$0${NOCOLOR} -t ${GREEN}<string | gitlab-access-token>${NOCOLOR} [-b ${CYAN}<string | branch>${NOCOLOR}] [-e]\n"
	
	printf "Example: ${RED}$0${NOCOLOR} -t ${GREEN}slfkSKDF-asdasas-879${NOCOLOR} -b ${CYAN}19-separate-web-component-from-demons${NOCOLOR} -e\n"

	printf "where:\n"

	printf "\t-t ${GREEN}gitlab-access-token${NOCOLOR} is gitlab access token needed to access the project repo.\n"
	printf "\t-b ${CYAN}branch${NOCOLOR} is a branch name from whcih to download artifacts.\n"
	printf "\t-e is a flag that causes script to use example settings from artifacts archive.\n"
	
	exit 1
}

BRANCH="master"
EXAMPLE=false

# Process command line arguments
while getopts "t:b:e" o; do
	case "${o}" in
		t)
			TOKEN=$OPTARG
			;;
		b)
			BRANCH=$OPTARG
			;;
		e)
			EXAMPLE=true
			;;
		*)
			usage
			;;
	esac
done
shift $((OPTIND-1))

[[ -n "$TOKEN" ]] || die "-t is required"

PROJECT_ID="2252178" # lookup in repo settings
JOB="release" # change if necessary

echo_info "Downloading artifacts into temporary directory"
curl \
	--header "PRIVATE-TOKEN: $TOKEN" \
	"https://gitlab.com/api/v4/projects/$PROJECT_ID/jobs/artifacts/$BRANCH/download?job=$JOB" \
> artifacts.zip \
|| die "Could not download artifacts"

echo_info "Extracting files"
unzip -o artifacts.zip \
|| die "Could not extract files"

[ -f ./docker-compose.yml ] || die "docker-compose.yml file is not found in artifacts archive"

[[ "$EXAMPLE" = false ]] || {
	echo_info "-e was specified, so overriding .env and appsetting.json"
	find . -name '*.example' -type f | while read NAME ; do mv "${NAME}" "${NAME%.example}" ; done
}

[ -f ./appsettings.json ] || die "appsettings.json file is required (use -e option to use example files)"
[ -f ./.env ] || die ".env file is required (use -e option to use example files)"

if ! grep -q "DOTNET_TAG=" ".env"; then
	printf "\n\nDOTNET_TAG=$BRANCH" >> .env
fi

echo_info "Running composition"
docker-compose -p statussite pull && \
docker-compose -p statussite stop && \
docker-compose -p statussite up -d || \
die "Could not running a composition"

echo_success "All done!"
