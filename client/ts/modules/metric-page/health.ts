import { HealthMetric, HealthDataPoint } from "../metrics/health";
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
 * @class HealthMetricPage
 * @extends {MetricPage<Metric<HealthDataPoint>>}
 */
export class HealthMetricPage extends MetricPage<Metric<HealthDataPoint>> {

	constructor(source: string, min: number, max: number) {
		super(min, max);

		this.metric = new HealthMetric(source);
	}

	protected configurePlot(): void {

		var data = [];
		this
			.metric
			.data
			.sortByProperty(dp => dp.timestamp.getTime())
			.reverse()
			.forEach((value, index, array) => data.push([value.timestamp.getTime(), value.health]));

		this.minData = data[data.length - 1][0];
		this.maxData = data[0][0];

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
				content: "%x: %y%",
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
				color: "rgb(77,167,77)",
				threshold: [
					{
						below: 90,
						color: "#FFC107"
					}, {
						below: 70,
						color: "rgb(200, 20, 30)"
					}
				]
			}
		];

		this.overviewPlotOptions = {
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
	};

	protected renderTable(): void {

		if (!this.dataTablesRendered) {

			let header = `
				<tr>
					<th>Timestamp</th>
					<th>Health</th>
				</tr>
			`;

			$("#metric-data thead").html(header);
			$("#metric-data tfoot").html(header);

			$("#metric-data tbody").html(
				this.metric
					.data
					.map(dp => <HealthDataPoint>dp)
					.map(
					dp => `
						<tr>
							<td>${dp.timestamp}</td>
							<td>${dp.health}%</td>
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
