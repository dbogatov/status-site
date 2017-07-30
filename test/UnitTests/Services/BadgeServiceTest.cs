using System.Collections.Generic;
using Moq;
using StatusMonitor.Shared.Models.Entities;
using StatusMonitor.Shared.Services;
using StatusMonitor.Web.Services;
using Xunit;

namespace StatusMonitor.Tests.UnitTests.Services
{
	public class BadgeServiceTest
	{
		[Theory]
		[InlineData(BadgeStatus.Success)]
		[InlineData(BadgeStatus.Neutural)]
		[InlineData(BadgeStatus.Failure)]
		public void ProducesMetricHealthBadge(BadgeStatus status)
		{
			// Arrange
			var badgeService = new BadgeService();
			var label = 
				status == BadgeStatus.Success ?
				AutoLabels.Normal :
				(
					status == BadgeStatus.Neutural ?
					AutoLabels.Warning :
					AutoLabels.Critical
				);

			// Act
			var badge = badgeService
				.GetMetricHealthBadge(
					"the-source",
					Metrics.CpuLoad,
					label
				);

			// Assert
			Assert.Equal(status, badge.Status);
			Assert.NotEqual(0, badge.TitleWidth);
			Assert.NotEqual(0, badge.MessageWidth);
			Assert.Contains("the-source", badge.Title.ToLower());
			Assert.Contains(Metrics.CpuLoad.ToString().ToLower(), badge.Title.ToLower());
			Assert.Contains(label.ToString().ToLower(), badge.Message.ToLower());
		}

		[Fact]
		public void ProducesSystemHealthBadgeNoData()
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
		public void ProducesSystemHealthBadge(BadgeStatus status)
		{
			// Arrange
			var badgeService = new BadgeService();

			var report = new Mock<HealthReport>();
			report
				.SetupGet(rep => rep.Health)
				.Returns(status == BadgeStatus.Success ? 94 : (status == BadgeStatus.Neutural ? 81 : 56));

			// Act
			var badge = badgeService.GetSystemHealthBadge(report.Object);

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
