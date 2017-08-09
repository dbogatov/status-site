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

	protected renderTable(redraw: boolean, start: Date, end: Date): void {

		if (!this.dataTablesRendered || redraw) {

			if (this.dataTablesRendered) {
				this.dataTable.destroy();
			}

			let header = `
				<tr>
					<th>Timestamp</th>
					<th>Health</th>
					<th>Details</th>
				</tr>
			`;

			$("#metric-data thead").html(header);
			$("#metric-data tfoot").html(header);

			$("#metric-data tbody").html(
				this.metric
					.data
					.map(dp => <HealthDataPoint>dp)
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
							<td>${dp.health}%</td>
							<td>
								<div style="display: none;" id="${dp.timestamp.getTime()}">
									${JSON.stringify(dp.data)}
								</div>
								<a onclick="
									var data = JSON.parse(document.getElementById('${dp.timestamp.getTime()}').textContent);
									var timestamp = new Date(${dp.timestamp.getTime()});
									var event = new CustomEvent(
										'showDetails', { 
											detail: {
												data: data,
												health: ${dp.health},
												timestamp: timestamp
											}
										}
									);
									document.dispatchEvent(event);
								">Details</a>
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

		// Listen for the event.
		document.addEventListener(
			'showDetails',
			(e: CustomEvent) => {

				let data: any[] = e.detail.data;
				let timestamp: Date = e.detail.timestamp;

				let code = `
					<div 
						class="modal fade health-details-modal"
						id="modal-details-${timestamp.getTime()}" 
						tabindex="-1" 
						role="dialog" 
						aria-hidden="true" 
						style="display: none;"
					>
						<div class="modal-dialog modal-lg">
							<div class="modal-content">

								<div class="modal-header">
									<h4 class="modal-title">
										Health report details | Health ${e.detail.health}% | ${timestamp}
										<small>
											Inspect metric labels at the moment report was generated.
											<a href="/home/metric/${MetricType[this.metric.metricType]}/${this.metric.source}/${new Date(timestamp.getTime() - 2 * 60 * 1000).getTime()}/${new Date(timestamp.getTime() + 2 * 60 * 1000).getTime()}">
												View data at that moment.
											</a>
										</small>
									</h4>
								</div>

								<div class="modal-body">

									<div class="row">
										<div class="col-md-12">
											<table id="details-table" class="table table-striped">
												<thead>
													<tr>
														<th>Type</th>
														<th>Source</th>
														<th>Label</th>
														<th>View data</th>
													</tr>
												</thead>
												<tbody>
													${
					data
						.sortByProperty(el => el.Source)
						.map(el => `
																<tr>
																	<td>${el.Type}</td>
																	<td>${el.Source}</td>
																	<td>${el.Label}</td>
																	<td>
																		<a href="/home/metric/${el.Type}/${el.Source}/${new Date(timestamp.getTime() - 2 * 60 * 1000).getTime()}/${new Date(timestamp.getTime() + 2 * 60 * 1000).getTime()}">
																			View data
																		</a>
																	</td>
																</tr>
															`)
						.join("")
					}
												</tbody>
											</table>
										</div>										
									</div>
								</div>


								<div class="modal-footer">
									<button type="button" class="btn btn-link waves-effect" data-dismiss="modal">
										Close
									</button>
								</div>
							</div>
						</div>
					</div>
				`;

				$("body").append(code);

				$('#details-table').DataTable({
					destroy: true,
					order: [[0, "desc"]],
					lengthChange: false,
					searching: false,
					pageLength: 10
				});

				$(`#modal-details-${timestamp.getTime()}`).modal();

				$(`#modal-details-${timestamp.getTime()}`).on(
					"hidden.bs.modal",
					() => $(".health-details-modal").remove()
				);

			},
			false);


		this.dataTablesRendered = true;
	};
}
