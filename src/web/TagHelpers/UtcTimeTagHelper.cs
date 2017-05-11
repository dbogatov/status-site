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
			output.Attributes.Add("class", "utc-time");

			output.Content.SetHtmlContent(Time.ToString());

			await Task.CompletedTask;
		}
	}
}
