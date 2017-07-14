using System;
using Xunit;
using StatusMonitor.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using StatusMonitor.Daemons.Services;
using Moq;
using Microsoft.AspNetCore.Hosting;
using StatusMonitor.Shared.Extensions;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using StatusMonitor.Shared.Models.Entities;
using Microsoft.Extensions.Logging;
using StatusMonitor.Shared.Services;
using System.Linq;

namespace StatusMonitor.Tests.UnitTests.Services
{
	public class NotificationServiceTest
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly IConfiguration _config;

		public NotificationServiceTest()
		{
			var services = new ServiceCollection();

			var mockEnv = new Mock<IHostingEnvironment>();
			mockEnv
				.SetupGet(environment => environment.EnvironmentName)
				.Returns("Testing");
			var env = mockEnv.Object;

			var mockConfig = new Mock<IConfiguration>();
			mockConfig
				.SetupGet(
					config => config[$"ServiceManager:NotificationService:Frequencies:{NotificationSeverity.High.ToString()}"]
				)
				.Returns($"{60}");
			mockConfig
				.SetupGet(
					config => config[$"ServiceManager:NotificationService:Frequencies:{NotificationSeverity.Medium.ToString()}"]
				)
				.Returns($"{60 * 60}");
			mockConfig
				.SetupGet(
					config => config[$"ServiceManager:NotificationService:Frequencies:{NotificationSeverity.Low.ToString()}"]
				)
				.Returns($"{24 * 60 * 60}");
			_config = mockConfig.Object;

			services.RegisterSharedServices(env, new Mock<IConfiguration>().Object);

			_serviceProvider = services.BuildServiceProvider();
		}

		[Fact]
		public async Task ChecksIfNeedToSend_OnlySentNotifications()
		{
			// Arrange
			var context = _serviceProvider.GetRequiredService<IDataContext>();
			await context.Notifications.AddAsync(new Notification());
			await context.SaveChangesAsync();

			var notificationService = new NotificationService(
				new Mock<ILogger<NotificationService>>().Object,
				new Mock<IConfiguration>().Object,
				context,
				new Mock<IEmailService>().Object,
				new Mock<ISlackService>().Object
			);

			// Act
			var actual = Enum
				.GetValues(typeof(NotificationSeverity))
				.Cast<object>()
				.Select(async severity => await notificationService.CheckIfNeedToSendAsync((NotificationSeverity)severity))
				.Select(task => task.Result)
				.Aggregate((self, next) => self && next);

			// Assert
			Assert.True(actual);
		}

		[Fact]
		public void ChecksIfNeedToSend_NoNotifications()
		{
			// Arrange
			var context = _serviceProvider.GetRequiredService<IDataContext>();

			var notificationService = new NotificationService(
				new Mock<ILogger<NotificationService>>().Object,
				new Mock<IConfiguration>().Object,
				context,
				new Mock<IEmailService>().Object,
				new Mock<ISlackService>().Object
			);

			// Act
			var actual = Enum
				.GetValues(typeof(NotificationSeverity))
				.Cast<object>()
				.Select(async severity => await notificationService.CheckIfNeedToSendAsync((NotificationSeverity)severity))
				.Select(task => task.Result)
				.Aggregate((self, next) => self && next);

			// Assert
			Assert.True(actual);
		}

		[Fact]
		public async Task ChecksIfNeedToSend_OldNotifications()
		{
			// Arrange
			var context = _serviceProvider.GetRequiredService<IDataContext>();

			await context.Notifications.AddAsync(
				new Notification
				{
					IsSent = true,
					DateSent = DateTime.UtcNow.AddMinutes(-2),
					Severity = NotificationSeverity.High
				}
			);
			await context.SaveChangesAsync();

			var notificationService = new NotificationService(
				new Mock<ILogger<NotificationService>>().Object,
				_config,
				context,
				new Mock<IEmailService>().Object,
				new Mock<ISlackService>().Object
			);

			// Act
			var actual = await notificationService.CheckIfNeedToSendAsync(NotificationSeverity.High);

			// Assert
			Assert.True(actual);
		}

		[Fact]
		public async Task ChecksIfNeedToSend_TooRecentNotification()
		{
			// Arrange
			var context = _serviceProvider.GetRequiredService<IDataContext>();

			await context.Notifications.AddAsync(
				new Notification
				{
					IsSent = true,
					DateSent = DateTime.UtcNow.AddSeconds(-30),
					Severity = NotificationSeverity.High
				}
			);
			await context.SaveChangesAsync();

			var notificationService = new NotificationService(
				new Mock<ILogger<NotificationService>>().Object,
				_config,
				context,
				new Mock<IEmailService>().Object,
				new Mock<ISlackService>().Object
			);

			// Act
			var actual = await notificationService.CheckIfNeedToSendAsync(NotificationSeverity.High);

			// Assert
			Assert.False(actual);
		}

		[Theory]
		[InlineData(NotificationSeverity.High)]
		[InlineData(NotificationSeverity.Medium)]
		[InlineData(NotificationSeverity.Low)]
		public void ComposesMessage(NotificationSeverity severity)
		{
			// Arrange
			var input = new List<Notification> {
				new Notification
				{
					IsSent = true,
					DateCreated = DateTime.UtcNow.AddSeconds(-30),
					Severity = severity,
					Message = "Hello, world!"
				}
			};

			var notificationService = new NotificationService(
				new Mock<ILogger<NotificationService>>().Object,
				new Mock<IConfiguration>().Object,
				new Mock<IDataContext>().Object,
				new Mock<IEmailService>().Object,
				new Mock<ISlackService>().Object
			);

			// Act
			var actual = notificationService.ComposeMessage(input);

			// Assert
			Assert.Contains($"Severity {severity}", actual);
		}

		[Fact]
		public async Task SchedulesNotification()
		{
			// Arrange
			var context = _serviceProvider.GetRequiredService<IDataContext>();

			var notificationService = new NotificationService(
				new Mock<ILogger<NotificationService>>().Object,
				new Mock<IConfiguration>().Object,
				context,
				new Mock<IEmailService>().Object,
				new Mock<ISlackService>().Object
			);

			// Act
			var actual = await notificationService
				.ScheduleNotificationAsync(
					"Hello, world",
					NotificationSeverity.High
				);

			// Assert
			Assert.True(await context.Notifications.AnyAsync(notif => notif.Id == actual.Id));
			Assert.Equal(NotificationSeverity.High, actual.Severity);
			Assert.Equal("Hello, world", actual.Message);
			Assert.False(actual.IsSent);
		}

		[Fact]
		public async Task ProcessesNotificationQueue()
		{
			// Arrange
			var context = _serviceProvider.GetRequiredService<IDataContext>();
			await context.Notifications.AddRangeAsync(
				new List<Notification> {
					new Notification {
						Severity = NotificationSeverity.High
					},
					new Notification {
						Severity = NotificationSeverity.Medium,
						IsSent = true,
						DateSent = DateTime.UtcNow.AddHours(-2)
					},
					new Notification { // this one triggers sending
						Severity = NotificationSeverity.Medium,
						DateSent = DateTime.UtcNow.AddMinutes(-30)
					},
					new Notification {
						Severity = NotificationSeverity.Low
					},
					new Notification {
						Severity = NotificationSeverity.Low,
						IsSent = true,
						DateSent = DateTime.UtcNow
					},
				}
			);
			await context.SaveChangesAsync();

			var mockEmail = new Mock<IEmailService>();
			var mockSlack = new Mock<ISlackService>();

			var notificationService = new NotificationService(
				new Mock<ILogger<NotificationService>>().Object,
				_config,
				context,
				mockEmail.Object,
				mockSlack.Object
			);

			// Act
			var actual = await notificationService.ProcessNotificationQueueAsync();

			// Assert
			Assert.True(actual);

			mockEmail
				.Verify(
					email => email.SendEmailAsync(
						It.IsAny<string[]>(),
						It.Is<string>(subj => subj.Contains("notifications")),
						It.IsAny<string>()
					),
					Times.Once()
				);

			mockSlack
				.Verify(
					slack => slack.SendMessageAsync(It.IsAny<string>()),
					Times.Once()
				);
			
			Assert.True(context.Notifications.All(notif => notif.IsSent));
			Assert.True(context.Notifications.All(notif => notif.DateSent != null));
		}

		[Fact]
		public async Task ProcessesNotificationQueue_NoData()
		{
			// Arrange
			var context = _serviceProvider.GetRequiredService<IDataContext>();

			var mockEmail = new Mock<IEmailService>();
			var mockSlack = new Mock<ISlackService>();

			var notificationService = new NotificationService(
				new Mock<ILogger<NotificationService>>().Object,
				_config,
				context,
				mockEmail.Object,
				mockSlack.Object
			);

			// Act
			var actual = await notificationService.ProcessNotificationQueueAsync();

			// Assert
			Assert.False(actual);

			mockEmail
				.Verify(
					email => email.SendEmailAsync(
						It.IsAny<string[]>(),
						It.Is<string>(subj => subj.Contains("notifications")),
						It.IsAny<string>()
					),
					Times.Never()
				);

			mockSlack
				.Verify(
					slack => slack.SendMessageAsync(It.IsAny<string>()),
					Times.Never()
				);
		}

		[Fact]
		public void UsesCorrectTimeZone()
		{
			// Arrange
			var config = new Mock<IConfiguration>();
			config
				.SetupGet(conf => conf["ServiceManager:NotificationService:TimeZone"])
				.Returns("Asia/Kabul");

			var notificationService = new NotificationService(
				new Mock<ILogger<NotificationService>>().Object,
				config.Object,
				new Mock<IDataContext>().Object,
				new Mock<IEmailService>().Object,
				new Mock<ISlackService>().Object
			);

			var date = DateTime.SpecifyKind(new DateTime(2017, 07, 14, 18, 25, 43), DateTimeKind.Utc);

			var input = new List<Notification> {
				new Notification {
					DateCreated = date,
					Message = $"The message.",
					Severity = NotificationSeverity.Medium
				}
			};
			var expected = date.ToStringUsingTimeZone("Asia/Kabul");

			// Act
			var actual = notificationService.ComposeMessage(input);

			// Assert
			Assert.Contains(expected, actual);
		}
	}
}
