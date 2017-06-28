using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using StatusMonitor.Daemons.Services;
using StatusMonitor.Shared.Models.Entities;
using Xunit;

namespace StatusMonitor.Tests.UnitTests
{
	public class ConfigurationTest
	{
		private readonly IConfiguration _config;

		public ConfigurationTest()
		{
			// Generating configuration key-value store from file.
			_config =
				new ConfigurationBuilder()
				.AddYamlFile("appsettings.yml", optional: false)
				.Build();
		}

		[Fact]
		public void TestSecrets()
		{
			TestStringKey("Secrets:ApiKey");

			TestStringKey("Secrets:AdminPassword");

			TestBoolKey("Secrets:ReCaptcha:Enabled");
			TestStringKey("Secrets:ReCaptcha:SiteKey");
			TestStringKey("Secrets:ReCaptcha:SecretKey");

			TestStringKey("Secrets:ConnectionString");

			TestBoolKey("Secrets:Email:Enabled");
			TestStringKey("Secrets:Email:ToEmail");
			TestStringKey("Secrets:Email:FromTitle");
			TestStringKey("Secrets:Email:FromEmail");
			TestStringKey("Secrets:Email:Password");
			TestStringKey("Secrets:Email:Host");
			TestIntKey("Secrets:Email:SMTP:Port");
			TestStringKey("Secrets:Email:SMTP:Security");

			TestBoolKey("Secrets:Slack:Enabled");
			TestStringKey("Secrets:Slack:WebHook");
		}

		[Theory]
		[InlineData("AutoLabels")]
		[InlineData("ManualLabels")]
		[InlineData("CompilationStages")]
		[InlineData("LogEntrySeverities")]
		[InlineData("Metrics")]
		public void TestEnumerations(string enumType)
		{
			// Converts string to type (type has to be in the same Assembly as Program class)
			Type type = typeof(Shared.Models.DataContext)
				.GetTypeInfo()
				.Assembly
				.GetType($"StatusMonitor.Shared.Models.Entities.{enumType}");

			Enum
				.GetValues(type) // Get integer values of enum (stored as objects)
				.OfType<int>() // Convert to Enumerable of integers
				.Select(val => Convert.ChangeType(val, type)) // Convert to Enum types
				.ToList() // Convert to list
				.ForEach(obj => TestStringKey($"Data:{type}:{obj.ToString()}")); // Test key for each enum value
		}

		[Fact]
		public void TestLogging()
		{
			TestStringKey("Logging:MinLogLevel");
			TestStringKey("Logging:LogSeverityReported");
		}

		[Fact]
		public void TestGeneral()
		{
			TestStringKey("CompanyName");
		}

		[Fact]
		public void TestGuard()
		{
			TestIntKey("Guard:Logging:Requests");
			TestIntKey("Guard:Logging:PerSeconds");
		}

		[Theory]
		[InlineData(ServiceManagerServices.Cache)]
		[InlineData(ServiceManagerServices.Clean)]
		[InlineData(ServiceManagerServices.Demo)]
		[InlineData(ServiceManagerServices.Notification)]
		[InlineData(ServiceManagerServices.Discrepancy)]
		[InlineData(ServiceManagerServices.Ping)]
		public void TestServiceManagerCommon(ServiceManagerServices service)
		{
			TestBoolKey($"ServiceManager:{service.ToString()}Service:Enabled");
			TestIntKey($"ServiceManager:{service.ToString()}Service:Interval");
		}

		[Fact]
		public void TestServiceManagerSpecific()
		{
			TestIntKey("ServiceManager:CleanService:MaxAge");

			TestBoolKey("ServiceManager:DemoService:Gaps:Enabled");
			TestIntKey("ServiceManager:DemoService:Gaps:Frequency");
			
			TestIntKey("ServiceManager:DiscrepancyService:DataTimeframe");
			TestIntKey("ServiceManager:DiscrepancyService:Gaps:MaxDifference");
			TestIntKey("ServiceManager:DiscrepancyService:Load:Threshold");
			TestIntKey("ServiceManager:DiscrepancyService:Load:MaxFailures");

			TestIntKey($"ServiceManager:NotificationService:Frequencies:{NotificationSeverity.Low.ToString()}");
			TestIntKey($"ServiceManager:NotificationService:Frequencies:{NotificationSeverity.Medium.ToString()}");
			TestIntKey($"ServiceManager:NotificationService:Frequencies:{NotificationSeverity.High.ToString()}");
		}

		private void TestStringKey(string key)
		{
			Assert.NotNull(_config[key]);
			Assert.False(string.IsNullOrWhiteSpace(_config[key]));
		}

		private void TestIntKey(string key)
		{
			Assert.NotNull(_config[key]);
			Assert.True(int.TryParse(_config[key], out _));
		}

		private void TestBoolKey(string key)
		{
			Assert.NotNull(_config[key]);
			Assert.True(bool.TryParse(_config[key], out _));
		}
	}
}
