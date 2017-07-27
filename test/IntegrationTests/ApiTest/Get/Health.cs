using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using StatusMonitor.Shared.Models.Entities;
using Xunit;

namespace StatusMonitor.Tests.IntegrationTests
{
	public partial class ApiControllerTest
	{
		[Fact]
		public async Task HealthNoData()
		{
			// Arrange
			var _url = "/api/health";

			// Act
			var noContent = await _client.GetAsync(_url);

			// Assert
			Assert.Equal(HttpStatusCode.NoContent, noContent.StatusCode);
		}
	}
}
