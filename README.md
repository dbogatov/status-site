# Status site

> Status site is the application for monitoring the health of the servers and web services.

[![build status](https://git.dbogatov.org/dbogatov/status-site/badges/master/build.svg)](https://git.dbogatov.org/dbogatov/status-site/commits/master)

## Features

* Agent reporting system stats
	- CPU load
	- RAM usage*
	- SWAP usage*
	- Disk space usage*
	- Number (and names) of processes*
* Collecting logs
	- Capturing message, source, category and auxillary data
	- Rich filtering tools
	- Guard against log DoS
* Web service monitor
	- Periodically access websites or ports
	- Record responses
* Notifications
	- Slack, email, telegram*, mattermost* and other providers*
	- Different severities - with different frequencies
* Discrepancies
	- Detect discrepancies in data points (gaps, high values, ping failures)
	- Detect the start and end of discrepancy - not reported twice
* Rich API
* Served as a docker composition - easy to install, configure and update
* Different databases for old and recent data
* Extensive documentation

\* to be implemented

## How to deploy

> Detailed instruction can be found [here](https://status.dbogatov.org/docs/deployment/).

## How to develop

> Detailed instruction can be found [here](https://status.dbogatov.org/docs/development/).

## How to configure

> Detailed instruction can be found [here](https://status.dbogatov.org/docs/configuration/).

## A little story

This project has started as a side project for the [RedwoodEDA](http://www.redwoodeda.com) - a helper tool to monitor [makerchip](http://makerchip.com) servers.
It turned out to be much more sophisticated piece of software than was initially designed.
It was decided to open source the project to give community a chance to develop it further.
