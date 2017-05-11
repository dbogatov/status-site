// Entry point for metric page

import { MetricPage } from "./modules/metric-page";
import { Utility } from "./modules/utility";

import "bootstrap-select"
import * as Waves from "Waves"
import "bootstrap"

declare var source: string;
declare var type: number;

let metricPage : MetricPage;

$(async () => {

	Utility.fixUtcTime();

	Waves.init();

	$('.selectpicker').selectpicker();

    metricPage = new MetricPage(source, type);

    $(window).resize(() => {
		metricPage.render();
	});

});
