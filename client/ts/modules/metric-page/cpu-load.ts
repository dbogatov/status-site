import { CpuLoadMetric, CpuLoadDataPoint } from "../metrics/cpu-load";
import { MetricPage } from "./abstract";
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
 * 
 * 
 * @export
 * @class CpuLoadMetricPage
 * @extends {MetricPage<Metric<CpuLoadDataPoint>>}
 */
export class CpuLoadMetricPage extends MetricPage<Metric<CpuLoadDataPoint>> {

	constructor(source: string, min: number, max: number) {
		super(min, max);

		this.metric = new CpuLoadMetric(source);
	}

	protected renderPlot(): void {

		var data = [];
		this
			.metric
			.data
			.sortByProperty(dp => dp.timestamp.getTime())
			.reverse()
			.forEach((value, index, array) => data.push([value.timestamp.getTime(), value.value]));


		let detailedPlotOptions: any = {
			yaxis: {
				max: this.max,
				min: this.min
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

		// if latest data point is more than 2 hours ago
		// select recent 2 hours in plot
		if (new Date().getTime() - data[data.length - 1][0] > 2 * 60 * 60 * 1000) {
			let from = new Date().getTime() - 2 * 60 * 60 * 1000;
			plot.setSelection({ xaxis: { from: from, to: data[0][0] }, yaxis: { from: 0, to: 0 } });
		}
	};

	protected renderTable(): void {

		if (!this.dataTablesRendered) {

			let header = `
				<tr>
					<th>Timestamp</th>
					<th>Value</th>
				</tr>
			`;

			$("#metric-data thead").html(header);
			$("#metric-data tfoot").html(header);

			$("#metric-data tbody").html(
				this.metric
					.data
					.map(dp => <CpuLoadDataPoint>dp)
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
}
