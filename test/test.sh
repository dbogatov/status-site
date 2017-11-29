#!/bin/bash

PREV_ENV=$ASPNETCORE_ENVIRONMENT

echo "Changing environment to testing..."
export ASPNETCORE_ENVIRONMENT="Testing"

echo "Copying the application settings..."
cp ../src/appsettings*.* .

if [ -n "$1" ]
then
	echo "Running test $1 ..."
	dotnet test --filter "FullyQualifiedName~$1"
else
	echo "Running dotnet tests..."
	dotnet test --no-build --no-restore --verbosity n
fi

echo "Removing application settings..."
rm appsettings*.*

echo "Reverting environment..."
export ASPNETCORE_ENVIRONMENT=$PREV_ENV

echo "Testing completed!"
