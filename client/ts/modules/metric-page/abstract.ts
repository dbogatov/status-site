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

	protected formattedData: any;
	protected detailedPlotOptions: any;
	protected overviewPlotOptions: any;

	protected minData: number;
	protected maxData: number;

	constructor(min: number, max: number) {
		this.max = max;
		this.min = min;

		window.setTimeout(async () => {

			await this.metric.loadData(60 * 60 * 24 * 7); // week

			this.render();

		}, 100);
	}

	/**
	 * Configures plot options.
	 * Sets following values:
	 * formattedData, detailedPlotOptions, overviewPlotOptions
	 * minData, maxData
	 * 
	 * @protected
	 * @abstract
	 * @memberof MetricPage
	 */
	protected abstract configurePlot(): void;

	/**
	 * Renders plot in the UI.
	 * Does not load the data.
	 * 
	 * @private
	 * 
	 * @memberOf MetricPage
	 */
	private renderPlot(): void {
		
		var plot: any = $.plot(
			$("#metric-detailed-plot"),
			this.formattedData,
			this.detailedPlotOptions
		);

		var overview: any = $.plot(
			$("#metric-overview-plot"),
			this.formattedData,
			this.overviewPlotOptions
		);

		// now connect the two

		$("#metric-detailed-plot").bind("plotselected", <any>((event, ranges) => {

			// do the zooming
			$.each(plot.getXAxes(), function (_, axis) {
				var opts = axis.options;
				opts.min = ranges.xaxis.from;
				opts.max = ranges.xaxis.to;
			});
			plot.setupGrid();
			plot.draw();
			plot.clearSelection();

			// don't fire event on the overview to prevent eternal loop
			overview.setSelection(ranges, true);
		}));

		$("#metric-overview-plot").bind("plotselected", <any>((event, ranges) => {
			plot.setSelection(ranges);
		}));

		// if latest data point is more than 2 hours ago
		// select recent 2 hours in plot
		if (new Date().getTime() - this.minData > 2 * 60 * 60 * 1000) {
			let from = new Date().getTime() - 2 * 60 * 60 * 1000;
			plot.setSelection({ xaxis: { from: from, to: this.maxData }, yaxis: { from: 0, to: 0 } });
		}
	}

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

		this.configurePlot()		
		this.renderPlot();
		
		this.renderValues();
		this.renderTable();
	}
}
