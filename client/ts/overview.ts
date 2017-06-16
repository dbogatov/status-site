// Entry point for overview page

import $ = require('jquery');
import { PingMetric } from "./modules/metrics/ping";
import { CpuLoadMetric } from "./modules/metrics/cpu-load";
import { Metric, DataPoint, MetricType } from "./modules/metrics/abstract";
import { MetricFactory } from "./modules/metrics/factory"
import "bootstrap"

let metrics = new Array<Metric<DataPoint>>();

$(() => {

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

		metrics.push(metric);
	});

	metrics.forEach((mt) => mt.turnOn());

	$(window).resize(() => {
		metrics.forEach((metric) => metric.render());
	});

});
