import { UserActionMetric, UserActionDataPoint } from "../metrics/user-action";
import { MetricPage } from "./abstract";
import { Metric, DataPoint, MetricType } from "../metrics/abstract";
import { Utility } from "../utility";
import * as Collections from 'typescript-collections';
import "../extensions";

import "flot";
import "datatables.net"
import "../../vendor/jquery.flot.time.js";
import "../../vendor/jquery.flot.selection.js";
import "../../vendor/jquery.flot.threshold.js";
import "../../vendor/jquery.flot.tooltip.js";
import { Constants } from "../constants";

/**
 * 
 * 
 * @export
 * @class UserActionMetricPage
 * @extends {MetricPage<Metric<UserActionDataPoint>>}
 */
export class UserActionMetricPage extends MetricPage<Metric<UserActionDataPoint>> {

	constructor(source: string, min: number, max: number) {
		super(min, max);

		this.metric = new UserActionMetric(source);
	}

	protected configurePlot(): void {
		var data = new Collections.Dictionary<string, any[]>();
		let min = 0, max = Number.MIN_SAFE_INTEGER, series = [];
		let keysNumber = 0, barWidth = 0, index = 0;

		// Split data into categories defined by action
		this
			.metric
			.data
			.sortByProperty(dp => dp.timestamp.getTime())
			.reverse()
			.forEach(
			(value, index, array) => {
				if (data.containsKey(value.action)) {
					data.getValue(value.action).push({ timestamp: value.timestamp, count: value.count });
				} else {
					data.setValue(value.action, [{ timestamp: value.timestamp, count: value.count }]);
				}
			}
			);

		keysNumber = data.size();
		barWidth = ((Constants.USER_ACTIONS_AGGREGATION_INTERVAL / 2) / keysNumber);

		data.forEach((key, value) => {
			let groupedByInterval = new Collections.Dictionary<number, any[]>();
			let seriesData = [];

			// Group and aggregate (sum) data per interval
			value.forEach(
				(val, i, array) => {
					let interval = Math.floor(val.timestamp.getTime() / (1000 * 60 * Constants.USER_ACTIONS_AGGREGATION_INTERVAL));
					if (groupedByInterval.containsKey(interval)) {
						groupedByInterval.setValue(interval, groupedByInterval.getValue(interval) + val.count);
					} else {
						groupedByInterval.setValue(interval, val.count);
					}
				}
			);

			let barShift = barWidth * index;

			groupedByInterval.forEach(
				(groupKey, groupValue) =>
					seriesData.push(
						[
							1000 * 60 * (groupKey * Constants.USER_ACTIONS_AGGREGATION_INTERVAL + barShift), // timestamp (x value)
							groupValue, // count
							key // action
						]
					)
			);

			seriesData
				.sortByProperty(point => point[0])
				.reverse();

			series.push({
				data: seriesData,
				bars: {
					show: true,
					align: 'center',
					barWidth: barWidth * 1000 * 60
				}
			});

			max = seriesData.max(val => val[1]) > max ? seriesData.max(val => val[1]) : max;
			index++;
		});

		this.minData = this.metric.data.min(dp => dp.timestamp.getTime());
		this.maxData = this.metric.data.max(dp => dp.timestamp.getTime());

		this.detailedPlotOptions = {
			yaxis: {
				max: max,
				min: min
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
				content: (label, x, y, item) => `${y} actions "${item.series.data[0][2]}" in a ${Constants.USER_ACTIONS_AGGREGATION_INTERVAL} minutes period`,
				defaultTheme: false,
				cssClass: "flot-tooltip"
			},
			selection: {
				mode: "x"
			}
		};

		this.formattedData = series;

		this.overviewPlotOptions = {
			series: {
				bars: {
					show: true,
					align: 'center'
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
					<th>Action</th>
					<th>Count</th>
					<th>Zoom plot</th>
				</tr>
			`;

			$("#metric-data thead").html(header);
			$("#metric-data tfoot").html(header);

			$("#metric-data tbody").html(
				this.metric
					.data
					.map(dp => <UserActionDataPoint>dp)
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
							<td>${dp.action}</td>
							<td># ${dp.count}</td>
							<td>
								<a href="/home/metric/${MetricType[this.metric.metricType]}/${this.metric.source}/${new Date(dp.timestamp.getTime() - 10 * 60 * 1000).getTime()}/${new Date(dp.timestamp.getTime() + 10 * 60 * 1000).getTime()}">
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
