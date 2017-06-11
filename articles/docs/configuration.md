# Configuration

## Developer perspective

ASP Core provides a [built-in mechanism](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration) for configuration management.
In brief, there is a number of configuration sources[^1], which are getting read in the order you specify, and the resulting "configuration" is a merged key-value dictionary.

A key in that dictionary is string built from configuration sections concatenated with semicolon `:`, for example `Secrets:Email:SMTP:Password`.
A value is the string, which may represent a number (`"10"`), a boolean (`"true"`), or anything else (`"2009-06-01T13:45:30"`).
Configuration is built in the `Startup` method and is available for [Dependency Injection](server/#dependency-injection).

!!! tip
	It is recommended to use configuration mechanism for static values (like label titles or constants).
	This way the values can easily be mocked for testing purposes.

[^1]: From MS Docs:
> There are configuration providers for:
>
> * File formats (INI, JSON, and XML)
> * Command-line arguments
> * Environment variables
> * In-memory .NET objects
> * An encrypted user store
> * Azure Key Vault
> * Custom providers, which you install or create

## User perspective

It is required to supply `appsettings.json` file when launching the application with *docker-compose*.
When [deploying with script](deployment/) it is possible to supply *example configuration* to get app up and running.
Then user is free to change the configuration and restart the app.

## Configuration specs

`appsettings.json` follows strict JSON format, in particular:

* **no comments** ~~`{ "name": "value" // comment }`~~
* **quotes are required for keys** ~~`{ name: "value" }`~~

The following is the explanation of configuration settings.

!!! warning
    Following example is written in YAML, **however** the required format is JSON.
	There is a [plan](https://git.dbogatov.org/dbogatov/status-site/issues/11) to add an option to supply general configuration in YAML. 
	See following *example configuration* to see how it is supposed to look like in JSON.

Configuration spec:

	#!yml
	CompanyName: "Orange Inc."								# Name of your company; appears on every page of the app
	Secrets:												# This section needs to be overridden and kept in secret
		ApiKey: "SDKJF5432SMDFKL"							# API Key to be used for API requests
		AdminPassword: "strong-password-987654" 			# Plain text password for administrator user
		ReCaptcha: 											# Settings for Google ReCaptcha (https://www.google.com/recaptcha/)
			Enabled: true 									# If false, ReCaptcha will not be displayed and checked
			SiteKey: "sdahjdjhd_678ajsdvbja" 				# Site key provided by Google
			SecretKey: "asgfdk_876ajhsvdjh" 				# Secret key provided by Google
		ConnectionString: 									# Connection string to PostgreSQL database
		Email: 												# Email settings
			Enabled: true 									# If false, all messages sent to email service will be logged to STDOUT instead
			ToEmail: "recipient@domain.com" 				# Email of the recipient
			FromTitle: "Status Notificator" 				# Title of sender
			FromEmail: "status@yourdomain.com" 				# Email of sender (also serves as login to SMTP server)
			Password: "smtp-password" 						# Password for SMTP server
			Host: "smtp.host.com" 							# Hostname / IP of SMTP server
			SMTP: 											# SMTP settings
				Port: 537 									# SMTP port
				Security: "StartTls" 						# SMTP Security option (one of Auto, None, SslOnConnect, StartTls, StartTlsWhenAvailable)
		Slack: 												# Slack settings
			Enabled: true 									# If false, all messages sent to slack service will be logged to STDOUT instead
			WebHook: "https://slack.com/webhooks/a78" 		# Slack webhook URL which uniquely defines chanel (https://api.slack.com/incoming-webhooks)
		Data: 												# Static data put into DB during app initialization. Rarely override this section
			PingSettings: 									# Array of servers to ping
				-	ServerUrl: "https://google.com" 		# URL to ping (FQDN required)
					MaxResponseTime: 2000 					# The time after which to consider service unavailable, milliseconds
					MaxFailures: 3 							# Allowed number of consecutive failures before call server dead
					GetMethodRequired: false 				# If true, HTTP GET method will be used to ping, HTTP HEAD otherwise
		Logging: 											# This section (except "LogSeverityReported") refers to status site own internal logging as an application (not API logging)
			MinLogLevel: "Information" 						# Minimal log level of status site itself to send to STDOUT
			LogSeverityReported: "Error" 					# Minimal log entry severity to notify user about
			Exclude 										# Array of strings which - if contained in the status site log source - to ignore
			- "Microsoft."									# Example of such string. All messages from "Microsoft" are ignored
		Guard: 												# This section defines protection settings for some API endpoints
			Logging: 										# Protection of /api/logmessage endpoint from SPAMing
			Requests: 10 									# Number of requests to allow from a single source and category per timeframe
			PerSeconds: 10 									# Timeframe in seconds
		ServiceManager: 									# Settings for daemon part of the app
			CacheService: 									# Settings for cache service of the app
				Enabled: true 								# Whether to use the service
				Interval: 30 								# How many seconds to wait between re-runs of the service
			CleanService: 									# Settings for clean service of the app
				Enabled: true 								# Whether to use the service
				Interval: 900 								# How many seconds to wait between re-runs of the service
				MaxAge: 18000 								# A number of seconds that defines a maximum age of data points and log entries before they are cleaned by the service
			PingService: 									# Settings for ping service of the app
				Enabled: true 								# Whether to use the service
				Interval: 60								# How many seconds to wait between re-runs of the service
			DemoService: 									# Settings for demo service of the app
				Enabled: true 								# Whether to use the service
				Interval: 30 								# How many seconds to wait between re-runs of the service
				Gaps: 										# Demo service may generate gaps in data
					Enabled: true 							# If true, then the gaps in data will be periodically generated
					Frequency: 10 							# N, where once in N runs a gap is generated
			DiscrepancyService: 							# Settings for discrepancy service of the app
				Enabled: ture 								# Whether to use the service
				Interval: 60 								# How many seconds to wait between re-runs of the service
				DataTimeframe: 800 							# Number of seconds of data to consider counting from the time of running the service when looking for discrepancies
				Gaps: 										# Settings for type of discrepancy "Gap in data"
					MaxDifference: 60 						# Number of seconds multiplied by 1.5 to consider as gap
				Load: 										# Settings for type of discrepancy "High load" 
					Threshold: 90 							# From which load to consider server high-loaded (only CPU Load metrics)
					MaxFailures: 6 							# Max allowed number of consecutive high loads before it gets reported
			NotificationService 							# Settings for notification service of the app
				Enabled: true 								# Whether to use the service
				Interval: 20								# How many seconds to wait between re-runs of the service
				Frequencies: 								# Number of seconds to wait before sending out notifications of given severity
					Low: 86400 								# Example: notifications of low severity will be sent no more than once in 86400 seconds
					Medium: 360 							# Example: notifications of medium severity will be sent no more than once in 360 seconds
					High: 60 								# Example: notifications of high severity will be sent no more than once in 60 seconds


Here is the *example configuration*:

	#!json
	{
		"CompanyName": "Your company",
		"Secrets": {
			"ApiKey": "needs-to-be-set",
			"AdminPassword": "needs-to-be-set",
			"ReCaptcha": {
				"Enabled": false,
				"SiteKey": "needs-to-be-set",
				"SecretKey": "needs-to-be-set"
			},
			"ConnectionString": "needs-to-be-set",
			"Email": {
				"Enabled": true,
				"ToEmail": "needs-to-be-set",
				"FromTitle": "Status site notificator",
				"FromEmail": "needs-to-be-set",
				"Password": "needs-to-be-set",
				"Host": "needs-to-be-set",
				"SMTP": {
					"Port": -1,
					"Security": "needs-to-be-set"
				}
			},
			"Slack": {
				"Enabled": true,
				"WebHook": "needs-to-be-set"
			}
		},
		"Data": {
			"PingSettings": [{
				"ServerUrl": "https://google.com"
			}]
		},
		"Logging": {
			"MinLogLevel": "Information",
			"LogSeverityReported": "Error",
			"Exclude": [
				"Microsoft."
			]
		},
		"Guard": {
			"Logging": {
				"Requests": 10,
				"PerSeconds": 10
			}
		},
		"ServiceManager": {
			"CacheService": {
				"Enabled": true,
				"Interval": 30
			},
			"CleanService": {
				"Enabled": true,
				"Interval": 900,
				"MaxAge": 18000
			},
			"PingService": {
				"Enabled": true,
				"Interval": 60
			},
			"DemoService": {
				"Enabled": true,
				"Interval": 30,
				"Gaps": {
					"Enabled": false,
					"Frequency": 10
				}
			},
			"DiscrepancyService": {
				"Enabled": true,
				"Interval": 60,
				"DataTimeframe": 1800,
				"Gaps": {
					"MaxDifference": 60
				},
				"Load": {
					"Threshold": 90,
					"MaxFailures": 5
				}
			},
			"NotificationService": {
				"Enabled": true,
				"Interval": 30,
				"Frequencies": {
					"Low": 86400,
					"Medium": 360,
					"High": 60
				}
			}
		}
	}

!!! summary
    Here are the helpful links:
	
	* [ASP.Core: Configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration)

