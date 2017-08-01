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
using StatusMonitor.Shared.Models.Entities;

namespace StatusMonitor.Web.Services
{
	/// <summary>
	/// Generates SVG badges
	/// </summary>
	public interface IBadgeService
	{
		/// <summary>
		/// Generates overall health of the system badge
		/// </summary>
		/// <param name="report">Health report from which to generate badge</param>
		/// <returns>Badge indicating overall health of the system</returns>
		Badge GetSystemHealthBadge(HealthReport report);

		/// <summary>
		/// Generates individual metric's health badge
		/// </summary>
		/// <param name="source">Metric source</param>
		/// <param name="type">Metric type</param>
		/// <param name="label">Metric label</param>
		/// <returns>Badge indicating individual health of the metric</returns>
		Badge GetMetricHealthBadge(string source, Metrics type, AutoLabels label);

		/// <summary>
		/// Generates server's uptime
		/// </summary>
		/// <param name="url">URL of the servers whose uptime is presented</param>
		/// <param name="uptime">Percentage of time when service is online</param>
		/// <returns>Badge indicating server's uptime</returns>
		Badge GetUptimeBadge(string url, int uptime);
	}

	public class BadgeService : IBadgeService
	{
		public Badge GetMetricHealthBadge(string source, Metrics type, AutoLabels label)
		{
			return new Badge
			{
				Title = $"{type.ToString().ToLower()} of {source.ToLower()}",
				Message = label.ToString(),
				Status =
					label == AutoLabels.Normal ?
						BadgeStatus.Success :
						(label == AutoLabels.Warning ?
							BadgeStatus.Neutural :
							BadgeStatus.Failure
						)
			};
		}

		public Badge GetSystemHealthBadge(HealthReport report)
		{
			return new Badge
			{
				Title = "System health",
				Message = $"{report.Health}%",
				Status =
					report.Health >= 90 ?
						BadgeStatus.Success :
						(report.Health >= 70 ?
							BadgeStatus.Neutural :
							BadgeStatus.Failure
						)
			};
		}

		public Badge GetUptimeBadge(string url, int uptime)
		{
			return new Badge
			{
				Title = $"{url} uptime",
				Message = $"{uptime}%",
				Status =
				uptime >= 95 ?
					BadgeStatus.Success :
					(uptime >= 85 ?
						BadgeStatus.Neutural :
						BadgeStatus.Failure
					)
			};
		}
	}

	/// <summary>
	/// Special action result that returns badge
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

	/// <summary>
	/// Badge model
	/// </summary>
	public class Badge
	{
		/// <summary>
		/// Leftmost text
		/// </summary>
		public string Title { get; set; }
		/// <summary>
		/// Rightmost text
		/// </summary>
		public string Message { get; set; }
		/// <summary>
		/// Semantic meaning of the badge (good/bad)
		/// </summary>
		public BadgeStatus Status { get; set; }

		/// <summary>
		/// Width in px of the title
		/// </summary>
		public int TitleWidth
		{
			get
			{
				return Title.Length * 8;
			}
			private set { }
		}
		/// <summary>
		/// Width in px of the message
		/// </summary>
		public int MessageWidth
		{
			get
			{
				return (int)Math.Round(Message.Length * 12.5);
			}
			private set { }
		}

		/// <summary>
		/// HEX color representation of the badge semantic meaning
		/// </summary>
		/// <returns></returns>
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
