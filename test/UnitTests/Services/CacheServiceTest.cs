using System;
using Xunit;
using StatusMonitor.Shared.Models.Entities;
using StatusMonitor.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using StatusMonitor.Daemons.Services;
using Moq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using StatusMonitor.Shared.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Net;

namespace StatusMonitor.Tests.UnitTests.Services
{
	public class CacheServiceTest
	{
		private readonly IServiceProvider _serviceProvider;

		public CacheServiceTest()
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
		public async Task ProperlyCacheMetric()
		{
			// Arrange
			var context = _serviceProvider.GetRequiredService<IDataContext>();

			var metric = (await context.Metrics.AddAsync(new Metric
			{
				Type = Metrics.CpuLoad.AsInt(),
				Source = "test-source"
			})).Entity;

			await context.AutoLabels.AddRangeAsync(new List<AutoLabel> {
				new AutoLabel { Id = AutoLabels.Normal.AsInt() },
				new AutoLabel { Id = AutoLabels.Critical.AsInt() },
				new AutoLabel { Id = AutoLabels.Warning.AsInt() },
			});

			await context.NumericDataPoints.AddRangeAsync(
				new NumericDataPoint
				{
					Value = 6,
					Timestamp = DateTime.UtcNow,
					Metric = metric
				},
				new NumericDataPoint
				{
					Value = 10,
					Timestamp = DateTime.UtcNow - new TimeSpan(0, 0, 10),
					Metric = metric
				},
				new NumericDataPoint
				{
					Value = 20,
					Timestamp = DateTime.UtcNow - new TimeSpan(0, 0, 10),
					Metric = metric
				}
			);

			await context.NumericDataPoints.AddRangeAsync(
				new NumericDataPoint
				{
					Value = 12,
					Timestamp = DateTime.UtcNow - new TimeSpan(2, 0, 0),
					Metric = metric
				},
				new NumericDataPoint
				{
					Value = 20,
					Timestamp = DateTime.UtcNow - new TimeSpan(2, 0, 0),
					Metric = metric
				},
				new NumericDataPoint
				{
					Value = 40,
					Timestamp = DateTime.UtcNow - new TimeSpan(2, 0, 0),
					Metric = metric
				}
			);

			await context.NumericDataPoints.AddRangeAsync(
				new NumericDataPoint
				{
					Value = 24,
					Timestamp = DateTime.UtcNow - new TimeSpan(2, 0, 0, 0),
					Metric = metric
				},
				new NumericDataPoint
				{
					Value = 40,
					Timestamp = DateTime.UtcNow - new TimeSpan(2, 0, 0, 0),
					Metric = metric
				},
				new NumericDataPoint
				{
					Value = 80,
					Timestamp = DateTime.UtcNow - new TimeSpan(2, 0, 0, 0),
					Metric = metric
				}
			);

			await context.SaveChangesAsync();

			var expectedMetric = new Metric
			{
				Public = metric.Public,
				Title = metric.Title,
				Type = metric.Type,
				Source = metric.Source,

				DayAvg = (6 + 10 + 20 + 12 + 20 + 40) / 6,
				DayMin = 6,
				DayMax = 40,

				HourAvg = (6 + 10 + 20) / 3,
				HourMin = 6,
				HourMax = 20,

				CurrentValue = 6,

				AutoLabel = new AutoLabel { Id = AutoLabels.Normal.AsInt() }
			};

			// Act
			var cachedMetric = await new CacheService(context, new Mock<ILogger<CacheService>>().Object)
				.CacheMetricAsync(metric);

			// Assert
			Assert.Equal(expectedMetric, cachedMetric, new MetricComparer());
		}

		[Theory]
		[InlineData(Metrics.CpuLoad)]
		[InlineData(Metrics.Ping)]
		public async Task LabelForTooFewPoints(Metrics type)
		{
			// Arrange
			var context = _serviceProvider.GetRequiredService<IDataContext>();

			var metric = (await context.Metrics.AddAsync(new Metric
			{
				Type = type.AsInt(),
				Source = "smth.com"
			})).Entity;

			await context.AutoLabels.AddRangeAsync(new List<AutoLabel> {
				new AutoLabel { Id = AutoLabels.Normal.AsInt() },
				new AutoLabel { Id = AutoLabels.Critical.AsInt() },
				new AutoLabel { Id = AutoLabels.Warning.AsInt() },
			});

			switch (type)
			{
				case Metrics.CpuLoad:
					await context.NumericDataPoints.AddRangeAsync(
						new NumericDataPoint { Value = 23, Metric = metric },
						new NumericDataPoint { Value = 65, Metric = metric },
						new NumericDataPoint { Value = 43, Metric = metric }
					);
					break;
				case Metrics.Ping:
					await context.PingSettings.AddAsync(
						new PingSetting
						{
							ServerUrl = "https://smth.com",
							MaxResponseTime = new TimeSpan(0, 0, 0, 0, 1000),
							MaxFailures = 5
						}
					);
					await context.PingDataPoints.AddRangeAsync(
						new PingDataPoint
						{
							HttpStatusCode = HttpStatusCode.OK.AsInt(),
							ResponseTime = new TimeSpan(0, 0, 0, 0, 432),
							Metric = metric
						},
						new PingDataPoint
						{
							HttpStatusCode = HttpStatusCode.OK.AsInt(),
							ResponseTime = new TimeSpan(0, 0, 0, 0, 750),
							Metric = metric
						},
						new PingDataPoint
						{
							HttpStatusCode = HttpStatusCode.OK.AsInt(),
							ResponseTime = new TimeSpan(0, 0, 0, 0, 750),
							Metric = metric
						},
						new PingDataPoint
						{
							HttpStatusCode = HttpStatusCode.ServiceUnavailable.AsInt(),
							ResponseTime = new TimeSpan(0, 0, 0, 0, 70),
							Metric = metric
						},
						new PingDataPoint
						{
							HttpStatusCode = HttpStatusCode.ServiceUnavailable.AsInt(),
							ResponseTime = new TimeSpan(0, 0, 0, 0, 70),
							Metric = metric
						}
					);
					break;
			}

			await context.SaveChangesAsync();

			var expectedLabel = AutoLabels.Normal;

			// Act
			var cachedMetric = await new CacheService(context, new Mock<ILogger<CacheService>>().Object)
				.CacheMetricAsync(metric);

			// Assert
			Assert.Equal(expectedLabel.AsInt(), cachedMetric.AutoLabel.Id);
		}

		[Theory]
		[InlineData(Metrics.CpuLoad)]
		[InlineData(Metrics.Ping)]
		public async Task LabelNormal(Metrics type)
		{
			// Arrange
			var context = _serviceProvider.GetRequiredService<IDataContext>();

			var metric = (await context.Metrics.AddAsync(new Metric
			{
				Type = type.AsInt(),
				Source = "smth.com"
			})).Entity;

			await context.AutoLabels.AddRangeAsync(new List<AutoLabel> {
				new AutoLabel { Id = AutoLabels.Normal.AsInt() },
				new AutoLabel { Id = AutoLabels.Critical.AsInt() },
				new AutoLabel { Id = AutoLabels.Warning.AsInt() },
			});

			switch (type)
			{
				case Metrics.CpuLoad:
					await context.NumericDataPoints.AddRangeAsync(
						new NumericDataPoint { Value = 23, Metric = metric },
						new NumericDataPoint { Value = 65, Metric = metric },
						new NumericDataPoint { Value = 43, Metric = metric },
						new NumericDataPoint { Value = 45, Metric = metric },
						new NumericDataPoint { Value = 34, Metric = metric }
					);
					break;
				case Metrics.Ping:
					await context.PingSettings.AddAsync(
						new PingSetting
						{
							ServerUrl = "https://smth.com",
							MaxResponseTime = new TimeSpan(0, 0, 0, 0, 1000),
							MaxFailures = 5
						}
					);
					await context.PingDataPoints.AddRangeAsync(
						new PingDataPoint
						{
							HttpStatusCode = HttpStatusCode.OK.AsInt(),
							ResponseTime = new TimeSpan(0, 0, 0, 0, 432),
							Metric = metric
						},
						new PingDataPoint
						{
							HttpStatusCode = HttpStatusCode.OK.AsInt(),
							ResponseTime = new TimeSpan(0, 0, 0, 0, 750),
							Metric = metric
						},
						new PingDataPoint
						{
							HttpStatusCode = HttpStatusCode.ServiceUnavailable.AsInt(),
							ResponseTime = new TimeSpan(0, 0, 0, 0, 70),
							Metric = metric
						},
						new PingDataPoint
						{
							HttpStatusCode = HttpStatusCode.OK.AsInt(),
							ResponseTime = new TimeSpan(0, 0, 0, 0, 876),
							Metric = metric
						},
						new PingDataPoint
						{
							HttpStatusCode = HttpStatusCode.ServiceUnavailable.AsInt(),
							ResponseTime = new TimeSpan(0, 0, 0, 0, 345),
							Metric = metric
						},
						new PingDataPoint
						{
							HttpStatusCode = HttpStatusCode.OK.AsInt(),
							ResponseTime = new TimeSpan(0, 0, 0, 0, 750),
							Metric = metric
						},
						new PingDataPoint
						{
							HttpStatusCode = HttpStatusCode.OK.AsInt(),
							ResponseTime = new TimeSpan(0, 0, 0, 0, 850),
							Metric = metric
						},
						new PingDataPoint
						{
							HttpStatusCode = HttpStatusCode.OK.AsInt(),
							ResponseTime = new TimeSpan(0, 0, 0, 0, 50),
							Metric = metric
						},
						new PingDataPoint
						{
							HttpStatusCode = HttpStatusCode.OK.AsInt(),
							ResponseTime = new TimeSpan(0, 0, 0, 0, 740),
							Metric = metric
						},
						new PingDataPoint
						{
							HttpStatusCode = HttpStatusCode.OK.AsInt(),
							ResponseTime = new TimeSpan(0, 0, 0, 0, 234),
							Metric = metric,
							Timestamp = DateTime.UtcNow.AddSeconds(5), // make sure it is the most recent data point
						}
					);
					break;
			}

			await context.SaveChangesAsync();

			var expectedLabel = AutoLabels.Normal;

			// Act
			var cachedMetric = await new CacheService(context, new Mock<ILogger<CacheService>>().Object)
				.CacheMetricAsync(metric);

			// Assert
			Assert.Equal(expectedLabel.AsInt(), cachedMetric.AutoLabel.Id);
		}

		[Theory]
		[InlineData(Metrics.CpuLoad)]
		[InlineData(Metrics.Ping)]
		public async Task LabelWarning(Metrics type)
		{
			// Arrange
			var context = _serviceProvider.GetRequiredService<IDataContext>();

			var metric = (await context.Metrics.AddAsync(new Metric
			{
				Type = type.AsInt(),
				Source = "smth.com"
			})).Entity;

			await context.AutoLabels.AddRangeAsync(new List<AutoLabel> {
				new AutoLabel { Id = AutoLabels.Normal.AsInt() },
				new AutoLabel { Id = AutoLabels.Critical.AsInt() },
				new AutoLabel { Id = AutoLabels.Warning.AsInt() },
			});

			switch (type)
			{
				case Metrics.CpuLoad:
					await context.NumericDataPoints.AddRangeAsync(
						new NumericDataPoint { Value = 56, Metric = metric },
						new NumericDataPoint { Value = 65, Metric = metric },
						new NumericDataPoint { Value = 43, Metric = metric },
						new NumericDataPoint { Value = 45, Metric = metric },
						new NumericDataPoint { Value = 90, Metric = metric }
					);
					break;
				case Metrics.Ping:
					await context.PingSettings.AddAsync(
						new PingSetting
						{
							ServerUrl = "https://smth.com",
							MaxFailures = 5
						}
					);
					await context.PingDataPoints.AddRangeAsync(
						new PingDataPoint
						{
							HttpStatusCode = HttpStatusCode.OK.AsInt(),
							Timestamp = DateTime.UtcNow.AddSeconds(0),
							Metric = metric
						},
						new PingDataPoint
						{
							HttpStatusCode = HttpStatusCode.OK.AsInt(),
							Timestamp = DateTime.UtcNow.AddSeconds(1),
							Metric = metric
						},
						new PingDataPoint
						{
							HttpStatusCode = HttpStatusCode.ServiceUnavailable.AsInt(),
							Timestamp = DateTime.UtcNow.AddSeconds(2),
							Metric = metric
						},
						new PingDataPoint
						{
							HttpStatusCode = HttpStatusCode.OK.AsInt(),
							Timestamp = DateTime.UtcNow.AddSeconds(1),
							Metric = metric
						},
						new PingDataPoint
						{
							HttpStatusCode = HttpStatusCode.OK.AsInt(),
							Timestamp = DateTime.UtcNow.AddSeconds(1),
							Metric = metric
						},
						new PingDataPoint
						{
							HttpStatusCode = HttpStatusCode.OK.AsInt(),
							Timestamp = DateTime.UtcNow.AddSeconds(1),
							Metric = metric
						},
						new PingDataPoint
						{
							HttpStatusCode = HttpStatusCode.ServiceUnavailable.AsInt(),
							Timestamp = DateTime.UtcNow.AddSeconds(2),
							Metric = metric
						},
						new PingDataPoint
						{
							HttpStatusCode = HttpStatusCode.OK.AsInt(),
							Timestamp = DateTime.UtcNow.AddSeconds(1),
							Metric = metric
						},
						new PingDataPoint
						{
							HttpStatusCode = HttpStatusCode.OK.AsInt(),
							Timestamp = DateTime.UtcNow.AddSeconds(1),
							Metric = metric
						},
						new PingDataPoint
						{
							HttpStatusCode = HttpStatusCode.ServiceUnavailable.AsInt(),
							Timestamp = DateTime.UtcNow.AddSeconds(2),
							Metric = metric
						}
					);
					break;
			}

			await context.SaveChangesAsync();

			var expectedLabel = AutoLabels.Warning;

			// Act
			var cachedMetric = await new CacheService(context, new Mock<ILogger<CacheService>>().Object)
				.CacheMetricAsync(metric);

			// Assert
			Assert.Equal(expectedLabel.AsInt(), cachedMetric.AutoLabel.Id);
		}

		[Theory]
		[InlineData(Metrics.CpuLoad)]
		[InlineData(Metrics.Ping)]
		public async Task LabelCritical(Metrics type)
		{
			// Arrange
			var context = _serviceProvider.GetRequiredService<IDataContext>();

			var metric = (await context.Metrics.AddAsync(new Metric
			{
				Type = type.AsInt(),
				Source = "smth.com"
			})).Entity;

			await context.AutoLabels.AddRangeAsync(new List<AutoLabel> {
				new AutoLabel { Id = AutoLabels.Normal.AsInt() },
				new AutoLabel { Id = AutoLabels.Critical.AsInt() },
				new AutoLabel { Id = AutoLabels.Warning.AsInt() },
			});

			switch (type)
			{
				case Metrics.CpuLoad:
					await context.NumericDataPoints.AddRangeAsync(
						new NumericDataPoint { Value = 90, Metric = metric },
						new NumericDataPoint { Value = 92, Metric = metric },
						new NumericDataPoint { Value = 88, Metric = metric },
						new NumericDataPoint { Value = 89, Metric = metric },
						new NumericDataPoint { Value = 100, Metric = metric }
					);
					break;
				case Metrics.Ping:
					await context.PingSettings.AddAsync(
						new PingSetting
						{
							ServerUrl = "https://smth.com",
							MaxFailures = 5
						}
					);
					await context.PingDataPoints.AddRangeAsync(
						new PingDataPoint { HttpStatusCode = HttpStatusCode.OK.AsInt(), Metric = metric },
						new PingDataPoint { HttpStatusCode = HttpStatusCode.OK.AsInt(), Metric = metric },
						new PingDataPoint { HttpStatusCode = HttpStatusCode.ServiceUnavailable.AsInt(), Metric = metric },
						new PingDataPoint { HttpStatusCode = HttpStatusCode.OK.AsInt(), Metric = metric },
						new PingDataPoint { HttpStatusCode = HttpStatusCode.ServiceUnavailable.AsInt(), Metric = metric },
						new PingDataPoint { HttpStatusCode = HttpStatusCode.ServiceUnavailable.AsInt(), Metric = metric },
						new PingDataPoint { HttpStatusCode = HttpStatusCode.OK.AsInt(), Metric = metric },
						new PingDataPoint { HttpStatusCode = HttpStatusCode.ServiceUnavailable.AsInt(), Metric = metric },
						new PingDataPoint { HttpStatusCode = HttpStatusCode.ServiceUnavailable.AsInt(), Metric = metric },
						new PingDataPoint { HttpStatusCode = HttpStatusCode.OK.AsInt(), Metric = metric }
					);
					break;
			}

			await context.SaveChangesAsync();

			var expectedLabel = AutoLabels.Critical;

			// Act
			var cachedMetric = await new CacheService(context, new Mock<ILogger<CacheService>>().Object)
				.CacheMetricAsync(metric);

			// Assert
			Assert.Equal(expectedLabel.AsInt(), cachedMetric.AutoLabel.Id);
		}
	}
}
