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
using StatusMonitor.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace StatusMonitor.Tests.ControllerTests
{
	public partial class AdminControllerTest
	{
		[Fact]
		public async Task ResolveDiscrepancySuccess()
		{
			// Arrange
			var now = DateTime.UtcNow;

			await _context.Discrepancies.AddAsync(
				new Discrepancy
				{
					Type = DiscrepancyType.GapInData,
					MetricType = Metrics.CpuLoad,
					MetricSource = "the-source",
					DateFirstOffense = now,
					Resolved = false
				}
			);
			await _context.SaveChangesAsync();

			var input = new DiscrepancyResolutionViewModel
			{
				DateFirstOffense = now,
				EnumDiscrepancyType = DiscrepancyType.GapInData,
				EnumMetricType = Metrics.CpuLoad,
				Source = "the-source"
			};

			// Act
			var result = await _controller.ResolveDiscrepancy(input);

			// Assert
			var redirectResult = Assert.IsType<RedirectToActionResult>(result);

			Assert.Equal("Index", redirectResult.ActionName);
			Assert.Equal("Admin", redirectResult.ControllerName);

			Assert.True(
				(await _context
					.Discrepancies
					.SingleAsync(d =>
						d.DateFirstOffense == now &&
						d.MetricSource == "the-source" &&
						d.MetricType == Metrics.CpuLoad &&
						d.Type == DiscrepancyType.GapInData
					)
				).Resolved
			);
			Assert.InRange(
				(await _context
					.Discrepancies
					.SingleAsync(d =>
						d.DateFirstOffense == now &&
						d.MetricSource == "the-source" &&
						d.MetricType == Metrics.CpuLoad &&
						d.Type == DiscrepancyType.GapInData
					)
				).DateResolved,
				now,
				now.AddMinutes(1)
			);

			_mockNotificationService
				.Verify(
					n => n.ScheduleNotificationAsync(
						It.Is<string>(s => s.Contains("the-source")),
						NotificationSeverity.Medium
					)
				);

			_mockConfig.Verify(conf => conf["ServiceManager:NotificationService:TimeZone"]);
		}

		[Fact]
		public async Task ResolveDiscrepancyWarning()
		{
			// Arrange
			var now = DateTime.UtcNow;

			await _context.Discrepancies.AddAsync(
				new Discrepancy
				{
					Type = DiscrepancyType.GapInData,
					MetricType = Metrics.CpuLoad,
					MetricSource = "the-source",
					DateFirstOffense = now,
					Resolved = true
				}
			);
			await _context.SaveChangesAsync();

			var input = new DiscrepancyResolutionViewModel
			{
				DateFirstOffense = now,
				EnumDiscrepancyType = DiscrepancyType.GapInData,
				EnumMetricType = Metrics.CpuLoad,
				Source = "the-source"
			};

			// Act
			var result = await _controller.ResolveDiscrepancy(input);

			// Assert
			var redirectResult = Assert.IsType<RedirectToActionResult>(result);

			Assert.Equal("Index", redirectResult.ActionName);
			Assert.Equal("Admin", redirectResult.ControllerName);

			_mockNotificationService
				.Verify(
					n => n.ScheduleNotificationAsync(
						It.Is<string>(s => s.Contains("the-source")),
						NotificationSeverity.Medium
					),
					Times.Never()
				);
		}

		[Fact]
		public async Task ResolveDiscrepancyNotFound()
		{
			// Arrange
			var input = new DiscrepancyResolutionViewModel
			{
				DateFirstOffense = DateTime.UtcNow,
				EnumDiscrepancyType = DiscrepancyType.GapInData,
				EnumMetricType = Metrics.CpuLoad,
				Source = "the-source"
			};

			// Act
			var result = await _controller.ResolveDiscrepancy(input);

			// Assert
			var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);

			_mockNotificationService
				.Verify(
					n => n.ScheduleNotificationAsync(
						It.Is<string>(s => s.Contains("the-source")),
						NotificationSeverity.Medium
					),
					Times.Never()
				);
		}
	}
}
