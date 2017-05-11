using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StatusMonitor.Web.ActionFilters;
using StatusMonitor.Shared.Extensions;
using StatusMonitor.Shared.Models.Entities;
using StatusMonitor.Web.ViewModels;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace StatusMonitor.Web.Controllers.Api
{
	public partial class ApiController
	{
		[HttpGet]
		[Produces("application/json")]
		[ServiceFilter(typeof(ModelValidation))]
		public async Task<IActionResult> GetData(DataRequestViewModel model)
		{
			// Retrieve requested metric
			var metrics = await _metricService.GetMetricsAsync(model.MetricType, model.Source);

			// Return appropriate status code if metric does not exist
			if (metrics.Count() == 0)
			{
				return NotFound();
			}

			var metric = metrics.First();

			if (!_auth.IsAuthenticated() && !metric.Public)
			{
				return Unauthorized();
			}

			// Compute the timestamp from which data is requested
			var fromTimestamp = DateTime.UtcNow - new TimeSpan(0, 0, model.TimePeriod);
			var data = new List<object>();

			switch ((Metrics)metric.Type)
			{
				case Metrics.CpuLoad:
					data = await GrabDataAsync<NumericDataPoint, Object>(
						_context.NumericDataPoints,
						metric,
						fromTimestamp
					);
					break;
				case Metrics.Compilation:
					data = await GrabDataAsync<CompilationDataPoint, Object>(
						_context.CompilationDataPoints,
						metric,
						fromTimestamp
					);
					break;
				case Metrics.Ping:
					data = await GrabDataAsync<PingDataPoint, Object>(
						_context.PingDataPoints,
						metric,
						fromTimestamp
					);
					break;
				case Metrics.Log:
					data = await GrabDataAsync<LogDataPoint, Object>(
						_context.LogDataPoints,
						metric,
						fromTimestamp
					);
					break;
				case Metrics.UserAction:
					data = await GrabDataAsync<UserActionDataPoint, Object>(
						_context.UserActionDataPoints,
						metric,
						fromTimestamp
					);
					break;
				default:
					var ex = new ArgumentOutOfRangeException($"Unknown metric type: {metric.Type}");
					_logger.LogCritical(LoggingEvents.Metrics.AsInt(), ex, "Unknown metric in GetData");
					throw ex;
			}

			return data != null ? (IActionResult)Json(data) : NoContent();
		}

		[NonAction]
		/// <summary>
		/// Queries the data provider set (database table) of data points
		/// and returns matched results or null.
		/// </summary>
		/// <param name="points">Reference to the data provider set (database table) with DataPoint's</param>
		/// <param name="metric">Metric for which data points are requested</param>
		/// <param name="includeExpression">Functional expression indicating which related property of T
		///  needs to be loaded from the data provider</param>
		/// <param name="fromTimestamp">Number of seconds ago from which data points are requested</param>
		/// <returns>List of objects of data points exposed to users (see DataPoint.PublicFields())
		/// or null if there is no data matching criteria.</returns>
		private async Task<List<object>> GrabDataAsync<T, TProperty>(
			DbSet<T> points,
			Metric metric,
			DateTime fromTimestamp,
			Expression<Func<T, TProperty>> includeExpression = null) where T : DataPoint
		{
			var data = points
				.Where(dp => dp.Metric == metric && dp.Timestamp > fromTimestamp);

			if (includeExpression != null)
			{
				data = data.Include(includeExpression);
			}

			if (data.Count() > 0)
			{
				return
					await data
						.OrderByDescending(dp => dp.Timestamp)
						.Select(dp => dp.PublicFields())
						.ToListAsync();
			}

			return null;
		}
	}
}
