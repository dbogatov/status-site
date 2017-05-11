// Entry point for logs page

import "eonasdan-bootstrap-datetimepicker"
import "datatables.net"
import "bootstrap"
import "jquery.json-viewer/json-viewer/jquery.json-viewer.js"
import "bootstrap-select"
import * as Waves from "Waves"
import { Utility } from "./modules/utility";

declare global {
	interface JQuery {
		jsonViewer(any): void;
		modal(any?): void;
		datetimepicker(any?): void;
		selectpicker(...any): void;
	}
}

declare var filterModel: any;

$(async () => {

	$('#logs-dt').DataTable({
		order: [
			[0, "desc"]
		],
		lengthChange: false,
		searching: false,
		pageLength: 50,
		columnDefs: [{
			targets: 5,
			render: function (data, type, full, meta) {
				return Utility.toHHMMSS(
					Utility.toLocalTimezone(
						new Date(data)
					)
				);
			}
		}]
	});

	$(".json-renderer").each(function () {
		var data = JSON.parse($(this).text());
		$(this).html("");
		$(this).jsonViewer(data);
	});

	Waves.init();

	// Restore filter data
	$('#sources-ui').selectpicker('val', filterModel.sources.split(","));
	$('#categories-ui').selectpicker('val', filterModel.categories.split(","));
	$('#severities-ui').selectpicker('val', filterModel.severities.split(","));

	var startDate = new Date(0);
	startDate.setUTCMilliseconds(parseInt(filterModel.start));
	let start = (filterModel.start === "") ? {} : {
		date: startDate
	};

	var endDate = new Date(0);
	endDate.setUTCMilliseconds(parseInt(filterModel.end));
	let end = (filterModel.end === "") ? {} : {
		date: endDate
	};

	$('#start-ui').datetimepicker(start);
	$('#end-ui').datetimepicker(end);

	$("input[name=keywords]").val(filterModel.keywords);

	// Before submitting the form set values from select2 and datetimepicker
	$('#filter-form').on('submit', (e) => {

		$('input[name=sources]').val($('#sources-ui').val());
		$('input[name=categories]').val($('#categories-ui').val());
		$('input[name=severities]').val($('#severities-ui').val());

		$('input[name=start]').val(
			$("#start-ui").val() === "" ?
				"" :
				Utility.toUtcDate(new Date($("#start-ui").val()))
		);

		$('input[name=end]').val(
			$("#end-ui").val() === "" ?
				"" :
				Utility.toUtcDate(new Date($("#end-ui").val()))
		);

	});

	Utility.fixUtcTime();

});
