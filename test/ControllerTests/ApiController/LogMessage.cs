using System.Threading.Tasks;
using Moq;
using StatusMonitor.Shared.Models.Entities;
using StatusMonitor.Web.ViewModels;
using Xunit;

namespace StatusMonitor.Tests.ControllerTests
{
	public partial class ApiControllerTest
	{
		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task LogMessageSchedulesNotification(bool severityEnabled)
		{
			// Arrange
			_mockConfig
				.SetupGet(conf => conf["Guard:Logging:Requests"])
				.Returns(100.ToString());
			_mockConfig
				.SetupGet(conf => conf["Guard:Logging:PerSeconds"])
				.Returns(100.ToString());
			_mockConfig
				.SetupGet(conf => conf["Logging:LogSeverityReported"])
				.Returns((severityEnabled ? LogEntrySeverities.Warn : LogEntrySeverities.Error).ToString());

			// Act
			await _controller.LogMessage(
				new LogMessageViewModel
				{
					MessageSeverity = LogEntrySeverities.Warn,
					Source = "the-source"
				}
			);
		
			// Assert
			_mockNotify
				.Verify(
					notif => notif.ScheduleNotificationAsync(
						It.Is<string>(msg => msg.Contains("the-source")),
						NotificationSeverity.High
					),
					severityEnabled ? Times.Once() : Times.Never()
				);
		}
	}
}
