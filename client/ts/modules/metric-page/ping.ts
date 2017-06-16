import { PingMetric, PingDataPoint } from "../metrics/ping";
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
 * @class PingMetricPage
 * @extends {MetricPage<Metric<PingDataPoint>>}
 */
export class PingMetricPage extends MetricPage<Metric<PingDataPoint>> {

	constructor(source: string, min: number, max: number) {
		super(min, max);

		this.metric = new PingMetric(source);
	}

	protected renderPlot(): void {

		var data = [], errors = [];
		this
			.metric
			.data
			.sort(dp => dp.timestamp.getMilliseconds())
			.reverse()
			.forEach(
			(value, index, array) => {
				if (value.httpStatusCode == 200) {
					data.push([value.timestamp.getTime(), value.responseTime]);
				} else {
					errors.push([value.timestamp.getTime(), this.max]);
				}
			}
			);

		let barWidth =
			this.metric.data.length > 1
				?
				this.metric.data.sort(dp => dp.timestamp.getMilliseconds())[1].timestamp.getTime() -
				this.metric.data.sort(dp => dp.timestamp.getMilliseconds())[0].timestamp.getTime()
				:
				60 * 1000; // default

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
				content: "%x: %y ms",
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
				points: {
					show: true
				},
				color: "#FFC107"
			},
			{
				data: errors,
				bars: {
					show: true,
					align: 'center',
					barWidth: barWidth * 1.05, // to make sure consecutive bars look like one bar
					lineWidth: 0,
					fillColor: {
						colors: [{
							opacity: 1.0
						}, {
							opacity: 1.0
						}]
					}
				},
				color: "rgb(200, 20, 30)",
				hoverable: false,
				highlightColor: "rgb(200, 20, 30)"
			}
		];

		var overviewPlotOptions = {
			series: {
				lines: {
					show: true,
					lineWidth: 1
				},
				bars: {
					show: true,
					align: 'center',
					barWidth: 1.9,
					lineWidth: 0,
					fillColor: {
						colors: [{
							opacity: 1.0
						}, {
							opacity: 1.0
						}]
					}
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

	protected renderTable(): void {

		if (!this.dataTablesRendered) {

			let header = `
				<tr>
					<th>Timestamp</th>
					<th>Latency</th>
					<th>Response Code</th>
				</tr>
			`;

			$("#metric-data thead").html(header);
			$("#metric-data tfoot").html(header);

			$("#metric-data tbody").html(
				this.metric
					.data
					.map(dp => <PingDataPoint>dp)
					.map(
					dp => `
						<tr>
							<td>${dp.timestamp}</td>
							<td>${dp.responseTime}</td>
							<td>${dp.httpStatusCode}</td>
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
