using System;
using Microsoft.AspNetCore.Razor.TagHelpers;
using StatusMonitor.Shared.Models.Entities;

namespace StatusMonitor.Web.TagHelpers
{
	[HtmlTargetElement("metric-values", Attributes = "metric")]
	/// <summary>
	/// Underlying class for <metric-values> tag.
	/// </summary>
	public class MetricValuesTagHelper : TagHelper
	{
		public Metric Metric { get; set; }

		/// <summary>
		/// Called by the framework. Renders tag helper.
		/// </summary>
		public override void Process(TagHelperContext context, TagHelperOutput output)
		{
			output.TagName = "div";
			output.TagMode = TagMode.StartTagAndEndTag;

			output.Attributes.Clear();
			output.Attributes.Add("class", $"row");

			output.Content.SetHtmlContent(
				$@"
					{GenerateLabels(Metric.AutoLabel, Metric.ManualLabel)}
					{GenerateCurrentValue(Metric.CurrentValue)}
					{GenerateStatValues(
						new Tuple<int, string>(Metric.DayAvg, "Day avg"),
						new Tuple<int, string>(Metric.HourAvg, "Hour avg")
					)}
					{GenerateStatValues(
						new Tuple<int, string>(Metric.DayMax, "Day max"),
						new Tuple<int, string>(Metric.HourMax, "Hour max")
					)}
					{GenerateStatValues(
						new Tuple<int, string>(Metric.DayMin, "Day min"),
						new Tuple<int, string>(Metric.HourMin, "Hour min")
					)}
				"
			);
		}

		/// <summary>
		/// Returns HTML code for labels part of the card.
		/// </summary>
		/// <param name="auto">Auto label to print</param>
		/// <param name="manual">Manual label to print</param>
		/// <returns>HTML code</returns>
		private string GenerateLabels(AutoLabel auto, ManualLabel manual)
		{
			return $@"
				<div class='col-md-12 col-sm-12 col-xs-12'>
					<div class='row metric-labels'>
						<div class='col-md-6 col-sm-6 col-xs-12'>
							<span data-identifier='{Metric.Type}-{Metric.Source}' class='metric-auto-label'>{auto.Title}<!-- --></span>
						</div>
						<div class='col-md-6 col-sm-6 col-xs-12'>
							<span data-identifier='{Metric.Type}-{Metric.Source}' class='metric-manual-label'>{manual.Title}<!-- --></span>
						</div>
					</div>
				</div>
			";
		}

		/// <summary>
		/// Returns HTML code for current value part of the card.
		/// </summary>
		/// <param name="value">Current value to print</param>
		/// <returns>HTML code</returns>
		private string GenerateCurrentValue(int value)
		{
			return $@"
				<div class='col-md-3 col-sm-3 col-xs-12'>
					<div class='metric-current-value'>
						<strong>{value}</strong>
						<small>Current value</small>
					</div>
				</div>
			";
		}

		/// <summary>
		/// Returns HTML code for statistical value part of the card.
		/// </summary>
		/// <param name="top">A tuple containing numeric value and description of stat 
		/// value for the top part of the card</param>
		/// <param name="bottom">A tuple containing numeric value and description of stat 
		/// value for the bottom part of the card</param>
		/// <returns>HTML code</returns>
		private string GenerateStatValues(Tuple<int, string> top, Tuple<int, string> bottom)
		{
			return $@"
				<div class='col-md-3 col-sm-3 col-xs-4'>
					<div class='metric-stat-value {GenerateStatClassName(top.Item2)}'>
						<strong>{top.Item1}</strong>
						<small>{top.Item2}</small>
					</div>
					<div class='metric-stat-value {GenerateStatClassName(bottom.Item2)}'>
						<strong>{bottom.Item1}</strong>
						<small>{bottom.Item2}</small>
					</div>
				</div>
			";
		}

		/// <summary>
		/// Returns class name for the given stat value description
		/// </summary>
		/// <param name="description">Description of the stat value</param>
		/// <returns>Class name for the sat value</returns>
		private string GenerateStatClassName(string description)
		{
			return description.Replace(" ", "-").ToLower();
		}
	}
}
