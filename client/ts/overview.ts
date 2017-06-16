// Entry point for overview page

import $ = require('jquery');
import { PingMetric } from "./modules/metrics/ping";
import { CpuLoadMetric } from "./modules/metrics/cpu-load";
import { Metric, DataPoint, MetricType } from "./modules/metrics/abstract";
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

		// Because we render only CPU metric for now
		// Should be MetricFactory when we render multiple types of metrics

		// TODO: use factory

		let metric : Metric<DataPoint>;

		if (type == MetricType.CpuLoad) {
			metric = new CpuLoadMetric(source);
		} else if (type == MetricType.Ping) {
			metric = new PingMetric(source);
		}

		metrics.push(metric);
	});

	metrics.forEach((mt) => mt.turnOn());

	$(window).resize(() => {
		metrics.forEach((metric) => metric.render());
	});

});
