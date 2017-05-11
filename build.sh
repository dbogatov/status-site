#!/bin/bash
set -e

# Ensure that the CWD is set to script's location
cd "${0%/*}"
CWD=$(pwd)

## TASKS

install-doc-generators () {

	cd $CWD

	echo "Installing node modules... Requires Yarn"
	yarn --ignore-engines > /dev/null

}

gen-server-docs () { 

	cd $CWD

	echo "Generating server side code documentation... Requires Doxygen"
	
	rm -rf src/web/wwwroot/docs/server
	mkdir -p src/web/wwwroot/docs/server
	doxygen Doxyfile > /dev/null
	mv src/web/wwwroot/docs/server/html/* src/web/wwwroot/docs/server

}

gen-client-docs () { 

	cd $CWD

	echo "Generating client side code documentation... Requires TypeDoc (Installed by Yarn)"
	mkdir -p src/web/wwwroot/docs/client
	$(yarn bin)/typedoc --logger none client/ts/ > /dev/null
	printf "\n"
	
}

gen-api-docs () {

	cd $CWD

	echo "Generating API documentation... Requires Spectacle (Installed by Yarn)"
	mkdir -p src/web/wwwroot/docs/api
	$(yarn bin)/spectacle api.yml -t src/web/wwwroot/docs/api > /dev/null

}

install-client-libs () {

	cd $CWD/client

	echo "Installing front-end libraries... Requires Yarn"
	yarn --ignore-engines > /dev/null

}

install-typings () {

	cd $CWD

	echo "Installing TypeScript typings... Requires Typings (Installed by Yarn)"
	mkdir -p client/typings
	$(yarn bin)/typings install > /dev/null

}

generate-client-bundle () {

	cd $CWD

	echo "Bundling front-end libraries... Requires Webpack (Installed by Yarn)"
	rm -rf src/web/wwwroot/{js,css}/*

	$(yarn bin)/webpack --context client/ --env prod --config client/webpack.config.js --output-path src/web/wwwroot/js > /dev/null

	mkdir -p src/web/wwwroot/css/
	mv src/web/wwwroot/js/app.min.css src/web/wwwroot/css/
	rm src/web/wwwroot/js/less.*

}

restore-dotnet () {

	cd $CWD/src

	echo "Restoring .NET dependencies... Requires .NET SDK"
	dotnet restore daemons/daemons.csproj > /dev/null
	dotnet restore web/web.csproj > /dev/null
}

build-dotnet () {

	cd $CWD/src

	echo "Building and publishing .NET app... Requires .NET SDK"
	dotnet publish -c release daemons/daemons.csproj > /dev/null
	dotnet publish -c release web/web.csproj > /dev/null
}

build-dev-client () {

	cd $CWD

	echo "Cleaning dist/"
	rm -rf client/dist/

	echo "Copying source files"
	mkdir -p client/dist/{ts,less}
	cp -R client/ts/* client/dist/ts/
	cp -R client/less/* client/dist/less/

	echo "Bundling front-end libraries... Requires Webpack (Installed globally)"
	webpack --env dev --display-error-details --output-path client/dist/ts --config client/webpack.config.js --context client/

	echo "Copying generated code"
	mkdir -p src/web/wwwroot/js/ts
	cp -R client/dist/ts/* src/web/wwwroot/js/ts/
	cp client/dist/ts/app.css src/web/wwwroot/css/app.css

	echo "Removing temporary directory"
	rm -rf client/dist/

	echo "Done!"
}

build-docker-images () {

	cd $CWD/src

	if [ -z "$DOTNET_TAG" ]; then
		DOTNET_TAG="local"
	fi

	echo "Building web-$DOTNET_TAG"
	docker build -f web/Dockerfile -t registry.gitlab.com/rweda/status-site:web-$DOTNET_TAG web/

	echo "Building daemons-$DOTNET_TAG"
	docker build -f daemons/Dockerfile -t registry.gitlab.com/rweda/status-site:daemons-$DOTNET_TAG daemons/

	echo "Done!"
}

push-docker-images () {

	if [ -z "$DOTNET_TAG" ]; then
		DOTNET_TAG="local"
	fi

	echo "Pushing web-$DOTNET_TAG"
	docker push registry.gitlab.com/rweda/status-site:web-$DOTNET_TAG

	echo "Pushing daemons-$DOTNET_TAG"
	docker push registry.gitlab.com/rweda/status-site:daemons-$DOTNET_TAG

	echo "Done!"
}

build-for-compose () {
	build-dotnet
	build-docker-images
}

## APP BUILDERS

# Unstable
build-app-parallel () {

	install-doc-generators &
	install-client-libs &
	restore-dotnet &
	gen-server-docs &

	wait %install-doc-generators
	
	gen-client-docs &
	gen-api-docs &

	wait %install-client-libs
	
	install-typings &

	wait %install-typings
	
	generate-client-bundle &

	wait
	
	build-dotnet

	echo "Build completed!"

}

build-app-sequential () {

	install-doc-generators
	install-client-libs

	gen-server-docs
	gen-client-docs
	gen-api-docs

	install-typings
	generate-client-bundle
	restore-dotnet

	build-dotnet

	echo "Build completed!"

}

usage () { 
	echo "Usage: $0 [-d -f <string>]" 1>&2 
}

while getopts "f:d" o; do
	case "${o}" in
		f)
			eval $OPTARG
			exit 0
			;;
		d)
			build-dev-client
			exit 0
			;;
		h)
			usage
			exit 0
			;;
		*)
			usage
			exit 1
			;;
	esac
done
shift $((OPTIND-1))

build-app-sequential
