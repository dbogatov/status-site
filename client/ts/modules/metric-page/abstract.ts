import { PingMetric } from "../metrics/ping";
import { CpuLoadMetric } from "../metrics/cpu-load";
import { Metric, DataPoint, MetricType } from "../metrics/abstract";
import { Utility } from "../utility";
import "../extensions";

import "flot";
import "datatables.net"
import "../../vendor/jquery.flot.time.js";
import "../../vendor/jquery.flot.selection.js";
import "../../vendor/jquery.flot.threshold.js";
import "../../vendor/jquery.flot.tooltip.js";


/**
 * Represents set of procedures for rendering metric page.
 * 
 * @export
 * @class MetricPage
 */
export abstract class MetricPage<T extends Metric<DataPoint>> {

	protected metric: T;

	protected dataTablesRendered: boolean = false;

	protected min: number;
	protected max: number;

	constructor(min: number, max: number) {
		this.max = max;
		this.min = min;

		window.setTimeout(async () => {

			await this.metric.loadData(60 * 60 * 24 * 7); // week

			this.render();

		}, 100);
	}

	/**
	 * Renders plot in the UI.
	 * Does not load the data.
	 * 
	 * @private
	 * 
	 * @memberOf MetricPage
	 */
	protected abstract renderPlot(): void;

	/**
	 * Renders data tables in the UI.
	 * Does not load the data.
	 * 
	 * @private
	 * 
	 * @memberOf MetricPage
	 */
	protected abstract renderTable(): void;

	/**
	 * Renders numeric values in the UI.
	 * Does not load the data.
	 * 
	 * @private
	 * 
	 * @memberOf MetricPage
	 */
	private renderValues(): void {
		this.metric.autoLabel.render();
		this.metric.manualLabel.render();
	}

	/**
	 * Renders all components in the UI.
	 * Does not load the data.
	 * 
	 * @private
	 * 
	 * @memberOf MetricPage
	 */
	public render(): void {
		this.renderPlot();
		this.renderValues();
		this.renderTable();
	}
}
