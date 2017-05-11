// Entry point for log page

import { Utility } from "./modules/utility";

import "bootstrap-select"
import * as Waves from "Waves"
import "bootstrap"

declare global {
	interface JQuery {
		jsonViewer(any): void;
	}
}

$(async () => {

	$('.selectpicker').selectpicker();

	

	Utility.fixUtcTime();

});
