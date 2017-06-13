# Deployment

## TL;DR

Make sure you have [Docker](https://www.docker.com) and [Docker compose](https://docs.docker.com/compose/) installed.
Run the following command in a directory where you want your configuration files to be.

	#!bash
	curl -Ls https://status.dbogatov.org/docs/deploy.sh | bash -s -- -e

!!! warning
    This script does not require `sudo` privileges.
	Nevertheless, it is recommended that you examine the script before running it.

!!! tip
    You may use `-b feature-branch` to deploy a specific branch.

		#!bash
		curl -Ls https://status.dbogatov.org/docs/deploy.sh | bash -s -- -b feature-branch


!!! note
	The `e` parameter in `bash -s -- -e` specifies that you want to use example configuration.
	Example configuration is conservative - most of the features are disabled, but is still enough for a basic operation of the app.
	If you want to change configuration, modify `appsettings.yml` and re-run the command without `-e` argument, otherwise it will override your changes to default example configuration.

## appsettings.yml and .env

There are 4 mandatory files that need to be in the directory alongside with `docker-compose.yml`, so that the app can start.
`appsettings.yml` is the main configuration file, see more in [Configuration](/configuration/).
`.env` file is simply a collection of environmental variables for composition.
Its content is self-explanatory, except for `DOTNET_TAG` which needs to point to the branch you want to use (*master* by default).

	POSTGRES_DB=statussite
	POSTGRES_USER=statususer
	POSTGRES_PASSWORD=SomethingWeird15

	DOTNET_TAG=master

!!! warning
    Environmental variables define database connection settings which you will use in `appsettings.yml`.
	For example, for the above env variables, this would be an appropriate database connection string.

		#!yml hl_lines="2"
		Secrets:
			ConnectionString: "User ID=statususer;Password=SomethingWeird15;Host=database;Port=5432;Database=statussite;Pooling=false;CommandTimeout=300;"

## Manual deployment

Application is packaged as a collection of docker images with the `docker-compose.yml` file, which knows how to orchestrate those images, and a couple of config files.

Manual deployment procedure is as follows:

* Download artifacts archive from [GitLab](https://git.dbogatov.org/dbogatov/status-site).
* Extract its contents.
* Create `appsettings.yml` and `.env`, or use example files (renaming $1.example to $1).
* Stop app if it is running - `docker-compose -p statussite stop`.
* Pull app images - `docker-compose -p statussite pull`.
* Start app - `docker-compose -p statussite up -d --remove-orphans`.

Now, the app is served on `http://localhost:5555`.

!!! summary
    Here are the helpful links:
	
	* [ASP.Core: Hosting and Deployment](https://docs.microsoft.com/en-us/aspnet/core/publishing/)
