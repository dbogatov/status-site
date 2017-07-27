using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using StatusMonitor.Web.Controllers.View;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models.Entities;
using StatusMonitor.Shared.Services;
using Xunit;
using Microsoft.AspNetCore.Http;
using Moq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StatusMonitor.Shared.Models;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using StatusMonitor.Web.Services;
using System.Linq;

namespace StatusMonitor.Tests.ControllerTests
{
	public partial class HomeControllerTest
	{
		[Fact]
		public async Task Health()
		{
			// Arrange
			await _context.HealthReports.AddAsync(new HealthReport());
			await _context.SaveChangesAsync();

			var badge = new Badge {
				Title = "System health",
				Message = "95%",
				Status = BadgeStatus.Success,
				TitleWidth = 100,
				MessageWidth = 40
			};

			_mockBadge
				.Setup(mock => mock.GetHealthBadge(It.IsAny<HealthReport>()))
				.Returns(badge);

			// Act
			var result = await _controller.Health();

			// Assert
			var badgeResult = Assert.IsType<BadgeResult>(result);

			var model = Assert.IsAssignableFrom<Badge>(
				badgeResult.Badge
			);

			Assert.Equal(badge, model);
		}

		[Fact]
		public async Task HealthNoData()
		{
			// Act
			var result = await _controller.Health();

			// Assert
			var noContentResult = Assert.IsType<NoContentResult>(result);
		}
	}
}
