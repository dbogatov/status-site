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
	public partial class AdminControllerTest
	{
		[Fact]
		public async Task CleanFailure()
		{
			// Act
			var result = await _controller.Clean(null);

			// Assert
			Assert.IsType<BadRequestObjectResult>(result);
		}

		[Fact]
		public async Task CleanSuccess()
		{
			// Act
			var result = await _controller.Clean(300);

			// Assert
			var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);

			Assert.Equal("Index", redirectToActionResult.ActionName);

			_mockCleanService
				.Verify(clean => clean.CleanDataPointsAsync(It.IsAny<TimeSpan?>()));
		}
	}
}
