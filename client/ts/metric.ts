// Entry point for metric page

import { CpuLoadMetricPage } from "./modules/metric-page/cpu-load";
import { PingMetricPage } from "./modules/metric-page/ping";
import { MetricPage } from "./modules/metric-page/abstract";
import { MetricType, Metric, DataPoint } from "./modules/metrics/abstract";
import { Utility } from "./modules/utility";

import "bootstrap-select"
import * as Waves from "Waves"
import "bootstrap"

declare var source: string;
declare var type: number;
declare var min: number;
declare var max: number;

// let metricPage : MetricPage<Metric>;

$(async () => {

	Utility.fixUtcTime();

	Waves.init();

	$('.selectpicker').selectpicker();


	// TODO: Factory
	let metricPage : MetricPage<Metric<DataPoint>>;

	if (type == MetricType.CpuLoad) {
		metricPage = new CpuLoadMetricPage(source, min, max);
	} else if (type == MetricType.Ping) {
		metricPage = new PingMetricPage(source, min, max);
	}

	$(window).resize(() => {
		metricPage.render();
	});

});
