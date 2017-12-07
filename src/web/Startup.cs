using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using StatusMonitor.Shared.Services;
using StatusMonitor.Shared.Models;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Web.ActionFilters;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.Tasks;
using System.Net;
using StatusMonitor.Shared.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using StatusMonitor.Web.Controllers.Api;
using StatusMonitor.Web.Controllers.View;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using StatusMonitor.Web.Services;
using StatusMonitor.Web.ViewModels;

namespace StatusMonitor.Web
{
	/// <summary>
	/// Framework class used to configure the application.
	/// </summary>
	public class Startup
	{
		/// <summary>
		/// This method gets called by the runtime. Used to build a global configuration object.
		/// </summary>
		/// <param name="env"></param>
		public Startup(IHostingEnvironment env)
		{
			var builder = new ConfigurationBuilder()
				.AddYamlFile("appsettings.yml", optional: false) // read defaults first
				.AddYamlFile(
					$"{(env.IsProduction() ? "/run/secrets/" : "")}appsettings.{env.EnvironmentName.ToLower()}.yml",
					optional: env.IsStaging()
				) // override with specific settings file
				.AddJsonFile("version.json", optional: true)
				.AddEnvironmentVariables();
			Configuration = builder.Build();

			CurrentEnvironment = env;
		}

		/// <summary>
		/// Global configuration object.
		/// Gets built by Startup method.
		/// </summary>
		public IConfigurationRoot Configuration { get; }

		private IHostingEnvironment CurrentEnvironment { get; set; }


		/// <summary>
		/// This method gets called by the runtime. Used to add services to the container.
		/// </summary>
		public void ConfigureServices(IServiceCollection services)
		{
			services.RegisterSharedServices(CurrentEnvironment, Configuration);

			services
				.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
				.AddCookie(
					CookieAuthenticationDefaults.AuthenticationScheme, 
					options => 
					{
						options.LoginPath = "/Account/LogIn";;
						options.AccessDeniedPath = new PathString("/account/login");
						options.Cookie.Name = "AUTHCOOKIE";
						options.ExpireTimeSpan = new TimeSpan(1, 0, 0, 0);
						options.Cookie.HttpOnly = true;
					}
				);

			services.AddMemoryCache();
			services.AddSession();

			// Add framework services.
			services.AddMvc().AddJsonOptions(opt =>
			{
				var resolver = opt.SerializerSettings.ContractResolver;
				if (resolver != null)
				{
					var res = resolver as DefaultContractResolver;
					res.NamingStrategy = null;  // this removes the camelcasing
				}
			});

			// lowercase all generated url within the app
			services.AddRouting(options => { options.LowercaseUrls = true; });

			// Add Cross Origin Security service
			services.AddCors();

			// Add application services.
			services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

			services.AddTransient<IApiController, ApiController>();
			services.AddTransient<HomeController>();
			services.AddTransient<AccountController>();

			services.AddTransient<IAuthService, AuthService>();
			services.AddTransient<IBadgeService, BadgeService>();

			services.AddTransient<ModelValidation>();
			services.AddTransient<ApiKeyCheck>();
			services.AddTransient<LogModel>();
			services.AddTransient<ReCaptcha>();
		}

		/// <summary>
		/// This method gets called by the runtime. Used to configure the HTTP request pipeline.
		/// </summary>
		public void Configure(
			IApplicationBuilder app,
			IHostingEnvironment env,
			ILoggerFactory loggerFactory,
			IServiceProvider serviceProvider
		)
		{
			loggerFactory
				.AddStatusLogger(
					serviceProvider.GetService<ILoggingService>(),
					env.IsTesting() ? LogLevel.Error : Configuration["Logging:MinLogLevel"].ToEnum<LogLevel>(),
					Configuration.StringsFromArray("Logging:Exclude").ToArray()
				);

			app.UseAuthentication();

			if (env.IsProduction())
			{
				app.UseExceptionHandler("/error"); // All serverside exceptions redirect to error page
				app.UseStatusCodePagesWithReExecute("/error/{0}");
			}
			else
			{
				app.UseDatabaseErrorPage();
				app.UseDeveloperExceptionPage(); // Print full stack trace
			}

			app.UseSession();

			app.UseCors(builder => builder.WithOrigins("*"));

			app.UseDefaultFiles(); // in wwwroot folder, index.html is served when opening a directory
			app.UseStaticFiles(); // make accessible and cache wwwroot files

			// define routes
			app.UseMvc(routes =>
			{
				routes.MapRoute(
					name: "default",
					template: "{controller=Home}/{action=Index}/{id?}");
			});


			if (env.IsTesting() || env.IsStaging())
			{
				using (var context = serviceProvider.GetService<IDataContext>())
				{
					// create scheme if it does not exist
					context.Database.EnsureCreated();
				}

				// Testing requires synchronous code
				// Test runner (at least XUnit) tends to run tests in parallel
				// When 2+ threads try to setup a virtual server in an async environment,
				// deadlock usually happens.
				serviceProvider.GetRequiredService<IDataSeedService>().SeedData();

				// Seed one CpuLoad data point 
				if (env.IsStaging())
				{
					serviceProvider
						.GetRequiredService<IApiController>()
						.CpuLoad(new CpuLoadViewModel { Source = "the-source", Value = 50 });
				}
			}
		}
	}
}
