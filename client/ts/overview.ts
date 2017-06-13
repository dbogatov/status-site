// Entry point for overview page

import $ = require('jquery');
import { CpuLoadMetric } from "./modules/concrete-metrics";
import { Metric, DataPoint, MetricType } from "./modules/abstract-metric";
import "bootstrap"

let metrics = new Array<Metric<DataPoint>>();

$(() => {

	$(".metric").each((index, element) => {

		let source = $(element)
			.data("identifier")
			.substring("0-".length);

		// Because we render only CPU metric for now
		// Should be MetricFactory when we render multiple types of metrics
		let metric = new CpuLoadMetric(source);

		metrics.push(metric);
	});

	metrics.forEach((mt) => mt.turnOn());

	$(window).resize(() => {
		metrics.forEach((metric) => metric.render());
	});

});
