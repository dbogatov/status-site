# Deployment

## TL;DR

Make sure you have [Docker](https://www.docker.com) and [Docker compose](https://docs.docker.com/compose/) installed.
Run the following command in a directory where you want your configuration files to be.

	#!bash
	curl https://status.dbogatov.org/docs/deploy.sh | bash -s -e

!!! warning
    This script does not require `sudo` privileges.
	Nevertheless, it is recommended that you examine the script before running it.

!!! note
	The `e` parameter in `bash -s -e` specifies that you want to use example configuration.
	Example configuration is conservative - most of the features are disabled, but is still enough for a basic operation of the app.
	If you want to change configuration, modify `appsettings.json` and rerun the command without `-e` argument, otherwise it will override your changes to default example configuration.
