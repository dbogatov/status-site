using System;
using Xunit;
using StatusMonitor.Shared.Models.Entities;
using StatusMonitor.Shared.Services;
using StatusMonitor.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using StatusMonitor.Shared.Extensions;
using Newtonsoft.Json;
using Moq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Collections.Generic;

namespace StatusMonitor.Tests.UnitTests.Services
{
	public class LoggingServiceTest
	{
		private readonly IDataContext _context;
		private readonly ILoggingService _loggingService;

		public LoggingServiceTest()
		{
			var services = new ServiceCollection();

			var mockEnv = new Mock<IHostingEnvironment>();
			mockEnv
				.SetupGet(environment => environment.EnvironmentName)
				.Returns("Testing");
			var env = mockEnv.Object;

			services.RegisterSharedServices(env, new Mock<IConfiguration>().Object);

			var serviceProvider = services.BuildServiceProvider();

			_context = serviceProvider.GetRequiredService<IDataContext>();
			_context.LogEntrySeverities.AddRange(
				Enum
					.GetValues(typeof(LogEntrySeverities))
					.Cast<LogEntrySeverities>()
					.Select(e => new LogEntrySeverity
					{
						Id = e.AsInt(),
						Description = e.ToString()
					})
			);
			_context.SaveChanges();

			_loggingService = new LoggingService(_context);
		}

		[Fact]
		public async Task RecordsMessage()
		{
			// Act
			var id = await _loggingService.RecordLogMessageAsync(
				"the message",
				JsonConvert.SerializeObject(new
				{
					Exception = "exception stack trace"
				}),
				"the-source",
				LoggingEvents.Ping.AsInt(),
				LogEntrySeverities.Error
			);

			// Assert
			Assert.True(await _context.LogEntries.AnyAsync(log => log.Id == id));
		}

		[Fact]
		public async Task GetsAvailableFilterData()
		{
			// Arrange
			var twoDaysAgo = DateTime.UtcNow.AddDays(-2);
			var hourAgo = DateTime.UtcNow.AddHours(-1);
			var now = DateTime.UtcNow;

			await _context.LogEntries.AddRangeAsync(
				new List<LogEntry> {
					new LogEntry {
						Severity = new LogEntrySeverity { Id = LogEntrySeverities.Debug.AsInt() },
						Category = LoggingEvents.Clean.AsInt(),
						Source = "the-source-1",
						Timestamp = twoDaysAgo
					},
					new LogEntry {
						Severity = new LogEntrySeverity { Id = LogEntrySeverities.Info.AsInt() },
						Category = LoggingEvents.Clean.AsInt(),
						Source = "the-source-2",
						Timestamp = twoDaysAgo
					},
					new LogEntry {
						Severity = new LogEntrySeverity { Id = LogEntrySeverities.Debug.AsInt() },
						Category = LoggingEvents.Clean.AsInt(),
						Source = "the-source-1",
						Timestamp = hourAgo
					},
					new LogEntry {
						Severity = new LogEntrySeverity { Id = LogEntrySeverities.Error.AsInt() },
						Category = LoggingEvents.ActionFilters.AsInt(),
						Source = "the-source-1",
						Timestamp = now
					}
				}
			);

			await _context.SaveChangesAsync();

			var expected = new LogMessagesFilterModel
			{
				Severities = new List<LogEntrySeverities> {
					LogEntrySeverities.Debug,
					LogEntrySeverities.Info,
					LogEntrySeverities.Error
				},
				Sources = new List<string> {
					"the-source-1",
					"the-source-2"
				},
				Categories = new List<int> {
					LoggingEvents.Clean.AsInt(),
					LoggingEvents.ActionFilters.AsInt()
				},
				Start = twoDaysAgo,
				End = now
			};

			// Act
			var actual = await _loggingService.GetAvailableFilterDataAsync();

			// Assert
			Assert.Equal(expected.Categories, actual.Categories);
			Assert.Equal(expected.Severities, actual.Severities);
			Assert.Equal(expected.Sources, actual.Sources);
			Assert.Equal(expected.Start, actual.Start);
			Assert.Equal(expected.End, actual.End);
		}

		[Fact]
		public async Task GetsMessageById()
		{
			// Arrange
			var entry = await _context.LogEntries.AddAsync(
				new LogEntry
				{
					Severity = new LogEntrySeverity { Id = LogEntrySeverities.Debug.AsInt() }
				}
			);
			await _context.SaveChangesAsync();

			// Act
			var existing = await _loggingService.GetMessageByIdAsync(entry.Entity.Id);
			var nonExisting = await _loggingService.GetMessageByIdAsync(-1);

			// Assert
			Assert.NotNull(existing);
			Assert.Equal(entry.Entity.Id, existing.Id);
			Assert.NotNull(existing.Severity);

			Assert.Null(nonExisting);
		}

		[Fact]
		public async Task GetsLogMessages()
		{
			// Arrange
			var twoDaysAgo = DateTime.UtcNow.AddDays(-2);
			var hourAgo = DateTime.UtcNow.AddHours(-1);
			var now = DateTime.UtcNow;

			var logEntries = new List<LogEntry> {
				new LogEntry {
					Severity = new LogEntrySeverity { Id = LogEntrySeverities.Debug.AsInt() },
					Category = LoggingEvents.Clean.AsInt(),
					Source = "the-source-1",
					Timestamp = twoDaysAgo
				},
				new LogEntry {
					Severity = new LogEntrySeverity { Id = LogEntrySeverities.Info.AsInt() },
					Category = LoggingEvents.Clean.AsInt(),
					Source = "the-source-2",
					Timestamp = twoDaysAgo
				},
				new LogEntry {
					Severity = new LogEntrySeverity { Id = LogEntrySeverities.Debug.AsInt() },
					Category = LoggingEvents.Clean.AsInt(),
					Source = "the-source-1",
					Timestamp = hourAgo
				},
				new LogEntry {
					Severity = new LogEntrySeverity { Id = LogEntrySeverities.Error.AsInt() },
					Category = LoggingEvents.ActionFilters.AsInt(),
					Source = "the-source-2",
					Timestamp = now
				},
				new LogEntry {
					Severity = new LogEntrySeverity { Id = LogEntrySeverities.Error.AsInt() },
					Category = LoggingEvents.Cache.AsInt(),
					Source = "the-source-3",
					Timestamp = hourAgo
				},
				new LogEntry {
					Severity = new LogEntrySeverity { Id = LogEntrySeverities.Detail.AsInt() },
					Category = LoggingEvents.ActionFilters.AsInt(),
					Source = "the-source-2",
					Timestamp = now
				}
			};
			await _context.LogEntries.AddRangeAsync(logEntries);

			await _context.SaveChangesAsync();

			var expectedBySource = // the-source 2
				new List<LogEntry>
				{
					logEntries[1],
					logEntries[3],
					logEntries[5]
				};

			var expectedByCategory = // ActionFilters
				new List<LogEntry>
				{
					logEntries[3],
					logEntries[5]
				};

			var expectedBySeverity = // Debug
				new List<LogEntry>
				{
					logEntries[0],
					logEntries[2]
				};

			var expectedByStart = // hourAgo
				new List<LogEntry>
				{
					logEntries[2],
					logEntries[3],
					logEntries[4],
					logEntries[5]
				};

			var expectedByEnd = // hourAgo
				new List<LogEntry>
				{
					logEntries[0],
					logEntries[1],
					logEntries[2],
					logEntries[4]
				};

			var expectedByCombined =
				new List<LogEntry>
				{
					logEntries[2]
				};

			var expectedEmpty = new List<LogEntry>();
			var expectedAll = logEntries;

			// Act
			var actualBySource = await _loggingService.GetLogMessagesAsync(
				new LogMessagesFilterModel
				{
					Sources = new List<string> { "the-source-2" }
				}
			);

			var actualByCategory = await _loggingService.GetLogMessagesAsync(
				new LogMessagesFilterModel
				{
					Categories = new List<int> { LoggingEvents.ActionFilters.AsInt() }
				}
			);

			var actualBySeverity = await _loggingService.GetLogMessagesAsync(
				new LogMessagesFilterModel
				{
					Severities = new List<LogEntrySeverities> { LogEntrySeverities.Debug }
				}
			);

			var actualByStart = await _loggingService.GetLogMessagesAsync(
				new LogMessagesFilterModel
				{
					Start = hourAgo
				}
			);

			var actualByEnd = await _loggingService.GetLogMessagesAsync(
				new LogMessagesFilterModel
				{
					End = hourAgo
				}
			);

			var actualEmpty = await _loggingService.GetLogMessagesAsync(
				new LogMessagesFilterModel
				{
					Severities = new List<LogEntrySeverities> { LogEntrySeverities.Fatal }
				}
			);

			var actualAll = await _loggingService.GetLogMessagesAsync(new LogMessagesFilterModel());

			// Assert
			Assert.Equal(
				expectedBySource.OrderBy(e => e.Id),
				actualBySource.OrderBy(e => e.Id)
			);
			Assert.Equal(
				expectedByCategory.OrderBy(e => e.Id),
				actualByCategory.OrderBy(e => e.Id)
			);
			Assert.Equal(
				expectedBySeverity.OrderBy(e => e.Id),
				actualBySeverity.OrderBy(e => e.Id)
			);
			Assert.Equal(
				expectedByStart.OrderBy(e => e.Id),
				actualByStart.OrderBy(e => e.Id)
			);
			Assert.Equal(
				expectedByEnd.OrderBy(e => e.Id),
				actualByEnd.OrderBy(e => e.Id)
			);
			Assert.Equal(
				expectedEmpty.OrderBy(e => e.Id),
				actualEmpty.OrderBy(e => e.Id)
			);
			Assert.Equal(
				expectedAll.OrderBy(e => e.Id),
				actualAll.OrderBy(e => e.Id)
			);
		}

		[Fact]
		public async Task GetsAvailableFilterDataForEmpty()
		{
			// Arrange
			var expected = new LogMessagesFilterModel();

			// Act
			var actual = await _loggingService.GetAvailableFilterDataAsync();

			// Assert
			Assert.Equal(expected.Categories, actual.Categories);
			Assert.Equal(expected.Severities, actual.Severities);
			Assert.Equal(expected.Sources, actual.Sources);
			Assert.Equal(expected.Start, actual.Start);
			Assert.Equal(expected.End, actual.End);
		}

		[Fact]
		public async Task GetsLogMessagesForEmpty()
		{
			// Act
			var actual = await _loggingService.GetLogMessagesAsync(new LogMessagesFilterModel());

			// Assert
			Assert.Empty(actual);
		}
	}
}
