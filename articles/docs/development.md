# Development

## Requirements

To build/debug/test all parts of the project you will need the following:

### Main app, client side, and testing

* [ASP.Core SDK](https://www.microsoft.com/net/core)
* [Yarn](https://yarnpkg.com/en/)

### Documentation

* [Yarn](https://yarnpkg.com/en/)
* [Doxygen](http://www.stack.nl/~dimitri/doxygen/)
* [MkDocs](http://www.mkdocs.org) with [Material theme](http://squidfunk.github.io/mkdocs-material/)

### Extra

* [Docker](https://www.docker.com)
* [Docker compose](https://docs.docker.com/compose/)

!!! tip
    You may want to run most of the tools in Docker.
	This way you would avoid installing software on your machine.
	Docker is powerful enough to let you run several containers for different jobs which operate on a single mounted volume with the app.
	You may want to examine `.gitlab-ci.yml` to find out which images you may need for certain tasks.

### Editor

The recommended editor for this project is [VS Code](https://code.visualstudio.com).
It has the first class support for C# and .NET, as well as TypeScript.
This project does not depend on any particular editor.
You may use any one (Visual Studio, Atom, Notepad++, etc.)

!!! tip
	There is `.vscode/` directory in the root of the project.
	This folder contains project specific settings for VS Code.
	You may want to set it up for yourself.

## How to build the app

### Server

Server-side apps[^1] are built with .NET Core.
Before building the project, make sure all dependencies (nuget packages) are installed - execute `#!bash dotnet restore`.
Then use the command `#!bash dotnet build` to build the app (or `#!bash dotnet publish -c release` to publish[^2] the app). 
Both commands should be executed in a directory where `*.csproj` is.

[^1]: Daemons and web 
[^2]: Publish command will build the app and pack into the directory with all its .NET dependencies.
You should use it when building for production.

### Client

To compile client, we need to compile LESS to CSS, TypeScript to JS and then put output files in the correct locations.
For this task we use `webpack` bundler and its modules.

First, install required node modules

* Run `yarn` in the root directory of the project
* Run `yarn` in the `client/` directory

Second, generate TypeScript typings `#!bash $(yarn bin)/typings install` (from the root directory).

Third, bundle the app with the Webpack.
Run `#!bash $(yarn bin)/webpack --context client/ --env prod --config client/webpack.config.js --output-path src/web/wwwroot/js`.

Finally, move files to correct locations.

	#!bash
	mkdir -p src/web/wwwroot/css/
	mv src/web/wwwroot/js/app.min.css src/web/wwwroot/css/
	rm src/web/wwwroot/js/less.*

!!! tip
    For your convenience, there is a `build.sh` script, which will take care of all these steps.
	`#!bash ./build.sh` will build and publish the app for production.
	`#!bash ./build.sh -d` will build client side only using development configuration[^3].
	`#!bash ./build.sh -f name-of-function` will run only one function from those defined in `build.sh`.

[^3]: In dev configuration, server will attempt to load resources from different location than in prod configuration.
For dev configuration, resource will not be minified/uglified and will have sourcemaps, so you can debug the client side.
It is essential to set env variable `ASPNETCORE_ENVIRONMENT` to `Development` when working with dev configuration.

## How to generate documentation

We generate 4 different types of documentation.
HTML output is generated in `documentation/out/` directory.

### C# Docs

Doxygen is used to generate HTML docs from C# comments.
Run `doxygen Doxyfile` to generate docs.

### TypeScript Docs

TypeDoc is used to generate HTML docs from TypeScript comments.
Run `$(yarn bin)/typedoc --logger none client/ts/` to generate docs.

### API Docs

Spectacle is used to generate HTML docs for API endpoints.
Run `$(yarn bin)/spectacle api.yml -t documentation/out/swagger` to generate docs.

### Articles

MkDocs is used to generate HTML articles (like the one you are reading now).
Run `mkdocs build` from `articles/` directory to generate articles.

!!! tip
    For your convenience, `./build.sh` has function `build-docs` that will do the work for you.
	Invoke it like this `#!bash ./build.sh -f name-of-function`.

## How to test the app

Testing is fairly simple.
You need to run `#!bash dotnet test` in the `test/` directory.
Do not forget to set env variable `ASPNETCORE_ENVIRONMENT` to `Testing` and restore dependencies.

!!! tip
    For your convenience, there is a `test/test-dotnet.sh` script that will do these actions for you.

Testing provider[^4] will print the results of the testing.
Specifically, you will see how many tests are failing, passing or being skipped.

[^4]: `xUnit` is this case

### BLC / Tidy

We also use little tools like BLC and Tidy to run quality checks on the app.
In particular, Tidy makes sure that generated HTML is W3C complainant, and BLC is checking for broken links.

To test the app with these tools, install them first, run the app in one process and pipe the HTML output from the app to the tools, like this `#!bash curl -Ls http://localhost:5555/ | tidy -e`.

## How to package the app

Packaging the app is as simple as running `#!bash docker build ...` on each project after the app has been built.

!!! tip
    For your convenience, `./build.sh` has functions `build-docker-images` and `push-docker-images` that will do the work for you.
	Invoke them like this `#!bash ./build.sh -f name-of-function`.


!!! tip
    You can always look up `.gitlab-ci.yml` to see exactly how app is being built, tested and packaged.

!!! summary
    Here are the helpful links:
	
	* [ASP.Core: Getting Started](https://docs.microsoft.com/en-us/aspnet/core/getting-started)
	* [Testing and Debugging](https://docs.microsoft.com/en-us/aspnet/core/testing/)
	* [Hosting and Deployment](https://docs.microsoft.com/en-us/aspnet/core/publishing/)
