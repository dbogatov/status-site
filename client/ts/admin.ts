// Entry point for log page

import { Utility } from "./modules/utility";

import "bootstrap-select"
import * as Waves from "Waves"
import "bootstrap"

$(async () => {

	$('.selectpicker').selectpicker();

	Utility.fixUtcTime();

	setupDiscrepancyViewer();

	document.dispatchEvent(new Event("page-ready"));
});

function setupDiscrepancyViewer() {
	var resolvedLoaded = 10;

	$("#load-more-resolved-btn").click(() => {

		for (var index = resolvedLoaded + 1; index <= resolvedLoaded + 10; index++) {
			$(`.discrepancy-card[data-number="${index}"]`).show();
		}

		resolvedLoaded += 10;

		$("#load-less-resolved-btn").show();

		if (resolvedLoaded >= $(".discrepancy-resolved").length) {
			$("#load-more-resolved-btn").hide();
		}
	});

	$("#load-less-resolved-btn").click(() => {

		for (var index = resolvedLoaded; index > resolvedLoaded - 10; index--) {
			$(`.discrepancy-card[data-number="${index}"]`).hide();
		}

		resolvedLoaded -= 10;

		$("#load-more-resolved-btn").show();

		if (resolvedLoaded <= 10) {
			$("#load-less-resolved-btn").hide();
		}
	});
}
