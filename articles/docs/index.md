# Status site

!!! quote
	Status site is the application for monitoring the health of the servers and web services.

## Features

* Agent reporting system stats
	- CPU load
	- RAM usage[^1]
	- SWAP usage[^1]
	- Disk space usage[^1]
	- Number (and names) of processes[^1]
* Collecting logs
	- Capturing message, source, category and auxillary data
	- Rich filtering tools
	- Guard against log DoS
* Web service monitor
	- Periodically access websites or ports[^1]
	- Record responses[^1]
* Notifications
	- Slack, email, telegram[^1], mattermost[^1] and other providers[^1]
	- Different severities - with different frequencies
* Discrepancies
	- Detect discrepancies in data points (gaps, high values, ping failures)
	- Detect the start and end of discrepancy - not reported twice[^1]
* Rich API
* Served as a docker composition - easy to install, configure and update
* Different databases for old and recent data
* Extensive documentation

[^1]: To be implemented

## How to deploy

!!! tip
	Detailed instruction can be found [here](deployment).

## How to develop

!!! tip
	Detailed instruction can be found [here](development).

## How to configure

!!! tip
	Detailed instruction can be found [here](configuration).

## A little story

This project has started as a side project for the [RedwoodEDA](http://www.redwoodeda.com) - a helper tool to monitor [makerchip](http://makerchip.com) servers.
It turned out to be much more sophisticated piece of software than was initially designed.
It was decided to open source the project to give community a chance to develop it further.
