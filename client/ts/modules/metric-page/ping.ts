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

	protected configurePlot(): void {

		var data = [], errors = [];
		this
			.metric
			.data
			.sortByProperty(dp => dp.timestamp.getTime())
			.reverse()
			.forEach(
			(value, index, array) => {
				if (value.success) {
					data.push([value.timestamp.getTime(), value.responseTime]);
				} else {
					errors.push([value.timestamp.getTime(), this.max]);
				}
			}
			);
		
		this.minData = this.metric.data.sortByProperty(dp => dp.timestamp.getTime())[0].timestamp.getTime();
		this.maxData = this.metric.data.sortByProperty(dp => dp.timestamp.getTime()).reverse()[0].timestamp.getTime();

		let barWidth =
			this.metric.data.length > 1
				?
				this.metric.data.sortByProperty(dp => dp.timestamp.getTime())[1].timestamp.getTime() -
				this.metric.data.sortByProperty(dp => dp.timestamp.getTime())[0].timestamp.getTime()
				:
				60 * 1000; // default

		this.detailedPlotOptions = {
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

		this.formattedData = [
			{
				data: data,
				lines: {
					show: true,
					fill: 0.50
				},
				color: "rgb(113, 185, 226)"
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

		this.overviewPlotOptions = {
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
	};

	protected renderTable(redraw: boolean, start: Date, end: Date): void {

		if (!this.dataTablesRendered || redraw) {

			if (this.dataTablesRendered) {
				this.dataTable.destroy();
			}

			let header = `
				<tr>
					<th>Timestamp</th>
					<th>Latency</th>
					<th>Result</th>
					<th>Zoom plot</th>
				</tr>
			`;

			$("#metric-data thead").html(header);
			$("#metric-data tfoot").html(header);

			$("#metric-data tbody").html(
				this.metric
					.data
					.map(dp => <PingDataPoint>dp)
					.filter((value, index, array) => {
						if (start != null && value.timestamp < start) {
							return false;
						}

						if (end != null && value.timestamp > end) {
							return false;
						}

						return true;
					})
					.map(
					dp => `
						<tr>
							<td>${dp.timestamp}</td>
							<td>${dp.responseTime} ms</td>
							<td>${dp.success ? "Success" : dp.message}</td>
							<td>
								<a href="/home/metric/${MetricType[this.metric.metricType]}/${this.metric.source}/${new Date(dp.timestamp.getTime() - 2 * 60 * 1000).getTime()}/${new Date(dp.timestamp.getTime() + 2 * 60 * 1000).getTime()}">
									Zoom plot
								</a>
							</td>
						</tr>
					`
					)
					.join()
			);

			this.dataTable = $('#metric-data').DataTable({
				"order": [[0, "desc"]],
				lengthChange: false,
				searching: false,
				pageLength: 10
			});
		}

		this.dataTablesRendered = true;
	};
}
