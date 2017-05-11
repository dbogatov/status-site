// Entry point for log page

import "jquery.json-viewer/json-viewer/jquery.json-viewer.js"
import { Utility } from "./modules/utility";

declare global {
	interface JQuery {
		jsonViewer(any): void;
	}
}

$(async () => {

	$(".json-renderer").each(function () {
		var data = JSON.parse($(this).text());
		$(this).html("");
		$(this).jsonViewer(data);
	});

	Utility.fixUtcTime();

});
