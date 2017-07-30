using System.Collections.Generic;
using StatusMonitor.Shared.Models.Entities;
using StatusMonitor.Shared.Services;
using StatusMonitor.Web.Services;
using Xunit;

namespace StatusMonitor.Tests.UnitTests.Services
{
	public class BadgeServiceTest
	{
		[Fact]
		public void ProducesHealthBadgeNoData()
		{
			// Arrange
			var badgeService = new BadgeService();

			// Act
			var badge = badgeService.GetSystemHealthBadge(new HealthReport());

			// Assert
			Assert.Equal(BadgeStatus.Failure, badge.Status);
			Assert.NotEqual(0, badge.TitleWidth);
			Assert.NotEqual(0, badge.MessageWidth);
			Assert.Contains(0.ToString(), badge.Message);
			Assert.Equal("System health".ToLower(), badge.Title.ToLower());
		}

		[Theory]
		[InlineData(BadgeStatus.Success)]
		[InlineData(BadgeStatus.Neutural)]
		[InlineData(BadgeStatus.Failure)]
		public void ProducesHealthBadge(BadgeStatus status)
		{
			// Arrange
			var badgeService = new BadgeService();

			IEnumerable<HealthReportDataPoint> dataPoints = null;

			switch (status)
			{
				case BadgeStatus.Success:
					dataPoints = new List<HealthReportDataPoint> { // 94
						new HealthReportDataPoint { MetricLabel = AutoLabels.Normal },
						new HealthReportDataPoint { MetricLabel = AutoLabels.Normal },
						new HealthReportDataPoint { MetricLabel = AutoLabels.Normal },
						new HealthReportDataPoint { MetricLabel = AutoLabels.Normal },
						new HealthReportDataPoint { MetricLabel = AutoLabels.Normal },
						new HealthReportDataPoint { MetricLabel = AutoLabels.Normal },
						new HealthReportDataPoint { MetricLabel = AutoLabels.Normal },
						new HealthReportDataPoint { MetricLabel = AutoLabels.Warning }
					};
					break;
				case BadgeStatus.Neutural:
					dataPoints = new List<HealthReportDataPoint> { // 81
						new HealthReportDataPoint { MetricLabel = AutoLabels.Normal },
						new HealthReportDataPoint { MetricLabel = AutoLabels.Normal },
						new HealthReportDataPoint { MetricLabel = AutoLabels.Normal },
						new HealthReportDataPoint { MetricLabel = AutoLabels.Normal },
						new HealthReportDataPoint { MetricLabel = AutoLabels.Normal },
						new HealthReportDataPoint { MetricLabel = AutoLabels.Normal },
						new HealthReportDataPoint { MetricLabel = AutoLabels.Critical },
						new HealthReportDataPoint { MetricLabel = AutoLabels.Warning }
					};
					break;
				case BadgeStatus.Failure:
					dataPoints = new List<HealthReportDataPoint> { // 56
						new HealthReportDataPoint { MetricLabel = AutoLabels.Normal },
						new HealthReportDataPoint { MetricLabel = AutoLabels.Normal },
						new HealthReportDataPoint { MetricLabel = AutoLabels.Critical },
						new HealthReportDataPoint { MetricLabel = AutoLabels.Normal },
						new HealthReportDataPoint { MetricLabel = AutoLabels.Critical },
						new HealthReportDataPoint { MetricLabel = AutoLabels.Normal },
						new HealthReportDataPoint { MetricLabel = AutoLabels.Critical },
						new HealthReportDataPoint { MetricLabel = AutoLabels.Warning }
					};
					break;
			}

			var report = new HealthReport { Data = dataPoints };

			// Act
			var badge = badgeService.GetSystemHealthBadge(report);

			// Assert
			Assert.Equal(status, badge.Status);
			Assert.NotEqual(0, badge.TitleWidth);
			Assert.NotEqual(0, badge.MessageWidth);
			switch (status)
			{
				case BadgeStatus.Success:
					Assert.Contains(94.ToString(), badge.Message);
					break;
				case BadgeStatus.Neutural:
					Assert.Contains(81.ToString(), badge.Message);
					break;
				case BadgeStatus.Failure:
					Assert.Contains(56.ToString(), badge.Message);
					break;
			}
			Assert.Equal("System health".ToLower(), badge.Title.ToLower());
		}
	}
}
