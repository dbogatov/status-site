import { CpuLoadMetric } from "./concrete-metrics";
import { Metric, DataPoint, MetricType } from "./abstract-metric";
import { Utility } from "./utility";
import "./extensions";

import "flot";
import "datatables.net"
import "../vendor/jquery.flot.time.js";
import "../vendor/jquery.flot.selection.js";
import "../vendor/jquery.flot.threshold.js";
import "../vendor/jquery.flot.tooltip.js";


/**
 * Represents set of procedures for rendering metric page.
 * 
 * @export
 * @class MetricPage
 */
export class MetricPage {

	/**
	 * Source of the metric
	 * 
	 * @private
	 * @type {string}
	 * @memberOf MetricPage
	 */
	private source: string;
	/**
	 * Type of the metric
	 * 
	 * @private
	 * @type {MetricType}
	 * @memberOf MetricPage
	 */
	private type: MetricType;

	/**
	 * Data points of the metric
	 * 
	 * @private
	 * @type {Metric<DataPoint>}
	 * @memberOf MetricPage
	 */
	private metric: Metric<DataPoint>;

	/**
	 * Flag representing if data tables have been already rendered.
	 * We cannot easily re-render data tables.
	 * 
	 * @private
	 * @type {boolean}
	 * @memberOf MetricPage
	 */
	private dataTablesRendered: boolean = false;

	/**
	 * Creates an instance of MetricPage.
	 * @param {string} source - source of the metric
	 * @param {MetricType} type - type of the metric
	 * 
	 * @memberOf MetricPage
	 */
	constructor(source: string, type: MetricType) {
		this.source = source;
		this.type = type;

		this.metric = new CpuLoadMetric(source);

		window.setTimeout(async () => {

			await this.metric.loadData(60 * 60 * 24 * 7); // week

			this.render();

		}, 0);
	}

	/**
	 * Renders plot in the UI.
	 * Does not load the data.
	 * 
	 * @private
	 * 
	 * @memberOf MetricPage
	 */
	private renderPlot(): void {

		var data = [];
		(<CpuLoadMetric>this.metric)
			.data
			.sortByProperty(dp => dp.timestamp.getTime())
			.reverse()
			.forEach((value, index, array) => data.push([value.timestamp.getTime(), value.value]));

		let detailedPlotOptions: any = {
			yaxis: {
				max: 100,
				min: 0
			},
			xaxis: {
				mode: "time",
				tickLength: 5,
				timezone: "browser"
			},
			grid: {
				borderWidth: 0,
				labelMargin: 10,
				hoverable: true,
				clickable: true,
				mouseActiveRadius: 6
			},
			tooltip: {
				show: true,
				content: "%x: %y%",
				defaultTheme: false,
				cssClass: "flot-tooltip"
			},
			selection: {
				mode: "x"
			}
		};

		var formattedData = [
			{
				data: data,
				lines: {
					show: true,
					fill: 0.50
				},
				color: 'rgb(200, 20, 30)',
				threshold: {
					below: 90,
					color: "#FFC107"
				}
			}
		];

		var overviewPlotOptions = {
			series: {
				lines: {
					show: true,
					lineWidth: 1
				},
				shadowSize: 0
			},
			xaxis: {
				ticks: [],
				mode: "time"
			},
			yaxis: {
				ticks: [],
				min: 0,
				autoscaleMargin: 0.1
			},
			selection: {
				mode: "x"
			}
		};

		var plot: any = $.plot(
			$("#metric-detailed-plot"),
			formattedData,
			detailedPlotOptions
		);

		var overview: any = $.plot(
			$("#metric-overview-plot"),
			formattedData,
			overviewPlotOptions
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
	};

	/**
	 * Renders data tables in the UI.
	 * Does not load the data.
	 * 
	 * @private
	 * 
	 * @memberOf MetricPage
	 */
	private renderTable(): void {

		if (!this.dataTablesRendered) {

			$("#metric-data tbody").html(
				(<CpuLoadMetric>this.metric)
					.data
					.map(
					dp => `
						<tr>
							<td>${dp.timestamp}</td>
							<td>${dp.value}</td>
						</tr>
					`
					)
					.join()
			);

			$('#metric-data').DataTable({
				"order": [[0, "desc"]],
				lengthChange: false,
				searching: false,
				pageLength: 10
			});
		}

		this.dataTablesRendered = true;
	};

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
