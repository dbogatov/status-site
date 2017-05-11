# Status Monitor

## Config

Configuration is read from `appsettings.json` file that needs to be placed in the same directory as the `docker-compose.yml`.
Application reads two AppSettings files - internal `appsettings.json` (`src/appsettings.json`) with default values and then the one provided by user.
Values in the second file override values in the first one.

Bellow is the configuration file with all options explained in comments:

```
{
	"CompanyName": "Orange Inc.", // Company / organization name. Appears in the UI.
	"Secrets": { // This section needs to be overridden and kept in secret
		"ApiKey": "needs-to-be-set", // API Key to be used for API requests
		"AdminPassword": "needs-to-be-set", // Plain text password for administrator user
		"ReCaptcha": { // Settings for Google ReCaptcha (https://www.google.com/recaptcha/)
			"Enabled": false, // if false, ReCaptcha will not be displayed and checked
			"SiteKey": "needs-to-be-set", // Site key provided by Google
			"SecretKey": "needs-to-be-set" // Secret key provided by Google
		},
		"ConnectionString": "needs-to-be-set", // Connection string to PostgreSQL database
		"Email": {
			"Enabled": true, // If false, all messages sent to email service will be logged to STDOUT instead
			"ToEmail": "needs-to-be-set", // email of the recipient
			"FromTitle": "Status site notificator", // Title of sender
			"FromEmail": "needs-to-be-set", // Email of sender (also serves as login to SMTP server)
			"Password": "needs-to-be-set", // Password for SMTP server
			"Host": "needs-to-be-set", // Hostname / IP of SMTP server
			"SMTP": {
				"Port": -1, // SMTP port
				"Security": "needs-to-be-set" // SMTP Security option (one of Auto, None, SslOnConnect, StartTls, StartTlsWhenAvailable)
			}
		},
		"Slack": {
			"Enabled": true, // If false, all messages sent to slack service will be logged to STDOUT instead
			"WebHook": "needs-to-be-set" // Slack webhook URL which uniquely defines chanel (https://api.slack.com/incoming-webhooks)
		}
	},
	"Data": { // Static data put into DB during app initialization. Rarely override this section
		"PingSettings": [{ // Array of servers to ping
			"ServerUrl": "https://google.com", // URL to ping (FQDN required)
			"MaxResponseTime": 2000, // The time after which to consider service unavailable, milliseconds
			"MaxFailures": 3, // Allowed number of consecutive failures before call server dead
			"GetMethodRequired": false // If true, HTTP GET method will be used to ping, HTTP HEAD otherwise
		}],
		"AutoLabels": { // Human readable titles for AutoLabels enum
			"Normal": "Normal operation",
			"Warning": "Minor degradation",
			"Critical": "Critical Problem"
		},
		"ManualLabels": { // Human readable titles for ManualLabels enum
			"None": "",
			"Investigating": "Investigating the issue"
		},
		"CompilationStages": { // Human readable titles for CompilationStages enum
			"M4": "M4 Stage",
			"SandPiper": "SandPiper",
			"Simulation": "Simulation"
		},
		"UserActions": { // Human readable titles for UserActions enum
			"Login": "Login",
			"Logout": "Logout",
			"Register": "Register",
			"ProjectCreated": "Created a project",
			"ProjectEdited": "Edited a project",
			"Visit": "Visit"
		},
		"LogEntrySeverities": { // Human readable titles for LogEntrySeverities enum
			"Debug": "Debug",
			"Detail": "Detail",
			"User": "User",
			"Info": "Info",
			"Warn": "Warning",
			"Error": "Error",
			"Fatal": "Fatal"
		},
		"Metrics": { // Human readable titles for Metrics enum
			"CpuLoad": "CPU Load",
			"UserAction": "User Actions",
			"Compilation": "Compilations",
			"Log": "Log Messages",
			"Ping": "Response time"
		}
	},
	"Logging": { // This section (except "LogSeverityReported") refers to status site own internal logging as an application (not API logging)
		"MinLogLevel": "Information", // Minimal log level of status site itself to send to STDOUT
		"LogSeverityReported": "Error", // Minimal log entry severity to notify user about
		"Exclude": [ // Array of strings which if contained in the status site log source to ignore
			"Microsoft."
		]
	},
	"Guard": { // This section defines protection settings for some API endpoints
		"Logging": { // Protection of /api/logmessage endpoint from SPAMing
			"Requests": 10, // Number of requests to allow from a single source and category per timeframe
			"PerSeconds": 10 // Timeframe in seconds
		}
	},
	"ServiceManager": {
		"CacheService": {
			"Enabled": true, // Whether to use the service
			"Interval": 30 // How many seconds to wait between re-runs of the service
		},
		"CleanService": {
			"Enabled": true, // Whether to use the service
			"Interval": 900, // How many seconds to wait between re-runs of the service
			"MaxAge": 18000 // A number of seconds that defines a maximum age of data points and log entries before they are cleaned by the service
		},
		"PingService": {
			"Enabled": true, // Whether to use the service
			"Interval": 60 // How many seconds to wait between re-runs of the service
		},
		"DemoService": {
			"Enabled": true, // Whether to use the service
			"Interval": 30, // How many seconds to wait between re-runs of the service
			"Gaps": {
				"Enabled": false, // If true, then the gaps in data will be periodically generated
				"Frequency": 10 // N, where once in N runs a gap is generated
			}
		},
		"DiscrepancyService": {
			"Enabled": true, // Whether to use the service
			"Interval": 60, // How many seconds to wait between re-runs of the service
			"DataTimeframe": 1800, // Number of seconds of data to consider counting from the time of running the service when looking for discrepancies
			"Gaps": {
				"MaxDifference": 60 // Number of seconds multiplied by 1.5 to consider as gap
			},
			"Load": {
				"Threshold": 90, // From which load to consider server high-loaded (only CPU)
				"MaxFailures": 5 // Max allowed number of consecutive high loads before it gets reported
			}
		},
		"NotificationService": {
			"Enabled": true, // Whether to use the service
			"Interval": 30, // How many seconds to wait between re-runs of the service
			"Frequencies": { // Number of seconds to wait before sending out notifications of given severity
				"Low": 86400,
				"Medium": 360,
				"High": 60 // Example: notifications of high severity will be sent no more than once in 60 seconds
			}
		}
	}
}
```

## Files and directories

### Confirguration / Dependencies

* `bower.json` / `.bowerrc` - define packages and configuration for Bower (front-end libraries manager). 
Do not manage them manually, use `bower ...` command.
* `appsettings.json` - configuration for the app. 
The app will read it during the initialization. 
The most important thing it ontains is a database connections string **with sensitive information**.
* `build.sh` - contains build commands to produce a set of binaries executable in a runtime environemnt. 
Does **not** install any dev dependencies.
* `docker-compose.*` - configuration for Docker Compose. 
Defines which containers need to be created and how they connect to each other. 
*build* version will try to build a container for the app, rather than pull it from the registry. 
Therefore, `./build.sh` needs to be executed before.
* `nginx.conf` - contains configuration for app's server block. 
This file is mounted by NGINX Docker container when deploying.
* `status-site.csproj` - app's project file. Only *Packages* section shall be manually modified.
* `version.json` - this file is recreated during each build. 
It contains a current commit's hash. 
This file is used to display the build hash in the app itself.

### Application

* `Program.cs` - entry point. Builds and runs a server object.
* `Startup.cs` - configuration for server.
* `Controllers/` - controller classes. 
Primary purpose is to accept external web requests.
The fefault routing is /{controller-name}/{action-name}.
Default routing may be extended in Startup.ConfigureServices() method.
* `Services/` - service classes. 
Each service is responsible for a single bit of functionality. 
Each service has to be defined as an interface and implementation. 
Services are autoinjectible into controllers (see `Startup.cs`).
* `Views/` - Razor files. 
Razor is a markup/templating language. 
Razor C#-like constructions are used in HTML markup. 
For autodiscovery, views that belong to a controller need to be placed inside a folder named after that controller.
* `Views/Shared/` - master page, common layout.
* `Models` - model classes for the app.
* `Models/Entities` - models that get mapped to database tables 
(see [Entity Framework](https://docs.microsoft.com/en-us/ef/core/)).
* `Models/DataContext.cs` - class responsible for managing entities 
(see [Entity Framework](https://docs.microsoft.com/en-us/ef/core/)).
* `Extensions` - adds methods to buit-in .NET classes.
