import { Metric, MetricType, DataPoint } from "./abstract";
import { Utility } from "../utility";
import "../extensions";
import "flot";
import "../../vendor/jquery.flot.threshold.js";
import "../../vendor/jquery.flot.tooltip.js";

type JsonPingDataPoint = {
	Timestamp: string;
	ResponseTime: number;
	HttpStatusCode: number;
}

/**
 * 
 * 
 * @export
 * @class PingDataPoint
 * @extends {DataPoint}
 */
export class PingDataPoint extends DataPoint {

	public responseTime: number;
	public httpStatusCode: number;

	constructor(json: JsonPingDataPoint) {
		super();

		this.timestamp = Utility.toDate(json.Timestamp);
		this.responseTime = json.ResponseTime;
		this.httpStatusCode = json.HttpStatusCode;
	}
}

/**
 * 
 * 
 * @export
 * @class PingMetric
 * @extends {Metric<PingDataPoint>}
 */
export class PingMetric extends Metric<PingDataPoint> {

	constructor(source: string) {
		super(source);

		this._metricType = MetricType.Ping;

		this.startLoadUI();
	}

	public generatePlotData(): any {
		var data = [], errors = [];
		this
			.data
			.sortByProperty(dp => dp.timestamp.getTime())
			.reverse()
			.forEach(
			(value, index, array) => {
				if (value.httpStatusCode == 200) {
					data.push([index, value.responseTime]);
				} else {
					errors.push([index, this.max]);
				}
			}
			);
		
		return [data, errors];
	}

	protected renderPlot(): void {
		var [data, errors] = this.generatePlotData();

		let plotOptions: any = {
			yaxis: {
				max: this.max,
				min: this.min
			},
			xaxis: {
				tickDecimals: 0,
				ticks: false
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
				content: "Latency %y ms",
				defaultTheme: false,
				cssClass: "flot-tooltip"
			}
		};

		$.plot(
			$(`[data-identifier="${this.getMetricIdentifier()}"] .metric-chart`),
			[
				{
					data: data,
					lines: {
						show: true,
						fill: 0.50
					},
					color: "#FFC107"
				},
				{
					data: errors,
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
					color: "rgb(200, 20, 30)",
					hoverable: false,
					highlightColor: "rgb(200, 20, 30)"
				}
			],
			plotOptions
		);
	}

	protected getDataPointFromJson(json: any): PingDataPoint {
		return new PingDataPoint(json);
	}
}
