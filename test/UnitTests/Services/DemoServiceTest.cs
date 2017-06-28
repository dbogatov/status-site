using System;
using Xunit;
using StatusMonitor.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using StatusMonitor.Shared.Models.Entities;
using System.Net;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Daemons.Services;
using Moq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StatusMonitor.Shared.Models;
using System.Linq;
using Newtonsoft.Json;

namespace StatusMonitor.Tests.UnitTests.Services
{
	public class DemoServiceTest
	{
		private readonly IServiceProvider _serviceProvider;

		public DemoServiceTest()
		{
			var services = new ServiceCollection();

			var mockEnv = new Mock<IHostingEnvironment>();
			mockEnv
				.SetupGet(environment => environment.EnvironmentName)
				.Returns("Testing");
			var env = mockEnv.Object;

			services.RegisterSharedServices(env, new Mock<IConfiguration>().Object);

			_serviceProvider = services.BuildServiceProvider();
		}

		[Fact]
		public async Task GeneratesProperLogEntry()
		{
			// Arrange
			var context = _serviceProvider.GetRequiredService<IDataContext>();
			await context.LogEntrySeverities.AddRangeAsync(
				Enum
					.GetValues(typeof(LogEntrySeverities))
					.Cast<LogEntrySeverities>()
					.Select(e => new LogEntrySeverity
					{
						Id = e.AsInt(),
						Description = e.ToString()
					})
			);
			await context.SaveChangesAsync();

			var demo = new DemoService(
				new Mock<ILogger<DemoService>>().Object,
				context,
				new Mock<IMetricService>().Object
			);

			// Act
			var result = await demo.GenerateDemoLogAsync("the-source");

			// Assert
			Assert.NotNull(result);
			Assert.InRange(result.Category, 0, 10);
			Assert.Equal("the-source", result.Source);
			if (result.AuxiliaryData != "")
			{
				Assert.NotNull(
					JsonConvert.DeserializeObject(
						result.AuxiliaryData
					)
				);
			}
		}

		[Theory]
		[InlineData(Metrics.CpuLoad)]
		[InlineData(Metrics.Log)]
		[InlineData(Metrics.Compilation)]
		[InlineData(Metrics.Ping)]
		[InlineData(Metrics.UserAction)]
		/// <summary>
		/// Check that a proper random data point is generated for the metric type.
		/// </summary>
		/// <param name="type">Type of the metric for which data point is generated</param>
		public async Task GeneratesProperDataPoint(Metrics type)
		{
			// Arrange
			var context = _serviceProvider.GetRequiredService<IDataContext>();
			var metric = await context.Metrics.AddAsync(
				new Metric { Type = Metrics.CpuLoad.AsInt(), Source = "the-source" }
			);
			await context.LogEntrySeverities.AddRangeAsync(
				Enum
					.GetValues(typeof(LogEntrySeverities))
					.Cast<LogEntrySeverities>()
					.Select(e => new LogEntrySeverity
					{
						Id = e.AsInt(),
						Description = e.ToString()
					})
			);
			await context.CompilationStages.AddRangeAsync(
				Enum
					.GetValues(typeof(CompilationStages))
					.Cast<CompilationStages>()
					.Select(e => new CompilationStage
					{
						Id = e.AsInt(),
						Name = e.ToString()
					})
			);
			await context.SaveChangesAsync();

			var mockMetricService = new Mock<IMetricService>();
			mockMetricService
				.Setup(mock => mock.GetOrCreateMetricAsync(It.IsAny<Metrics>(), It.IsAny<string>()))
				.ReturnsAsync(metric.Entity);

			var demo = new DemoService(new Mock<ILogger<DemoService>>().Object, context, mockMetricService.Object);

			// Act
			var result = await demo.GenerateDemoDataAsync(type, "the-source");

			// Assert
			Assert.NotNull(result.Metric);
			Assert.InRange(
				result.Timestamp,
				DateTime.MinValue,
				DateTime.UtcNow
			);

			switch (type)
			{
				case Metrics.CpuLoad:
					var numericDataPoint = Assert.IsType<NumericDataPoint>(result);
					Assert.InRange(numericDataPoint.Value, 0, 100);
					break;

				case Metrics.Compilation:
					var compilationDataPoint = Assert.IsType<CompilationDataPoint>(result);
					Assert.InRange(compilationDataPoint.SourceSize, 1000, 10000);
					Assert.InRange(
						compilationDataPoint.CompileTime,
						new TimeSpan(0, 0, 0, 0, 100),
						new TimeSpan(0, 0, 0, 0, 900)
					);
					Assert.NotNull(compilationDataPoint.Stage);
					break;

				case Metrics.Log:
					var logDataPoint = Assert.IsType<LogDataPoint>(result);
					Assert.InRange(logDataPoint.Count, 1, 10);
					Assert.NotNull(logDataPoint.Severity);
					break;

				case Metrics.UserAction:
					var userActionDataPoint = Assert.IsType<UserActionDataPoint>(result);
					Assert.InRange(userActionDataPoint.Count, 1, 10);
					Assert.NotNull(userActionDataPoint.Action);
					break;

				case Metrics.Ping:
					var pingDataPoint = Assert.IsType<PingDataPoint>(result);
					Assert.True(
						pingDataPoint.HttpStatusCode == HttpStatusCode.OK.AsInt() ||
						pingDataPoint.HttpStatusCode == HttpStatusCode.ServiceUnavailable.AsInt()
					);
					if (pingDataPoint.HttpStatusCode == HttpStatusCode.OK.AsInt())
					{
						Assert.InRange(
							pingDataPoint.ResponseTime,
							new TimeSpan(0, 0, 0, 0, 100),
							new TimeSpan(0, 0, 0, 0, 900)
						);
					}
					else
					{
						Assert.Equal(new TimeSpan(0), pingDataPoint.ResponseTime);
					}
					break;
			}
		}
	}
}
