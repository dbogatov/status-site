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

		// Handler for remove metric click
		// Tells metric to remove itself and excludes it from the array of metrics
		$(`[data-identifier="${metric.getMetricIdentifier()}"] .remove-metric`).click(
			async (e) => {
				// Do not follow the link
				e.preventDefault();

				if (confirm(`Are sure you want to remove this metric {${metric.source}, ${MetricType[metric.metricType]}}?`)) {
					// Tell metric to destroy itself (gracefully)
					await metric.destroy();
					// Remove metric object form global array
					metrics.splice(metrics.indexOf(metric), 1);	
				}
			}
		);

		metrics.push(metric);
	});

	metrics.forEach((mt) => mt.turnOn());

	$(window).resize(() => {
		metrics.forEach((metric) => metric.render());
	});

});
