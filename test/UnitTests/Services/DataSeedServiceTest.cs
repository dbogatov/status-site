using System;
using Xunit;
using StatusMonitor.Shared.Models.Entities;
using StatusMonitor.Shared.Services;
using StatusMonitor.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using StatusMonitor.Shared.Extensions;
using Moq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace StatusMonitor.Tests.UnitTests.Services
{
	public class DataSeedServiceTest
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly IConfiguration _config;
		private readonly IDataContext _dataContext;

		public DataSeedServiceTest()
		{
			var services = new ServiceCollection();

			var mockEnv = new Mock<IHostingEnvironment>();
			mockEnv
				.SetupGet(environment => environment.EnvironmentName)
				.Returns("Testing");
			var env = mockEnv.Object;

			var mockConfig = new Mock<IConfiguration>();
			mockConfig
				.SetupGet(config => config[It.IsAny<string>()])
				.Returns("test-title");
			mockConfig
				.SetupGet(config => config["Data:PingSettings"])
				.Returns("");
			_config = mockConfig.Object;

			services.RegisterSharedServices(env, _config);

			_serviceProvider = services.BuildServiceProvider();

			_dataContext = _serviceProvider.GetRequiredService<IDataContext>();
		}

		[Fact]
		public void InitiallyNoValues()
		{
			// Assert
			Assert.Empty(_dataContext.AutoLabels);
			Assert.Empty(_dataContext.ManualLabels);
			Assert.Empty(_dataContext.CompilationStages);
			Assert.Empty(_dataContext.LogEntrySeverities);
			Assert.Empty(_dataContext.Metrics);
			Assert.Empty(_dataContext.AbstractMetrics);
		}

		[Fact]
		public async Task ServiceSeedsValuesToDataProvider()
		{
			// Arrange
			var dataSeedService = new DataSeedService(
				_dataContext,
				new Mock<ILogger<DataSeedService>>().Object,
				_config
			);

			// Act
			await dataSeedService.SeedDataAsync();

			// Assert
			Assert.NotEmpty(_dataContext.AutoLabels);
			Assert.NotEmpty(_dataContext.ManualLabels);
			Assert.NotEmpty(_dataContext.CompilationStages);
			Assert.NotEmpty(_dataContext.LogEntrySeverities);
			Assert.NotEmpty(_dataContext.AbstractMetrics);
		}

		[Fact]
		public async Task ProperlyUpdatesData()
		{
			// Arrange
			var dataSeedService = new DataSeedService(
				_dataContext,
				new Mock<ILogger<DataSeedService>>().Object,
				_config
			);

			await _dataContext.CompilationStages.AddRangeAsync(new List<CompilationStage> {
				new CompilationStage { Id = CompilationStages.M4.AsInt(), Name = "M4 stage" },
				new CompilationStage { Id = CompilationStages.SandPiper.AsInt(), Name = "SandPiper" }
			});

			// Act
			await dataSeedService.SeedDataAsync();

			// Assert
			Assert.Equal(
				Enum.GetNames(typeof(CompilationStages)).Count(),
				await _dataContext.CompilationStages.CountAsync()
			);
		}

		[Fact]
		public async Task NoDuplicates()
		{
			// Arrange
			var dataSeedService = new DataSeedService(
				_dataContext,
				new Mock<ILogger<DataSeedService>>().Object,
				_config
			);

			// Act
			await dataSeedService.SeedDataAsync();
			await dataSeedService.SeedDataAsync();

			// Assert
			Assert.Equal(
				Enum.GetNames(typeof(CompilationStages)).Count(),
				await _dataContext.CompilationStages.CountAsync()
			);
		}
	}
}
