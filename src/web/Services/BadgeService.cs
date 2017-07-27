using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using StatusMonitor.Shared.Models;

namespace StatusMonitor.Web.Services
{
	public interface IBadgeService
	{
		Task<Badge> GetHealthBadgeAsync();
	}

	public class BadgeService : IBadgeService
	{
		private readonly IDataContext _context;

		public BadgeService(
			IDataContext context
		)
		{
			_context = context;
		}

		public async Task<Badge> GetHealthBadgeAsync()
		{
			var healthReport = await _context.HealthReports.OrderByDescending(hp => hp.Timestamp).FirstAsync();

			return new Badge
			{
				Title = "System health",
				Message = $"{healthReport.Health.ToString("D2")}%",
				Status =
					healthReport.Health == 100 ?
						BadgeStatus.Success :
						(healthReport.Health >= 70 ?
							BadgeStatus.Neutural :
							BadgeStatus.Failure
						),
				TitleWidth = 100,
				MessageWidth = 40
			};
		}
	}

	/// <summary>
	/// Special action result that returns sitemap
	/// </summary>
	public class BadgeResult : IActionResult
	{
		public Badge Badge { get; private set; }

		public BadgeResult(Badge badge)
		{
			Badge = badge;
		}

		public Task ExecuteResultAsync(ActionContext context)
		{
			context.HttpContext.Response.ContentType = "image/svg+xml";

			using (var output = XmlWriter.Create(context.HttpContext.Response.Body))
			{
				WriteTo(output);
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Generate XML from the a badge
		/// </summary>
		/// <param name="writer">XmlWriter to generate to</param>
		public void WriteTo(XmlWriter writer)
		{
			var xml = XDocument.Parse(
				$@"
					<svg xmlns='http://www.w3.org/2000/svg' width='{Badge.TitleWidth + Badge.MessageWidth}' height='20'>
						<linearGradient id='b' x2='0' y2='100%'>
							<stop offset='0' stop-color='#bbb' stop-opacity='.1'/>
							<stop offset='1' stop-opacity='.1'/>
						</linearGradient>

						<mask id='a'>
							<rect width='{Badge.TitleWidth + Badge.MessageWidth}' height='20' rx='3' fill='#fff'/>
						</mask>

						<g mask='url(#a)'>
							<path fill='#555'
								d='M0 0 h{Badge.TitleWidth} v20 H0 z'/>
							<path fill='{Badge.HexColor}'
								d='M{Badge.TitleWidth} 0 h{Badge.MessageWidth} v20 H{Badge.TitleWidth} z'/>
							<path fill='url(#b)'
								d='M0 0 h{Badge.TitleWidth + Badge.MessageWidth} v20 H0 z'/>
						</g>

						<g fill='#fff' text-anchor='middle'>
							<g font-family='DejaVu Sans,Verdana,Geneva,sans-serif' font-size='11'>
								<text x='{Badge.TitleWidth / 2}' y='15' fill='#010101' fill-opacity='.3'>
									{Badge.Title}
								</text>
								<text x='{Badge.TitleWidth / 2}' y='14'>
									{Badge.Title}
								</text>
								<text x='{Badge.TitleWidth + Badge.MessageWidth / 2}' y='15' fill='#010101' fill-opacity='.3'>
									{Badge.Message}
								</text>
								<text x='{Badge.TitleWidth + Badge.MessageWidth / 2}' y='14'>
									{Badge.Message}
								</text>
							</g>
						</g>
					</svg>
				"
			);
			xml.Save(writer);
		}
	}

	public class Badge
	{
		public string Title { get; set; }
		public string Message { get; set; }
		public BadgeStatus Status { get; set; }

		public int TitleWidth { get; set; }
		public int MessageWidth { get; set; }

		public string HexColor
		{
			get
			{
				switch (Status)
				{
					case BadgeStatus.Neutural:
						return "#dfb317";
					case BadgeStatus.Success:
						return "#44cc11";
					case BadgeStatus.Failure:
						return "#e05d44";
					default:
						return "#000000";
				}
			}
		}
	}

	public enum BadgeStatus
	{
		Success, Failure, Neutural
	}
}
