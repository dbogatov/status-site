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
import { Constants } from "../constants";


declare var start: number;
declare var end: number;

/**
 * Represents set of procedures for rendering metric page.
 * 
 * @export
 * @class MetricPage
 */
export abstract class MetricPage<T extends Metric<DataPoint>> {

	protected metric: T;

	protected dataTablesRendered: boolean = false;
	protected dataTable : DataTables.DataTable;

	/**
	 * Minimal theoretical value for data series.
	 * Used to define min and max for plot render.
	 * 
	 * @protected
	 * @type {number}
	 * @memberof MetricPage
	 */
	protected min: number;
	/**
	 * Maximal theoretical value for data series.
	 * Used to define min and max for plot render.
	 * 
	 * @protected
	 * @type {number}
	 * @memberof MetricPage
	 */
	protected max: number;

	protected formattedData: any;
	protected detailedPlotOptions: any;
	protected overviewPlotOptions: any;

	protected minData: number;
	protected maxData: number;

	protected start: Date = null;
	protected end: Date = null;

	constructor(min: number, max: number) {
		this.max = max;
		this.min = min;

		if (start > 0) {
			this.start = new Date(start);
		}

		if (end > 0) {
			this.end = new Date(end);
		}

		window.setTimeout(async () => {

			await this.metric.loadData(60 * 60 * 24 * 3); // 3 days

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

			this.renderTable(true, new Date(ranges.xaxis.from), new Date(ranges.xaxis.to));
		}));

		$("#metric-overview-plot").bind("plotselected", <any>((event, ranges) => {
			plot.setSelection(ranges);
		}));

		if (this.start != null) {

			let from = Math.min(
				Math.max(
					this.start.getTime(),
					this.minData
				),
				this.maxData)
				;

			let to =
				this.end == null ?
					Math.min(
						this.maxData,
						this.start.getTime() + Constants.METRIC_PAGE_DATA_PREVIEW
					) :
					Math.max(
						Math.min(
							this.end.getTime(),
							this.maxData
						),
						this.minData)
				;

			plot.setSelection({ xaxis: { from: from, to: to }, yaxis: { from: 0, to: 0 } });
		} else {
			// if latest data point is more than 2 hours ago
			// select recent 2 hours in plot
			if (new Date().getTime() - this.minData > Constants.METRIC_PAGE_DATA_PREVIEW) {
				let from = new Date().getTime() - Constants.METRIC_PAGE_DATA_PREVIEW;
				plot.setSelection({ xaxis: { from: from, to: this.maxData }, yaxis: { from: 0, to: 0 } });
			}
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

	/**
	 * Renders data tables in the UI.
	 * Does not load the data.
	 * 
	 * @protected
	 * @abstract
	 * @param {boolean} redraw if data table has to be force-redrawn; implies non-null values of start and end;
	 * @param {Date} start start date for filter
	 * @param {Date} end end date for filter
	 * @memberof MetricPage
	 */
	protected abstract renderTable(redraw: boolean, start: Date, end: Date): void;

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
		this.renderTable(false, null, null);
	}
}
