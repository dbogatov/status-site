using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Configuration;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models.Entities;

namespace StatusMonitor.Web.TagHelpers
{
	[HtmlTargetElement("settings-viewer")]
	/// <summary>
	/// Underlying class for <settings-viewer> tag.
	/// </summary>
	public class SettingsViewerTagHelper : TagHelper
	{
		private IConfiguration _config;

		public SettingsViewerTagHelper(IConfiguration config)
		{
			_config = config;
		}

		/// <summary>
		/// Called by the framework. Renders tag helper.
		/// </summary>
		public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
			output.TagName = "div";
			output.TagMode = TagMode.StartTagAndEndTag;

			output.Attributes.Clear();
			output.Attributes.Add("class", "settings-viewer");

			output.Content.SetHtmlContent(GenerateContent());

			await Task.CompletedTask;
		}

		private string GenerateContent()
		{
			return $@"
				{GenerateHeading("General settings", "")}
				{GenerateTableSettings(new Dictionary<string, string> {
					{ "Company name", _config["CompanyName"] }
				})}

				{GenerateHeading("Sensitive settings", "Passwords, keys, tokens...")}
				{GenerateTableSettings(new Dictionary<string, string> {
					{ "API Key", _config["Secrets:ApiKey"] },
					{ "Admin password", _config["Secrets:AdminPassword"] },
					{ "Database connection string", _config["Secrets:ConnectionString"] },
					{ "ReCaptcha", GenerateReCaptchaInfo() },
					{ "Email", GenerateEmailInfo() },
					{ "Slack", GenerateSlackInfo() }
				})}

				{GenerateHeading("Logging settings", "Log level, exclusions...")}
				{GenerateTableSettings(new Dictionary<string, string> {
					{ "Minimal log level", _config["Logging:MinLogLevel"] },
					{ "Log entry severities to notify", $"{_config["Logging:LogSeverityReported"]} and higher" },
					{
						"Exclusions",
						_config
							.StringsFromArray("Logging:Exclude")
							.Aggregate((self, next) => $"\"{next}\", \"{self}\"")
					}
				})}

				{GenerateHeading("Log guard", "Settings that controll log message requests throttling")}
				{GenerateTableSettings(new Dictionary<string, string> {
					{ "Number of requests per timeframe", _config["Guard:Logging:Requests"] },
					{ "Timeframe (seconds)", _config["Guard:Logging:PerSeconds"] }
				})}

				{GenerateHeading("Enumerations values", "Metrics, labels, severities...")}
				{GenerateTableSettings(new Dictionary<string, string> {
					{ "Auto labels", GenerateEnumConfigValues(typeof(AutoLabels)) },
					{ "Manual labels", GenerateEnumConfigValues(typeof(ManualLabels)) },
					{ "Compilation stages", GenerateEnumConfigValues(typeof(CompilationStages)) },
					{ "Log entry severities", GenerateEnumConfigValues(typeof(LogEntrySeverities)) },
					{ "Metrics", GenerateEnumConfigValues(typeof(Metrics)) }
				})}

				{GenerateHeading("Service manager settings", "Cache, clean, demo...")}
				{GenerateTableSettings(new Dictionary<string, string> {
					{ "Cache service", GenerateCacheServiceInfo() },
					{ "Clean service", GenerateCleanServiceInfo() },
					{ "Ping service", GeneratePingServiceInfo() },
					{ "Demo service", GenerateDemoServiceInfo() },
					{ "Discrepancy service", GenerateDiscrepancyServiceInfo() },
					{ "Notification service", GenerateNotificationServiceInfo() }
				})}

				{(
					_config.SectionsFromArray("Data:PingSettings").Count() > 0 ?
					$@"
						{GenerateHeading("Ping settings", "Which servers to ping")}	
						{
							GenerateTableSettings(
								_config
									.SectionsFromArray("Data:PingSettings")
									.ToDictionary(
										section => section["ServerUrl"],
										section => GeneratePingSettingValue(section))
							)
						}
					" :
					""
				)}
			";
		}

		private string GenerateHeading(string title, string description)
		{
			return $@"
				<h5>{title}</h5>
				<small>{description}</small>
			";
		}

		private string GenerateTableSettings(Dictionary<string, string> data)
		{
			return $@"
				<div class='table-responsive' style='padding-top: 10px;'>
					<table class='table table-hover table-condensed'>
						<tbody>
							{
								data
									.Select(pair => $@"
										<tr>
											<td>{pair.Key}</td>
											<td>{pair.Value}</td>
										</tr>"
									)
									.Aggregate((self, next) => $"{self}{next}")
							}
						</tbody>
					</table>
				</div>
			";
		}

		private string GenerateEnumConfigValues(Type type)
		{
			return Enum
				.GetValues(type)
				.Cast<object>()
				.Select(obj => _config[$"Data:{type.ToShortString()}:{obj.ToString()}"])
				.Aggregate((self, next) => $"{next}, {(string.IsNullOrWhiteSpace(self) ? "(empty)" : self)}");
		}

		private string GenerateEnabledDisabled(string setting)
		{
			return Convert.ToBoolean(_config[setting]) ? "Enabled": "Disbaled";
		}

		private string GenerateEnabledDisabledInfo(string service, string setting, string info)
		{
			return $@"
				{service} is {GenerateEnabledDisabled(setting).ToLower()}. 
				{(
					Convert.ToBoolean(_config[setting]) ? 
					info : 
					""
				)}";
		}


		private string GeneratePingSettingValue(IConfigurationSection section)
		{
			return $@"
				URL is {section["ServerUrl"]}
				{(section["MaxResponseTime"] != null ? $", max response time is {section["MaxResponseTime"]}" : "")}
				{(section["MaxFailures"] != null ? $", max number of failures is {section["MaxFailures"]}" : "")}
				{(section["GetMethodRequired"] != null ? $", should be accessed by {(Convert.ToBoolean(section["GetMethodRequired"]) ? "GET" : "HEAD")} method" : "")}
				.
			"
			.Replace(Environment.NewLine, "")
			.Replace(" ,", ",")
			.Replace(" .", ".");
		}

		private string GenerateReCaptchaInfo()
		{
			return GenerateEnabledDisabledInfo(
				"ReCaptcha",
				"Secrets:ReCaptcha:Enabled",
				$"Site key is {_config["Secrets:ReCaptcha:SiteKey"]}, secret key is {_config["Secrets:ReCaptcha:SecretKey"]}."
			);
		}

		private string GenerateEmailInfo()
		{
			return GenerateEnabledDisabledInfo(
				"Email",
				"Secrets:Email:Enabled",
				$@"Messages will be sent to {_config["Secrets:Email:ToEmail"]} 
				from {_config["Secrets:Email:FromEmail"]} (will appear as '{_config["Secrets:Email:FromTitle"]}').
				SMTP authentication uses port {_config["Secrets:Email:SMTP:Port"]} on host {_config["Secrets:Email:Host"]} with password '{_config["Secrets:Email:Password"]}' and security '{_config["Secrets:Email:SMTP:Security"]}'."
			);
		}

		private string GenerateSlackInfo()
		{
			return GenerateEnabledDisabledInfo(
				"Slack",
				"Secrets:Slack:Enabled",
				$"Webhook is {_config["Secrets:Slack:Webhook"]}."
			);
		}

		private string GenerateCacheServiceInfo()
		{
			return GenerateEnabledDisabledInfo(
				"Cache service",
				"ServiceManager:CacheService:Enabled",
				$"The interval is {_config["ServiceManager:CacheService:Interval"]} seconds."
			);
		}

		private string GeneratePingServiceInfo()
		{
			return GenerateEnabledDisabledInfo(
				"Ping service",
				"ServiceManager:PingService:Enabled",
				$"The interval is {_config["ServiceManager:PingService:Interval"]} seconds."
			);
		}

		private string GenerateCleanServiceInfo()
		{
			return GenerateEnabledDisabledInfo(
				"Clean service",
				"ServiceManager:CleanService:Enabled",
				$"The interval is {_config["ServiceManager:CleanService:Interval"]} seconds with logs and datapoints max age set to {_config["ServiceManager:CleanService:MaxAge"]} seconds."
			);
		}

		private string GenerateDemoServiceInfo()
		{
			return GenerateEnabledDisabledInfo(
				"Demo service",
				"ServiceManager:DemoService:Enabled",
				$@"The interval is {_config["ServiceManager:DemoService:Interval"]} seconds with gaps {GenerateEnabledDisabled("ServiceManager:DemoService:Gaps:Enabled").ToLower()}. 
				{(
					Convert.ToBoolean(_config["ServiceManager:DemoService:Gaps:Enabled"]) ?
					$"Gaps will be generated once per {_config["ServiceManager:DemoService:Gaps:Frequency"]} runs." :
					""
				)}"
			);
		}

		private string GenerateDiscrepancyServiceInfo()
		{
			return GenerateEnabledDisabledInfo(
				"Discrepancy service",
				"ServiceManager:DiscrepancyService:Enabled",
				$@"
					The interval is {_config["ServiceManager:DiscrepancyService:Interval"]}.
					Service will analyze {_config["ServiceManager:DiscrepancyService:DataTimeframe"]} seconds of data per run.
					'Gap in data' discrepancy will be reported if time difference between any two consecutive datapoints is more than 1.5x of {_config["ServiceManager:DiscrepancyService:Gaps:MaxDifference"]} seconds.
					'High load' discrepancy will be reported if load of {_config["ServiceManager:DiscrepancyService:Load:Threshold"]}+ occurs for more than {_config["ServiceManager:DiscrepancyService:Load:MaxFailures"]} consecutive recordings."
			);
		}

		private string GenerateNotificationServiceInfo()
		{
			return GenerateEnabledDisabledInfo(
				"Notification service",
				"ServiceManager:NotificationService:Enabled",
				$@"
					The interval is {_config["ServiceManager:NotificationService:Interval"]} seconds.
					{
						Enum
							.GetValues(typeof(NotificationSeverity))
							.Cast<object>()
							.Select(obj => new { 
								Severity = obj.ToString(),
								Frequency = _config[$"ServiceManager:NotificationService:Frequencies:{obj.ToString()}"] 
							})
							.Select(obj => $"Recipient will get notifications of severity {obj.Severity} no more than once in {obj.Frequency} seconds.")
							.Aggregate(
								(self, next) => $"{next}{Environment.NewLine}{self}"
							)
					}
				"
			);
		}
	}
}
