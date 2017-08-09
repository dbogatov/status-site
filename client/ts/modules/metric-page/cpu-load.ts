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

	protected configurePlot(): void {

		var data = [];
		this
			.metric
			.data
			.sortByProperty(dp => dp.timestamp.getTime())
			.reverse()
			.forEach((value, index, array) => data.push([value.timestamp.getTime(), value.value]));

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
				color: 'rgb(200, 20, 30)',
				threshold: {
					below: 90,
					color: "#FFC107"
				}
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

	protected renderTable(redraw: boolean, start: Date, end: Date): void {

		if (!this.dataTablesRendered || redraw) {

			if (this.dataTablesRendered) {
				this.dataTable.destroy();
			}

			let header = `
				<tr>
					<th>Timestamp</th>
					<th>Value</th>
					<th>Zoom plot</th>
				</tr>
			`;

			$("#metric-data thead").html(header);
			$("#metric-data tfoot").html(header);

			$("#metric-data tbody").html(
				this.metric
					.data
					.map(dp => <CpuLoadDataPoint>dp)
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
							<td>${dp.value}%</td>
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
