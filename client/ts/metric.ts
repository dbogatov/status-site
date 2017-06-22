// Entry point for metric page

import { CpuLoadMetricPage } from "./modules/metric-page/cpu-load";
import { PingMetricPage } from "./modules/metric-page/ping";
import { MetricPage } from "./modules/metric-page/abstract";
import { MetricPageFactory } from "./modules/metric-page/factory";
import { MetricType, Metric, DataPoint } from "./modules/metrics/abstract";
import { Utility } from "./modules/utility";

import "bootstrap-select"
import * as Waves from "Waves"
import "bootstrap"

declare var source: string;
declare var type: number;
declare var min: number;
declare var max: number;

$(async () => {

	Utility.fixUtcTime();

	Waves.init();

	$('.selectpicker').selectpicker();

	let metricPage = new MetricPageFactory(source, min, max).getMetricPage(type);

	$(window).resize(() => {
		metricPage.render();
	});

	document.dispatchEvent(new Event("page-ready"));
});
