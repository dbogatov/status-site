using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace StatusMonitor.Web.TagHelpers
{
	[HtmlTargetElement("utc-time", Attributes = "time")]
	/// <summary>
	/// Underlying class for <local-time> tag.
	/// </summary>
	public class UtcTimeTagHelper : TagHelper
	{
		/// <summary>
		/// If true, the full date should be rendered
		/// Otherwise, only time part should be rendered
		/// </summary>
		public bool ShowDate { get; set; } = false;

		public DateTime Time { get; set; }

		[ViewContext]
		public ViewContext ViewContext { get; set; }

		/// <summary>
		/// Called by the framework. Renders tag helper.
		/// </summary>
		public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
			output.TagName = "span";
			output.TagMode = TagMode.StartTagAndEndTag;

			output.Attributes.Clear();
			
			output.Attributes.Add("class", ShowDate ? "utc-date" : "utc-time");

			output.Content.SetHtmlContent(Time.ToString());

			await Task.CompletedTask;
		}
	}
}
