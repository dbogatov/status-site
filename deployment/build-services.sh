#!/usr/bin/env bash 

set -e

shopt -s globstar

SERVICES=("ping" "nginx" "docs" "daemons" "web" "database")

if [ $# -eq 0 ]
then
	TAG="master"
else
	TAG=$1	
fi

# "49-move-to-kubernetes-deployment"

rm -rf services/
mkdir -p services

cp sources/namespace.yaml services/

for service in ${SERVICES[@]}
do
	echo "Generating $service configs..."

	mkdir -p services/$service

	cp sources/service/{service,deployment}.yaml services/$service

	if [ "$service" != "database" ]
	then
		IMAGE="dbogatov/status-site:$service-$TAG"
		PORT="80"
	else
		IMAGE="postgres:9.6.3-alpine"
		PORT="5432"
	fi

	sed -i '' -e "s#__NAME__#$service#g" services/$service/{service,deployment}.yaml
	sed -i '' -e "s#__IMAGE__#$IMAGE#g" services/$service/{service,deployment}.yaml
	sed -i '' -e "s#__PORT__#$PORT#g" services/$service/{service,deployment}.yaml

done

echo "Done!"
