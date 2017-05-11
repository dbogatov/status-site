using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using StatusMonitor.Shared.Models.Entities;

namespace StatusMonitor.Web.TagHelpers
{
	[HtmlTargetElement("metric-card", Attributes = "metric")]
	/// <summary>
	/// Underlying class for <metric-card> tag.
	/// </summary>
	public class MetricCardTagHelper : TagHelper
	{
		public Metric Metric { get; set; }

		[ViewContext]
		public ViewContext ViewContext { get; set; }

		/// <summary>
		/// Called by the framework. Renders tag helper.
		/// </summary>
		public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
			output.TagName = "div";
			output.TagMode = TagMode.StartTagAndEndTag;

			output.Attributes.Clear();
			output.Attributes.Add("class", $"card metric");
			output.Attributes.Add("data-identifier", $"{Metric.Type}-{Metric.Source}");

			var visibilityIcon = $@"
				<li>
					<a>
						<i class='zmdi zmdi-eye{ (Metric.Public ? "" : "-off") }'><!-- --></i>
					</a>
				</li>
			";

			output.Content.SetHtmlContent(
				$@"
					<div class='card-header'>
						<h2>
							{Metric.Source}
							<small>Last updated <span class='last-updated utc-time'>{Metric.LastUpdated}</span></small>
						</h2>

						<ul class='actions'>
							{ (ViewContext.HttpContext.User.Identity.IsAuthenticated ? visibilityIcon : "") }
							<li>
								<a href='/home/metric/{((Metrics)Metric.Type).ToString()}/{Metric.Source}'>
									<i class='zmdi zmdi-open-in-new'><!-- --></i>
								</a>
							</li>
						</ul>
					</div>

					<div class='card-body'>
						<div class='row'>
							<div class='col-md-12'>
								{(await output.GetChildContentAsync()).GetContent()}
							</div>
							<div class='col-md-12'>
								<div class='metric-chart flot-chart line-chart'></div>
							</div>
						</div>
					</div>
				"
			);
		}
	}
}
