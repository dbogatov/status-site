using System;
using Xunit;
using StatusMonitor.Shared.Models.Entities;
using StatusMonitor.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Collections.Generic;
using StatusMonitor.Shared.Extensions;
using Moq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using StatusMonitor.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace StatusMonitor.Tests.UnitTests.Services
{
	public class MetricServiceTest
	{
		private readonly IServiceProvider _serviceProvider;

		public MetricServiceTest()
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
		public async Task ReturnsExistingMetricOrNull()
		{
			// Arrange
			var context = _serviceProvider.GetRequiredService<IDataContext>();
			await context.Metrics.AddAsync(new Metric { Type = Metrics.CpuLoad.AsInt(), Source = "existing-source" });
			await context.SaveChangesAsync();

			var metricService = new MetricService(context, new Mock<ILogger<MetricService>>().Object);

			// Act
			var nonExisting = await metricService.GetMetricsAsync(Metrics.CpuLoad, "non-existing-source");
			var existing = await metricService.GetMetricsAsync(Metrics.CpuLoad, "existing-source");

			// Assert
			Assert.Empty(nonExisting);
			Assert.NotEmpty(existing);
		}

		[Fact]
		public async Task CreatesMetric()
		{
			// Arrange
			var context = _serviceProvider.GetRequiredService<IDataContext>();
			await context.AbstractMetrics.AddAsync(new AbstractMetric { Type = Metrics.CpuLoad.AsInt() });
			await context.AutoLabels.AddAsync(new AutoLabel { Id = AutoLabels.Normal.AsInt() });
			await context.ManualLabels.AddAsync(new ManualLabel { Id = ManualLabels.None.AsInt() });
			await context.SaveChangesAsync();

			var metricService = new MetricService(context, new Mock<ILogger<MetricService>>().Object);

			// Act
			var metric = await metricService.GetOrCreateMetricAsync(Metrics.CpuLoad, "the-source");

			// Assert
			Assert.NotNull(metric);
			Assert.Equal(Metrics.CpuLoad.AsInt(), metric.Type);
			Assert.Equal("the-source", metric.Source);
			Assert.True(
				await context.Metrics.AnyAsync(mt => mt.Type == Metrics.CpuLoad.AsInt() && mt.Source == "the-source")
			);
		}

		[Fact]
		public async Task ReturnsMetric()
		{
			// Arrange
			var context = _serviceProvider.GetRequiredService<IDataContext>();
			await context.Metrics.AddAsync(
				new Metric { 
					Type = Metrics.CpuLoad.AsInt(), 
					Source = "existing-source",  
					CurrentValue = 10
				}
			);
			await context.SaveChangesAsync();
			
			var metricService = new MetricService(context, new Mock<ILogger<MetricService>>().Object);

			// Act
			var metric = await metricService.GetOrCreateMetricAsync(Metrics.CpuLoad, "existing-source");

			// Assert
			Assert.NotNull(metric);
			Assert.Equal(Metrics.CpuLoad.AsInt(), metric.Type);
			Assert.Equal("existing-source", metric.Source);
			Assert.Equal(10, metric.CurrentValue);
			Assert.True(
				await context.Metrics.AnyAsync(
					mt => mt.Type == Metrics.CpuLoad.AsInt() && mt.Source == "existing-source"
				)
			);
		}

		[Fact]
		public async Task GetsCurrentValue()
		{
			// Arrange
			var context = _serviceProvider.GetRequiredService<IDataContext>();
			var metric = await context.Metrics.AddAsync(
				new Metric { 
					Type = Metrics.CpuLoad.AsInt(), 
					Source = "existing-source"
				}
			);
			await context.NumericDataPoints.AddRangeAsync(
				new List<NumericDataPoint> {
					new NumericDataPoint { 
						Timestamp = DateTime.UtcNow, 
						Value = 10, 
						Metric = metric.Entity 
					},
					new NumericDataPoint { 
						Timestamp = DateTime.UtcNow - new TimeSpan(1, 0, 0), 
						Value = 10, 
						Metric = metric.Entity 
					}
				}
			);
			await context.SaveChangesAsync();
			
			var metricService = new MetricService(context, new Mock<ILogger<MetricService>>().Object);

			// Act
			var value = await metricService.GetCurrentValueForMetricAsync(metric.Entity);

			// Assert
			Assert.Equal(10, value);
		}
	}

	/// <summary>
	/// Helper classes which compares two Metric objects
	/// </summary>
	public class MetricComparer : IEqualityComparer<Metric>
	{
		/// <summary>
		/// Returns true if two given Metric objects should be considered equal
		/// </summary>
		public bool Equals(Metric x, Metric y)
		{
			return
				x.Source == y.Source &&
				x.Type == y.Type &&
				x.Title == y.Title &&
				x.Public == y.Public &&

				x.DayAvg == y.DayAvg &&
				x.DayMax == y.DayMax &&
				x.DayMin == y.DayMin &&

				x.HourAvg == y.HourAvg &&
				x.HourMax == y.HourMax &&
				x.HourMin == y.HourMin &&

				x.CurrentValue == y.CurrentValue &&
				
				(x.AutoLabel == null && y.AutoLabel == null) || 
				x.AutoLabel.Id == y.AutoLabel.Id &&

				(x.ManualLabel == null && y.ManualLabel == null || 
				x.ManualLabel.Id == y.ManualLabel.Id);
		}

		/// <summary>
		/// Returns a hash of the given Metric object
		/// </summary>
		public int GetHashCode(Metric metric)
		{
			return
				2 * metric.Source.GetHashCode() +
				3 * metric.Type.GetHashCode() +
				5 * metric.Title.GetHashCode() +
				7 * metric.Public.GetHashCode() +

				11 * metric.DayAvg.GetHashCode() +
				13 * metric.DayMax.GetHashCode() +
				17 * metric.DayMin.GetHashCode() +

				23 * metric.HourAvg.GetHashCode() +
				29 * metric.HourMax.GetHashCode() +
				31 * metric.HourMin.GetHashCode() +

				37 * metric.CurrentValue.GetHashCode() +
				
				41 * (metric.AutoLabel == null ? 0 : metric.AutoLabel.Id) +
				43 * (metric.AutoLabel == null ? 0 : metric.ManualLabel.Id);
		}
	}
}
