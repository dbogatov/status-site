using System;
using Xunit;

namespace StatusMonitor.Tests.UnitTests
{
	public class EnvironmentTest
	{
		[Fact]
		public void TestingEnvironmentSet()
		{
			var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

			Assert.Equal("Testing", env);
		}
	}
}
