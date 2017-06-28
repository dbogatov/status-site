// Entry point for overview page

import $ = require('jquery');
import { PingMetric } from "./modules/metrics/ping";
import { CpuLoadMetric } from "./modules/metrics/cpu-load";
import { Metric, DataPoint, MetricType } from "./modules/metrics/abstract";
import { MetricFactory } from "./modules/metrics/factory"
import "bootstrap"
import { SharedDataProvider, IDataProvider } from "./modules/metrics/data-provider";
import { Utility } from "./modules/utility";

let metrics = new Array<Metric<DataPoint>>();
let dataProvider: IDataProvider = new SharedDataProvider();

$(async () => {

	while (!dataProvider.isLoaded()) {
		await Utility.sleep(50);
	}

	$(".metric").each((index, element) => {

		let source = $(element)
			.data("identifier")
			.substring("0-".length);

		let type = parseInt(
			$(element)
				.data("identifier")
				.substring(0, "0".length)
		);

		let metric = new MetricFactory(source).getMetric(type);
		metric.setDataProvider(dataProvider);

		metrics.push(metric);
	});

	metrics.forEach((mt) => mt.turnOn());

	$(window).resize(() => {
		metrics.forEach((metric) => metric.render());
	});

	$(".metric-group").on("show.bs.collapse", function() {
		$(`#${ $(this).data("metric-type")}-header-icon`)
			.removeClass("zmdi-chevron-up")
			.addClass("zmdi-chevron-down");
	});

	$(".metric-group").on("hide.bs.collapse", function() {
		$(`#${ $(this).data("metric-type")}-header-icon`)
			.removeClass("zmdi-chevron-down")
			.addClass("zmdi-chevron-up");
	});

	document.dispatchEvent(new Event("page-ready"));
});
