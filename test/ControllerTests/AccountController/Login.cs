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

namespace StatusMonitor.Tests.ControllerTests
{
	public partial class AccountControllerTest
	{
		[Fact]
		public void Login()
		{
			// Act
			var result = _controller.Login();

			// Assert
			var viewResult = Assert.IsType<ViewResult>(result);

			var model = Assert.IsAssignableFrom<ReturnUrlViewModel>(
				viewResult.Model
			);

			Assert.False(model.IsError);
			Assert.Equal("/return", model.ReturnUrl);
		}
	}
}
